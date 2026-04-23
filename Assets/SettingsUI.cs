using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;
    public Transform    avatarHead;
    public Sprite       cogSprite;

    [Header("Cog Position")]
    public Vector2 cogOffset = new Vector2(-60f, 48f);
    [Range(24f, 128f)] public float cogSize = 64f;

    [Header("Timing")]
    [Range(0.1f, 2f)] public float hoverDelay   = 0.8f;
    [Range(0.5f, 3f)] public float fadeOutDelay = 1.5f;

    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out TransparentWindow.POINT p);

    GraphicRaycaster _raycaster;
    CanvasGroup      _cogGroup;
    RectTransform    _cogRect;
    CanvasGroup      _panelGroup;
    RectTransform    _panelRect;
    Text             _bpmText;
    bool             _panelOpen;
    float            _hoverTimer;
    float            _outTimer;
    bool             _cogVisible;

    void Awake() => BuildUI();

    // ─────────────────────────────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────────────────────────────

    void Update()
    {
        UpdateCogPosition();
        UpdateHover();
        UpdateFade();
        if (_panelOpen && _bpmText != null && beatDetector != null)
            _bpmText.text = $"BPM: {beatDetector.BPM:F1}";
    }

    void UpdateCogPosition()
    {
        if (!avatarHead || !Camera.main) return;
        Vector3 sp = Camera.main.WorldToScreenPoint(avatarHead.position);
        // Anchor cog above and to the viewer's left of the head
        _cogRect.sizeDelta        = new Vector2(cogSize, cogSize);
        _cogRect.anchoredPosition = new Vector2(
            sp.x - Screen.width  * 0.5f + cogOffset.x,
            sp.y - Screen.height * 0.5f + cogOffset.y);
    }

    void UpdateHover()
    {
#if UNITY_EDITOR
        // In editor use Unity's mouse position for testing
        bool over = IsOverAvatar();
#else
        bool over = IsOverAvatar();
#endif
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
            if (_cogVisible && !_panelOpen)
            {
                _outTimer += Time.deltaTime;
                if (_outTimer >= fadeOutDelay) HideCog();
            }
        }
    }

    void UpdateFade()
    {
        float cogTarget   = _cogVisible  ? 1f : 0f;
        float panelTarget = _panelOpen   ? 1f : 0f;

        _cogGroup.alpha  = Mathf.MoveTowards(_cogGroup.alpha,   cogTarget,   Time.deltaTime * 5f);
        _panelGroup.alpha = Mathf.MoveTowards(_panelGroup.alpha, panelTarget, Time.deltaTime * 8f);

        _cogGroup.interactable    = _cogGroup.alpha   > 0.5f;
        _cogGroup.blocksRaycasts  = _cogGroup.alpha   > 0.5f;
        _panelGroup.interactable   = _panelGroup.alpha > 0.5f;
        _panelGroup.blocksRaycasts = _panelGroup.alpha > 0.5f;
    }

    void ShowCog() { _cogVisible = true; }
    void HideCog() { _cogVisible = false; _hoverTimer = 0f; }

    void TogglePanel()
    {
        _panelOpen = !_panelOpen;
        if (_panelOpen)
        {
            // Position panel above the cog, clamped to screen
            Vector2 cogPos  = _cogRect.anchoredPosition;
            float   panelH  = _panelRect.sizeDelta.y;
            float   panelW  = _panelRect.sizeDelta.x;
            float   screenHalf = Screen.height * 0.5f;
            float   yPos    = cogPos.y + 32f + panelH * 0.5f;
            // Clamp so panel stays on screen
            yPos = Mathf.Clamp(yPos, -screenHalf + panelH * 0.5f + 10f, screenHalf - panelH * 0.5f - 10f);
            float xPos = Mathf.Clamp(cogPos.x, -Screen.width * 0.5f + panelW * 0.5f + 10f, Screen.width * 0.5f - panelW * 0.5f - 10f);
            _panelRect.anchoredPosition = new Vector2(xPos, yPos);
            ShowCog();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Hit testing — used by TransparentWindow to keep click-through off
    // ─────────────────────────────────────────────────────────────────────

    public bool IsPointerOverUI()
    {
        GetCursorPos(out TransparentWindow.POINT p);
        float screenY = Screen.height - p.Y;
        var ped = new PointerEventData(EventSystem.current) { position = new Vector2(p.X, screenY) };
        var results = new List<RaycastResult>();
        _raycaster.Raycast(ped, results);
        return results.Count > 0;
    }

    bool IsOverAvatar()
    {
        if (!Camera.main) return false;
#if UNITY_EDITOR
        Vector2 mp = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mp);
#else
        GetCursorPos(out TransparentWindow.POINT p);
        float screenY = Screen.height - p.Y;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(p.X, screenY, 0));
#endif
        return Physics.Raycast(ray) || IsPointerOverUI();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UI Construction
    // ─────────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGo = new GameObject("SettingsCanvas");
        var canvas   = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        _raycaster = canvasGo.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        BuildCog(canvasGo.transform);
        BuildPanel(canvasGo.transform);
    }

    void BuildCog(Transform root)
    {
        var go = new GameObject("Cog", typeof(RectTransform));
        go.transform.SetParent(root, false);

        _cogGroup                  = go.AddComponent<CanvasGroup>();
        _cogGroup.alpha            = 0f;
        _cogGroup.interactable     = false;
        _cogGroup.blocksRaycasts   = false;

        _cogRect           = (RectTransform)go.transform;
        _cogRect.sizeDelta = new Vector2(48, 48);
        _cogRect.anchorMin = _cogRect.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        if (cogSprite != null) img.sprite = cogSprite;
        else                   img.color  = new Color(1f, 0.85f, 0.3f, 0.9f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(1f, 0.85f, 0.3f);
        c.pressedColor     = new Color(0.8f, 0.6f, 0.2f);
        btn.colors         = c;
        btn.onClick.AddListener(TogglePanel);
    }

    void BuildPanel(Transform root)
    {
        var go = new GameObject("SettingsPanel", typeof(RectTransform));
        go.transform.SetParent(root, false);

        _panelGroup                  = go.AddComponent<CanvasGroup>();
        _panelGroup.alpha            = 0f;
        _panelGroup.interactable     = false;
        _panelGroup.blocksRaycasts   = false;

        _panelRect           = (RectTransform)go.transform;
        _panelRect.sizeDelta = new Vector2(260, 252);
        _panelRect.anchorMin = _panelRect.anchorMax = new Vector2(0.5f, 0.5f);

        var panelImg  = go.AddComponent<Image>();
        panelImg.color  = new Color(0.11f, 0.13f, 0.18f, 0.92f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform p = go.transform;

        // Header
        MakeText("Title", p, new Vector2(-40f, 108f), new Vector2(140, 26),
                 "SETTINGS", 13, new Color(1f, 0.85f, 0.3f), font, FontStyle.Bold);

        // Close button
        var closeRT = MakeRect("CloseBtn", p, new Vector2(108f, 108f), new Vector2(26, 26));
        var closeBg  = closeRT.gameObject.AddComponent<Image>();
        closeBg.color = new Color(0.6f, 0.18f, 0.18f);
        var closeBtn = closeRT.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(TogglePanel);
        MakeText("X", closeRT, Vector2.zero, new Vector2(26, 26), "✕", 12, Color.white, font);

        MakeDivider(p, 94f);

        // Sliders
        float y = 68f;
        if (beatDetector != null)
        {
            y = SliderRow(p, font, "Sensitivity", y, 1f,  3f,  () => beatDetector.sensitivity,   v => beatDetector.sensitivity   = v);
            y = SliderRow(p, font, "Neck Angle",  y, 0f,  2f,  () => beatDetector.headNodScale,  v => beatDetector.headNodScale  = v);
            y = SliderRow(p, font, "Spine Angle", y, 0f,  2f,  () => beatDetector.spineScale,    v => beatDetector.spineScale    = v);
        }

        MakeDivider(p, y - 8f);

        // Live BPM readout
        var bpmRT = MakeRect("BPM", p, new Vector2(0, y - 28f), new Vector2(230, 20));
        _bpmText           = bpmRT.gameObject.AddComponent<Text>();
        _bpmText.font      = font;
        _bpmText.fontSize  = 11;
        _bpmText.color     = new Color(0.45f, 0.85f, 0.55f);
        _bpmText.alignment = TextAnchor.MiddleCenter;
        _bpmText.text      = "BPM: —";

        // React to Music toggle
        var reactRT  = MakeRect("ReactBtn", p, new Vector2(0, y - 54f), new Vector2(220, 28));
        var reactBg  = reactRT.gameObject.AddComponent<Image>();
        bool reactOn = beatDetector == null || beatDetector.reactToMusic;
        reactBg.color = reactOn ? new Color(0.15f, 0.38f, 0.22f) : new Color(0.28f, 0.15f, 0.15f);
        var reactBtn = reactRT.gameObject.AddComponent<Button>();
        reactBtn.targetGraphic = reactBg;
        var reactLabel = MakeText("Label", reactRT, Vector2.zero, new Vector2(220, 28),
                                  "React to Music: " + (reactOn ? "ON" : "OFF"), 11, Color.white, font);
        reactBtn.onClick.AddListener(() =>
        {
            if (!beatDetector) return;
            beatDetector.reactToMusic = !beatDetector.reactToMusic;
            bool on = beatDetector.reactToMusic;
            reactBg.color   = on ? new Color(0.15f, 0.38f, 0.22f) : new Color(0.28f, 0.15f, 0.15f);
            reactLabel.text = "React to Music: " + (on ? "ON" : "OFF");
        });
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    float SliderRow(Transform parent, Font font, string label, float y,
                    float min, float max, Func<float> get, Action<float> set)
    {
        var row = MakeRect(label + "Row", parent, new Vector2(0, y), new Vector2(240, 36));

        MakeText("Lbl", row, new Vector2(-72f, 0), new Vector2(82, 28), label,
                 11, new Color(0.92f, 0.92f, 0.92f), font);

        var valRT = MakeRect("Val", row, new Vector2(102f, 0), new Vector2(40, 28));
        var valTxt = valRT.gameObject.AddComponent<Text>();
        valTxt.font      = font;
        valTxt.fontSize  = 11;
        valTxt.color     = new Color(1f, 0.85f, 0.3f);
        valTxt.alignment = TextAnchor.MiddleRight;
        valTxt.text      = get().ToString("F2");

        var slider = MakeSlider(row, new Vector2(12f, 0), new Vector2(118, 14), min, max, get());
        slider.onValueChanged.AddListener(v => { set(v); valTxt.text = v.ToString("F2"); });
        return y - 44f;
    }

    Slider MakeSlider(RectTransform parent, Vector2 pos, Vector2 size, float min, float max, float val)
    {
        var go     = MakeRect("Slider", parent, pos, size);
        var slider = go.gameObject.AddComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = val;

        var bg = MakeRect("BG", go, Vector2.zero, Vector2.zero);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

        var fillArea = MakeRect("FillArea", go, Vector2.zero, new Vector2(-20, 0));
        fillArea.anchorMin = new Vector2(0, 0.25f); fillArea.anchorMax = new Vector2(1, 0.75f);
        fillArea.offsetMin = new Vector2(5, 0);     fillArea.offsetMax = new Vector2(-15, 0);

        var fill = MakeRect("Fill", fillArea, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0, 1);
        fill.gameObject.AddComponent<Image>().color = new Color(1f, 0.78f, 0.2f);
        slider.fillRect = fill;

        var handleArea = MakeRect("HandleArea", go, Vector2.zero, Vector2.zero);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);

        var handle    = MakeRect("Handle", handleArea, Vector2.zero, new Vector2(16, 16));
        handle.anchorMin = handle.anchorMax = new Vector2(0, 0.5f);
        var handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color        = Color.white;
        slider.handleRect      = handle;
        slider.targetGraphic   = handleImg;

        return slider;
    }

    RectTransform MakeRect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return rt;
    }

    Text MakeText(string name, Transform parent, Vector2 pos, Vector2 size,
                  string content, int fontSize, Color color, Font font,
                  FontStyle style = FontStyle.Normal)
    {
        var rt   = MakeRect(name, parent, pos, size);
        var txt  = rt.gameObject.AddComponent<Text>();
        txt.text      = content;
        txt.font      = font;
        txt.fontSize  = fontSize;
        txt.fontStyle = style;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
    }

    void MakeDivider(Transform parent, float y)
    {
        var rt = MakeRect("Divider", parent, new Vector2(0, y), new Vector2(268, 1));
        rt.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);
    }

    Sprite MakeRoundedRectSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color[size * size];
        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
            pixels[py * size + px] = RoundedRectContains(px, py, size, size, radius) ? Color.white : Color.clear;
        tex.SetPixels(pixels);
        tex.Apply();
        float b = radius;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0,
                             SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    bool RoundedRectContains(int x, int y, int w, int h, int r)
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
