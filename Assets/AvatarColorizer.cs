using UnityEngine;
using UnityEngine.UI;

public class AvatarColorizer : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] Renderer _beanieRenderer;
    [SerializeField] Renderer _hoodieRenderer;
    [SerializeField] Renderer _headRenderer;
    [SerializeField] Renderer _neckRenderer;

    [Header("Swatches")]
    [SerializeField] Button _beanieButton;
    [SerializeField] Button _hoodieButton;
    [SerializeField] Button _headButton;

    [Header("Picker")]
    [SerializeField] ColorPickerPopup _picker;

    static readonly Color DefaultBeanie = new Color(0.49f, 0.847f, 0.91f);  // #7DD8E8
    static readonly Color DefaultHoodie = new Color(0.361f, 0.42f, 0.18f);  // #5C6B2E
    static readonly Color DefaultHead   = new Color(0.231f, 0.122f, 0.055f); // #3B1F0E

    void Start()
    {
        Apply(_beanieRenderer, LoadColor("color_beanie", DefaultBeanie));
        Apply(_hoodieRenderer, LoadColor("color_hoodie", DefaultHoodie));
        Color head = LoadColor("color_head", DefaultHead);
        Apply(_headRenderer, head);
        Apply(_neckRenderer, head);

        RefreshSwatch(_beanieButton, LoadColor("color_beanie", DefaultBeanie));
        RefreshSwatch(_hoodieButton, LoadColor("color_hoodie", DefaultHoodie));
        RefreshSwatch(_headButton,   LoadColor("color_head",   DefaultHead));

        _beanieButton?.onClick.AddListener(() => OpenPicker("color_beanie", DefaultBeanie, _beanieRenderer, null,          _beanieButton));
        _hoodieButton?.onClick.AddListener(() => OpenPicker("color_hoodie", DefaultHoodie, _hoodieRenderer, null,          _hoodieButton));
        _headButton?.onClick.AddListener(()   => OpenPicker("color_head",   DefaultHead,   _headRenderer,   _neckRenderer, _headButton));
    }

    void OpenPicker(string key, Color defaultColor, Renderer primary, Renderer secondary, Button swatch)
    {
        if (!primary || !_picker) return;
        _picker.Show(primary.material.color, defaultColor, c =>
        {
            Apply(primary, c);
            if (secondary) Apply(secondary, c);
            RefreshSwatch(swatch, c);
            SaveColor(key, c);
        });
    }

    void Apply(Renderer r, Color c) { if (r) r.material.color = c; }

    void RefreshSwatch(Button b, Color c) { if (b) b.GetComponent<Image>().color = c; }

    Color LoadColor(string key, Color fallback) => new Color(
        PlayerPrefs.GetFloat(key + "_r", fallback.r),
        PlayerPrefs.GetFloat(key + "_g", fallback.g),
        PlayerPrefs.GetFloat(key + "_b", fallback.b));

    void SaveColor(string key, Color c)
    {
        PlayerPrefs.SetFloat(key + "_r", c.r);
        PlayerPrefs.SetFloat(key + "_g", c.g);
        PlayerPrefs.SetFloat(key + "_b", c.b);
        PlayerPrefs.Save();
    }
}
