using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ColorPickerPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform _settingsPanelRect;
    [SerializeField] RawImage      _wheel;
    [SerializeField] Slider        _valueSlider;
    [SerializeField] Image         _preview;
    [SerializeField] Image         _previousSwatch;
    [SerializeField] Button        _previousButton;
    [SerializeField] Button        _resetButton;
    [SerializeField] Button        _closeButton;

    Action<Color> _onChange;
    Color         _previousColor;
    Color         _defaultColor;
    float         _hue, _sat, _val = 1f;
    Texture2D     _wheelTex;
    RectTransform _rect;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        GenerateWheel();

        var trigger = _wheel.gameObject.AddComponent<EventTrigger>();
        AddTrigger(trigger, EventTriggerType.PointerDown, SampleWheel);
        AddTrigger(trigger, EventTriggerType.Drag,        SampleWheel);

        _valueSlider.minValue = 0f;
        _valueSlider.maxValue = 1f;
        _valueSlider.onValueChanged.AddListener(v => { _val = v; Push(); });

        _closeButton?.onClick.AddListener(Hide);
        _previousButton?.onClick.AddListener(RestorePrevious);
        _resetButton?.onClick.AddListener(RestoreDefault);

        CreateBackdrop();

        gameObject.SetActive(false);
    }

    void CreateBackdrop()
    {
        var go  = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2000f, -2000f);
        rt.offsetMax = new Vector2( 2000f,  2000f);

        var img = go.GetComponent<Image>();
        img.color = Color.clear;

        go.GetComponent<Button>().onClick.AddListener(Hide);
    }

    public void Show(Color initial, Color defaultColor, Action<Color> onChange)
    {
        _onChange      = onChange;
        _previousColor = initial;
        _defaultColor  = defaultColor;

        if (_previousSwatch) _previousSwatch.color = initial;

        Color.RGBToHSV(initial, out _hue, out _sat, out _val);
        _valueSlider.SetValueWithoutNotify(_val);

        gameObject.SetActive(true);
        PositionNextToSettings();
        Push();
    }

    public void Hide() => gameObject.SetActive(false);

    void RestorePrevious()
    {
        Color.RGBToHSV(_previousColor, out _hue, out _sat, out _val);
        _valueSlider.SetValueWithoutNotify(_val);
        Push();
    }

    void RestoreDefault()
    {
        Color.RGBToHSV(_defaultColor, out _hue, out _sat, out _val);
        _valueSlider.SetValueWithoutNotify(_val);
        Push();
    }

    void PositionNextToSettings()
    {
        if (!_settingsPanelRect || !_rect) return;

        float   halfW    = Screen.width  * 0.5f;
        float   halfH    = Screen.height * 0.5f;
        Vector2 panelPos = _settingsPanelRect.anchoredPosition;
        float   panelW   = _settingsPanelRect.sizeDelta.x;
        float   pickerW  = _rect.sizeDelta.x;
        float   pickerH  = _rect.sizeDelta.y;

        float panelRight = panelPos.x + panelW * 0.5f;
        float panelLeft  = panelPos.x - panelW * 0.5f;

        float x = (halfW - panelRight) >= (panelLeft + halfW)
            ? panelRight + pickerW * 0.5f + 8f
            : panelLeft  - pickerW * 0.5f - 8f;

        x = Mathf.Clamp(x, -halfW + pickerW * 0.5f + 10f, halfW - pickerW * 0.5f - 10f);
        float y = Mathf.Clamp(panelPos.y, -halfH + pickerH * 0.5f + 10f, halfH - pickerH * 0.5f - 10f);

        _rect.anchoredPosition = new Vector2(x, y);
    }

    void SampleWheel(BaseEventData data)
    {
        var ped  = (PointerEventData)data;
        var rect = (RectTransform)_wheel.transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, ped.position, ped.pressEventCamera, out Vector2 local)) return;

        float r  = rect.rect.width * 0.5f;
        float dx = local.x / r;
        float dy = local.y / r;
        float d  = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 1f) return;

        _hue = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f);
        _sat = d;
        Push();
    }

    void Push()
    {
        Color c = Color.HSVToRGB(_hue, _sat, _val);
        if (_preview) _preview.color = c;
        _onChange?.Invoke(c);
    }

    void GenerateWheel()
    {
        const int size = 128;
        _wheelTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = (x - half) / half;
            float dy   = (y - half) / half;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist > 1f) { _wheelTex.SetPixel(x, y, Color.clear); continue; }
            float hue  = Mathf.Repeat(Mathf.Atan2(dy, dx) / (2f * Mathf.PI), 1f);
            _wheelTex.SetPixel(x, y, Color.HSVToRGB(hue, dist, 1f));
        }
        _wheelTex.Apply();
        _wheel.texture = _wheelTex;
    }

    static void AddTrigger(EventTrigger et, EventTriggerType type,
                           UnityEngine.Events.UnityAction<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(cb);
        et.triggers.Add(entry);
    }

    void OnDestroy() { if (_wheelTex) Destroy(_wheelTex); }
}
