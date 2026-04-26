using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Runtime.InteropServices;

public class AvatarDrag : MonoBehaviour
{
    public Transform avatarRoot;

    [Header("Interactions")]
    [SerializeField] AvatarSpeech _avatarSpeech;
    [Range(0.3f, 5f)] public float spinDuration  = 0.6f;
    [Range(0.5f, 5f)] public float spinCooldown  = 2f;
    [Range(1f,  10f)] public float pokeResetTime = 3f;
    [Range(5f,  30f)] public float holdTime      = 10f;

    [Header("Drag Lean")]
    [Range(0f,  30f)] public float leanMaxAngle = 18f;
    [Range(0.5f, 6f)] public float leanVelScale  = 4f;
    [Range(1f,  20f)] public float leanSmooth    = 5f;

    public static bool IsDragging { get; private set; }

    [DllImport("user32.dll")] static extern bool  GetCursorPos(out TransparentWindow.POINT p);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

    const int VK_LBUTTON  = 0x01;
    const int VK_LCONTROL = 0xA2;
    const int VK_S        = 0x53;
    const int VK_M        = 0x4D;

    Animator _animator;
    bool     _rootMotionDisabled;
    Vector3  _dragOffset;
    Vector3  _dragStartPos;
    bool     _prevButtonDown;

    int   _spinCount;
    float _spinCooldownTimer;
    bool  _prevSpinCombo;
    bool  _isSpinning;

    int   _pokeCount;
    float _pokeTimer;
    bool  _pokeMadFired;

    float      _holdTimer;
    bool       _holdReactionFired;
    Vector3    _prevCursorWorld;
    float      _leanX;
    Quaternion _dragBaseRot;

    void Start()
    {
        if (avatarRoot)
        {
            _animator = avatarRoot.GetComponentInChildren<Animator>();
            if (PlayerPrefs.HasKey("avatarPosX"))
                avatarRoot.position = new Vector3(
                    PlayerPrefs.GetFloat("avatarPosX"),
                    PlayerPrefs.GetFloat("avatarPosY"),
                    avatarRoot.position.z);
        }
        _prevCursorWorld = CursorWorldPos();
        _dragBaseRot     = avatarRoot.rotation;
    }

    void LateUpdate()
    {
        if (!avatarRoot || !Camera.main) return;

        if (!_rootMotionDisabled && _animator != null)
        {
            _animator.applyRootMotion = false;
            _rootMotionDisabled = true;
        }

        bool buttonDown   = IsKeyDown(VK_LBUTTON);
        bool justPressed  = buttonDown && !_prevButtonDown;
        bool justReleased = !buttonDown && _prevButtonDown;
        _prevButtonDown   = buttonDown;

        UpdateSpinHotkey();

        if (_pokeTimer > 0f)
        {
            _pokeTimer -= Time.deltaTime;
            if (_pokeTimer <= 0f) { _pokeCount = 0; _pokeMadFired = false; }
        }

        if (justPressed && !SettingsUI.PanelOpen && IsOverAvatar())
        {
            IsDragging       = true;
            _dragStartPos    = avatarRoot.position;
            _dragOffset      = avatarRoot.position - CursorWorldPos();
            _dragBaseRot     = avatarRoot.rotation;
            _prevCursorWorld = CursorWorldPos();
        }

        if (justReleased && IsDragging)
        {
            PlayerPrefs.SetFloat("avatarPosX", avatarRoot.position.x);
            PlayerPrefs.SetFloat("avatarPosY", avatarRoot.position.y);
            PlayerPrefs.Save();

            if (Vector3.Distance(avatarRoot.position, _dragStartPos) < 0.05f)
                HandlePoke();

            _holdTimer         = 0f;
            _holdReactionFired = false;
            // let _leanX lerp back to 0 naturally — no hard reset
        }

        if (!buttonDown)    IsDragging = false;
        if (IsDragging && buttonDown) avatarRoot.position = CursorWorldPos() + _dragOffset;

        UpdateHoldTimer(buttonDown);
        UpdateRagdoll();
    }

    void UpdateHoldTimer(bool buttonDown)
    {
        if (!IsDragging || !buttonDown) return;
        _holdTimer += Time.deltaTime;
        if (!_holdReactionFired && _holdTimer >= holdTime)
        {
            _holdReactionFired = true;
            PlayerPrefs.SetInt("heldTooLong", 1);
            PlayerPrefs.Save();
            _avatarSpeech?.TriggerHold();
        }
    }

    void UpdateRagdoll()
    {
        if (_isSpinning) return;

        float targetX = 0f;

        if (IsDragging)
        {
            Vector3 cursor = CursorWorldPos();
            float   dt     = Mathf.Max(Time.deltaTime, 0.001f);
            float   vx     = (cursor.x - _prevCursorWorld.x) / dt;
            targetX          = Mathf.Clamp(vx * leanVelScale, -leanMaxAngle, leanMaxAngle);
            _prevCursorWorld = cursor;
        }

        _leanX = Mathf.LerpAngle(_leanX, targetX, Time.deltaTime * leanSmooth);

        if (Mathf.Abs(_leanX) > 0.05f)
            avatarRoot.rotation = _dragBaseRot * Quaternion.Euler(0f, 0f, _leanX);
    }

    void UpdateSpinHotkey()
    {
        bool combo        = IsKeyDown(VK_LCONTROL) && IsKeyDown(VK_S) && IsKeyDown(VK_M);
        bool justActivated = combo && !_prevSpinCombo;
        _prevSpinCombo    = combo;

        if (_spinCooldownTimer > 0f) _spinCooldownTimer -= Time.deltaTime;

        if (justActivated && _spinCooldownTimer <= 0f && !_isSpinning)
        {
            _spinCount++;
            _spinCooldownTimer = spinCooldown;
            _avatarSpeech?.TriggerSpin(_spinCount);
            StartCoroutine(SpinAvatar());
        }
    }

    void HandlePoke()
    {
        _pokeTimer = pokeResetTime;
        _pokeCount++;

        if (!_pokeMadFired && _pokeCount >= 10)
        {
            _pokeMadFired = true;
            PlayerPrefs.SetInt("pokedMad", 1);
            PlayerPrefs.Save();
            _avatarSpeech?.TriggerPoke(2);
        }
        else if (_pokeCount == 5)
        {
            _avatarSpeech?.TriggerPoke(1);
        }
    }

    IEnumerator SpinAvatar()
    {
        _isSpinning = true;
        float     elapsed  = 0f;
        Quaternion startRot = avatarRoot.rotation;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            avatarRoot.rotation = startRot * Quaternion.Euler(0f, 360f * (elapsed / spinDuration), 0f);
            yield return null;
        }

        avatarRoot.rotation = startRot;
        _isSpinning = false;
    }

    static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    Vector3 CursorWorldPos()
    {
        float depth = Camera.main.WorldToScreenPoint(avatarRoot.position).z;
#if UNITY_EDITOR
        Vector2 sp = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        Vector2 sp = TransparentWindow.CursorWindowPos();
#endif
        return Camera.main.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));
    }

    bool IsOverAvatar()
    {
#if UNITY_EDITOR
        if (Mouse.current == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
#else
        Vector2 cp  = TransparentWindow.CursorWindowPos();
        Ray     ray = Camera.main.ScreenPointToRay(new Vector3(cp.x, cp.y, 0));
#endif
        return Physics.Raycast(ray);
    }
}
