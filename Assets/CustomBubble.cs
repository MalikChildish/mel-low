using TMPro;
using UnityEngine;

public class CustomBubble : MonoBehaviour
{
    [SerializeField] BubbleAnimation _normal;
    [SerializeField] BubbleAnimation _thought;
    [SerializeField] BubbleAnimation _hype;
    [SerializeField] TMP_Text        _text;

    void Awake() => Hide();

    public void Show(BubbleType type, string message)
    {
        Hide();
        BubbleAnimation anim = type == BubbleType.Thought ? _thought
                             : type == BubbleType.Hype    ? _hype
                             : _normal;
        if (anim) { anim.gameObject.SetActive(true); anim.Play(); }
        if (_text) { _text.gameObject.SetActive(true); _text.text = message; }
    }

    public void Hide()
    {
        if (_normal)  _normal.gameObject.SetActive(false);
        if (_thought) _thought.gameObject.SetActive(false);
        if (_hype)    _hype.gameObject.SetActive(false);
        if (_text)    _text.gameObject.SetActive(false);
    }
}
