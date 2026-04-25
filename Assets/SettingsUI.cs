using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;
    public Transform    avatarHead;

    [Header("Cog")]
    public Vector2 cogOffset = new Vector2(-60f, 48f);
    [Range(24f, 128f)] public float cogSize = 64f;
    [SerializeField] RectTransform _cogRect;
    [SerializeField] CanvasGroup   _cogGroup;
    [SerializeField] Button        _cogButton;

    [Header("Panel")]
    [SerializeField] RectTransform _panelRect;
    [SerializeField] CanvasGroup   _panelGroup;
    [SerializeField] Image         _panelImage;
    [SerializeField] Button        _closeButton;
    [SerializeField] Text          _bpmText;
    [Range(0, 40)] public int cornerRadius = 16;

    [Header("Sliders")]
    [SerializeField] Slider _sensitivitySlider;
    [SerializeField] Text   _sensitivityValue;
    [SerializeField] Slider _neckAngleSlider;
    [SerializeField] Text   _neckAngleValue;
    [SerializeField] Slider _spineAngleSlider;
    [SerializeField] Text   _spineAngleValue;

    [Header("React Button")]
    [SerializeField] Button _reactButton;
    [SerializeField] Text   _reactButtonLabel;

    [Header("Chat Bubble Button")]
    [SerializeField] Button      _chatBubbleButton;
    [SerializeField] Text        _chatBubbleButtonLabel;
    public           AvatarSpeech avatarSpeech;

    [Header("Monitor")]
    [SerializeField] TMP_Dropdown _monitorDropdown;
    [SerializeField] AvatarDrag   _avatarDrag;

    [Header("Mood History")]
    [SerializeField] MoodHistory _moodHistory;
    [SerializeField] Button      _clearHistoryButton;

    [Header("Timing")]
    [Range(0.1f, 2f)] public float hoverDelay   = 0.8f;
    [Range(0.5f, 3f)] public float fadeOutDelay = 1.5f;

    [DllImport("user32.dll")] static extern IntPtr  GetActiveWindow();
    [DllImport("user32.dll")] static extern bool    SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern bool    GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] static extern bool    EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int left, top, right, bottom; }

    public static bool PanelOpen { get; private set; }
    float _hoverTimer;
    float _outTimer;
    bool  _cogVisible;
    readonly List<RECT> _monitors       = new List<RECT>();
    MonitorEnumProc     _monitorEnumProc;

    void Start()
    {
        // Rounded panel corners
        if (_panelImage)
        {
            _panelImage.sprite = MakeRoundedSprite(64, cornerRadius);
            _panelImage.type   = Image.Type.Sliced;
        }

        // Hide both groups at start
        SetGroup(_cogGroup,   false);
        SetGroup(_panelGroup, false);

        // Button listeners
        _cogButton?.onClick.AddListener(TogglePanel);
        _closeButton?.onClick.AddListener(TogglePanel);
        _reactButton?.onClick.AddListener(ToggleReactToMusic);
        _chatBubbleButton?.onClick.AddListener(ToggleChatBubble);
        _clearHistoryButton?.onClick.AddListener(ClearMoodHistory);

        // Slider bindings
        if (beatDetector != null)
        {
            BindSlider(_sensitivitySlider, _sensitivityValue, 0.1f, 5f,
                () => beatDetector.sensitivity,   v => beatDetector.sensitivity   = v, "sensitivity");
            if (beatDetector.boneChain != null && beatDetector.boneChain.Length > 1)
                BindSlider(_neckAngleSlider,  _neckAngleValue,  -20f, 20f,
                    () => beatDetector.boneChain[1].angle, v => beatDetector.boneChain[1].angle = v, null);
            if (beatDetector.boneChain != null && beatDetector.boneChain.Length > 0)
                BindSlider(_spineAngleSlider, _spineAngleValue, -20f, 20f,
                    () => beatDetector.boneChain[0].angle, v => beatDetector.boneChain[0].angle = v, null);
        }

        // Load saved toggle states
        if (beatDetector != null)
            beatDetector.reactToMusic = PlayerPrefs.GetInt("reactToMusic", 1) == 1;
        if (avatarSpeech != null)
            avatarSpeech.enabled = PlayerPrefs.GetInt("chatBubble", 1) == 1;

        RefreshReactButton();
        RefreshChatBubbleButton();
        PopulateMonitorDropdown();
    }

    void Update()
    {
        UpdateCogPosition();
        UpdateHover();
        UpdateFade();

        if (PanelOpen && _bpmText && beatDetector)
            _bpmText.text = $"BPM: {beatDetector.BPM:F1}";
    }

    // ── Cog positioning ──────────────────────────────────────────────────────

    void UpdateCogPosition()
    {
        if (!avatarHead || !Camera.main || !_cogRect) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(avatarHead.position);
        _cogRect.sizeDelta        = new Vector2(cogSize, cogSize);
        _cogRect.anchoredPosition = new Vector2(
            sp.x - Screen.width  * 0.5f + cogOffset.x,
            sp.y - Screen.height * 0.5f + cogOffset.y);
    }

    // ── Hover show/hide ──────────────────────────────────────────────────────

    void UpdateHover()
    {
        bool over = IsOverAvatar();
        if (over)
        {
            _outTimer = 0f;
            if (!_cogVisible)
            {
                _hoverTimer += Time.deltaTime;
                if (_hoverTimer >= hoverDelay) ShowCog();
            }
        }
        else
        {
            _hoverTimer = 0f;
            if (_cogVisible && !PanelOpen)
            {
                _outTimer += Time.deltaTime;
                if (_outTimer >= fadeOutDelay) HideCog();
            }
        }
    }

    void ShowCog() { _cogVisible = true; }
    void HideCog() { _cogVisible = false; _hoverTimer = 0f; }

    // ── Alpha fading ─────────────────────────────────────────────────────────

    void UpdateFade()
    {
        Fade(_cogGroup,   _cogVisible  ? 1f : 0f, 5f);
        Fade(_panelGroup, PanelOpen   ? 1f : 0f, 8f);
    }

    void Fade(CanvasGroup g, float target, float speed)
    {
        if (!g) return;
        g.alpha           = Mathf.MoveTowards(g.alpha, target, Time.deltaTime * speed);
        g.interactable    = g.alpha > 0.5f;
        g.blocksRaycasts  = g.alpha > 0.5f;
    }

    // ── Panel toggle ─────────────────────────────────────────────────────────

    void TogglePanel()
    {
        PanelOpen = !PanelOpen;
        if (!PanelOpen) return;

        ShowCog();

        Vector2 cogPos   = _cogRect.anchoredPosition;
        float   panelH   = _panelRect.sizeDelta.y;
        float   panelW   = _panelRect.sizeDelta.x;
        float   halfW    = Screen.width  * 0.5f;
        float   halfH    = Screen.height * 0.5f;

        // Open toward whichever side has more space
        float cogScreenY = cogPos.y + halfH;
        float yDir       = (Screen.height - cogScreenY) >= cogScreenY ? 1f : -1f;
        float yPos       = Mathf.Clamp(cogPos.y + yDir * (32f + panelH * 0.5f),
                               -halfH + panelH * 0.5f + 10f,
                                halfH - panelH * 0.5f - 10f);

        float cogScreenX = cogPos.x + halfW;
        float xDir       = (Screen.width - cogScreenX) >= cogScreenX ? 1f : -1f;
        float xPos       = Mathf.Clamp(cogPos.x + xDir * panelW * 0.5f,
                               -halfW + panelW * 0.5f + 10f,
                                halfW - panelW * 0.5f - 10f);

        _panelRect.anchoredPosition = new Vector2(xPos, yPos);
    }

    // ── React to Music ───────────────────────────────────────────────────────

    void ToggleReactToMusic()
    {
        if (!beatDetector) return;
        beatDetector.reactToMusic = !beatDetector.reactToMusic;
        PlayerPrefs.SetInt("reactToMusic", beatDetector.reactToMusic ? 1 : 0);
        PlayerPrefs.Save();
        RefreshReactButton();
    }

    void RefreshReactButton()
    {
        if (_reactButtonLabel)
            _reactButtonLabel.text = "React to Music: " + (beatDetector != null && beatDetector.reactToMusic ? "ON" : "OFF");
    }

    // ── Chat Bubble ──────────────────────────────────────────────────────────

    void ToggleChatBubble()
    {
        if (!avatarSpeech) return;
        avatarSpeech.enabled = !avatarSpeech.enabled;
        PlayerPrefs.SetInt("chatBubble", avatarSpeech.enabled ? 1 : 0);
        PlayerPrefs.Save();
        RefreshChatBubbleButton();
    }

    void RefreshChatBubbleButton()
    {
        if (_chatBubbleButtonLabel)
            _chatBubbleButtonLabel.text = "Chat Bubble: " + (avatarSpeech != null && avatarSpeech.enabled ? "ON" : "OFF");
    }

    // ── Mood History ─────────────────────────────────────────────────────────

    void ClearMoodHistory() => _moodHistory?.ClearHistory();

    // ── Hit testing (used by TransparentWindow) ───────────────────────────────

    public bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        var ped = new PointerEventData(EventSystem.current)
                      { position = TransparentWindow.CursorWindowPos() };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }

    bool IsOverAvatar()
    {
        if (!Camera.main) return false;
#if UNITY_EDITOR
        if (Mouse.current == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
#else
        Vector2 cp  = TransparentWindow.CursorWindowPos();
        Ray     ray = Camera.main.ScreenPointToRay(new Vector3(cp.x, cp.y, 0));
#endif
        return Physics.Raycast(ray) || IsPointerOverUI();
    }

    // ── Monitor selection ────────────────────────────────────────────────────

    void PopulateMonitorDropdown()
    {
        if (!_monitorDropdown) return;

        _monitors.Clear();
        _monitorEnumProc = (IntPtr hMon, IntPtr hdc, ref RECT rect, IntPtr data) =>
        {
            _monitors.Add(rect);
            return true;
        };
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, _monitorEnumProc, IntPtr.Zero);

        _monitorDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < _monitors.Count; i++)
        {
            RECT r = _monitors[i];
            options.Add(new TMP_Dropdown.OptionData($"Monitor {i + 1} ({r.right - r.left}x{r.bottom - r.top})"));
        }
        _monitorDropdown.AddOptions(options);

        int current = DetectCurrentMonitor();
        _monitorDropdown.SetValueWithoutNotify(current);
        PlayerPrefs.SetInt("monitor", current);
        PlayerPrefs.Save();
        _monitorDropdown.onValueChanged.AddListener(OnMonitorChanged);
    }

    int DetectCurrentMonitor()
    {
        if (_monitors.Count == 0) return 0;
        if (!GetWindowRect(GetActiveWindow(), out RECT wr)) return 0;
        int cx = (wr.left + wr.right) / 2;
        int cy = (wr.top  + wr.bottom) / 2;
        for (int i = 0; i < _monitors.Count; i++)
        {
            RECT r = _monitors[i];
            if (cx >= r.left && cx < r.right && cy >= r.top && cy < r.bottom)
                return i;
        }
        return 0;
    }

    void OnMonitorChanged(int index)
    {
        PlayerPrefs.SetInt("monitor", index);
        PlayerPrefs.Save();
#if !UNITY_EDITOR
        Vector2 rel = GetAvatarNormalizedPos();
        MoveToMonitor(index);
        StartCoroutine(SnapAvatarAfterMove(rel));
#endif
    }

    void MoveToMonitor(int index)
    {
        if (index < 0 || index >= _monitors.Count) return;
        RECT   r  = _monitors[index];
        IntPtr hw = GetActiveWindow();
        SetWindowPos(hw, IntPtr.Zero, r.left, r.top, r.right - r.left, r.bottom - r.top, 0x0040);
    }

    Vector2 GetAvatarNormalizedPos()
    {
        if (!_avatarDrag || !_avatarDrag.avatarRoot || !Camera.main) return new Vector2(0.5f, 0.2f);
        Vector3 sp = Camera.main.WorldToScreenPoint(_avatarDrag.avatarRoot.position);
        return new Vector2(sp.x / Screen.width, sp.y / Screen.height);
    }

    IEnumerator SnapAvatarAfterMove(Vector2 rel)
    {
        yield return null;
        if (!_avatarDrag || !_avatarDrag.avatarRoot || !Camera.main) yield break;

        Transform root  = _avatarDrag.avatarRoot;
        float     depth = Camera.main.WorldToScreenPoint(root.position).z;
        root.position   = Camera.main.ScreenToWorldPoint(
            new Vector3(rel.x * Screen.width, rel.y * Screen.height, depth));

        PlayerPrefs.SetFloat("avatarPosX", root.position.x);
        PlayerPrefs.SetFloat("avatarPosY", root.position.y);
        PlayerPrefs.Save();
    }

    // ── Dev utilities ────────────────────────────────────────────────────────

    [ContextMenu("Clear Saved Settings")]
    void ClearSavedSettings()
    {
        PlayerPrefs.DeleteKey("sensitivity");
        PlayerPrefs.DeleteKey("reactToMusic");
        PlayerPrefs.DeleteKey("chatBubble");
        PlayerPrefs.DeleteKey("avatarPosX");
        PlayerPrefs.DeleteKey("avatarPosY");
        PlayerPrefs.DeleteKey("monitor");
        PlayerPrefs.Save();
        Debug.Log("Saved settings cleared.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void BindSlider(Slider slider, Text valueText, float min, float max,
                    Func<float> get, Action<float> set, string prefsKey)
    {
        if (!slider) return;

        if (prefsKey != null && PlayerPrefs.HasKey(prefsKey))
            set(PlayerPrefs.GetFloat(prefsKey));

        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = get();
        if (valueText) valueText.text = get().ToString("F2");
        slider.onValueChanged.AddListener(v =>
        {
            set(v);
            if (valueText) valueText.text = v.ToString("F2");
            if (prefsKey != null) { PlayerPrefs.SetFloat(prefsKey, v); PlayerPrefs.Save(); }
        });
    }

    void SetGroup(CanvasGroup g, bool visible)
    {
        if (!g) return;
        g.alpha           = visible ? 1f : 0f;
        g.interactable    = visible;
        g.blocksRaycasts  = visible;
    }

    Sprite MakeRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = InRoundedRect(x, y, size, size, radius)
                ? Color.white : new Color(1f, 1f, 1f, 0f);
        tex.SetPixels(pixels);
        tex.Apply();
        float b = radius;
        return Sprite.Create(tex, new Rect(0, 0, size, size),
                             new Vector2(0.5f, 0.5f), 1f, 0,
                             SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    bool InRoundedRect(int x, int y, int w, int h, int r)
    {
        bool cx = x < r || x >= w - r;
        bool cy = y < r || y >= h - r;
        if (!cx || !cy) return true;
        int nx = x < r ? r : w - r - 1;
        int ny = y < r ? r : h - r - 1;
        float dx = x - nx, dy = y - ny;
        return dx * dx + dy * dy <= (float)(r * r);
    }
}
