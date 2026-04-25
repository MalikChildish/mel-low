using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;

public class AvatarDrag : MonoBehaviour
{
    public Transform avatarRoot;

    public static bool IsDragging { get; private set; }

    [DllImport("user32.dll")] static extern bool  GetCursorPos(out TransparentWindow.POINT p);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

    const int VK_LBUTTON = 0x01;

    Animator _animator;
    bool     _rootMotionDisabled;
    Vector3  _dragOffset;
    bool     _prevButtonDown;

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
    }

    void LateUpdate()
    {
        if (!avatarRoot || !Camera.main) return;

        if (!_rootMotionDisabled && _animator != null)
        {
            _animator.applyRootMotion = false;
            _rootMotionDisabled = true;
        }

        bool buttonDown  = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        bool justPressed = buttonDown && !_prevButtonDown;
        _prevButtonDown  = buttonDown;

        if (justPressed && !SettingsUI.PanelOpen && IsOverAvatar())
        {
            IsDragging  = true;
            _dragOffset = avatarRoot.position - CursorWorldPos();
        }

        if (IsDragging && !buttonDown)
        {
            PlayerPrefs.SetFloat("avatarPosX", avatarRoot.position.x);
            PlayerPrefs.SetFloat("avatarPosY", avatarRoot.position.y);
            PlayerPrefs.Save();
        }

        if (!buttonDown)
            IsDragging = false;

        if (IsDragging && buttonDown)
            avatarRoot.position = CursorWorldPos() + _dragOffset;
    }

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
