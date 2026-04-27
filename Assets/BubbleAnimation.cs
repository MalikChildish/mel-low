using UnityEngine;
using UnityEngine.UI;

public class BubbleAnimation : MonoBehaviour
{
    public Sprite[] frames;
    [Range(1f, 30f)] public float fps  = 12f;
    public bool              loop      = true;

    Image _img;
    float _timer;
    int   _frame;
    bool  _playing;

    void Awake()
    {
        _img = GetComponent<Image>();
        Stop();
    }

    void Update()
    {
        if (!_playing || frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer < 1f / fps) return;
        _timer = 0f;

        _frame++;
        if (_frame >= frames.Length)
        {
            _frame   = loop ? 0 : frames.Length - 1;
            _playing = loop;
        }

        _img.sprite = frames[_frame];
    }

    public void Play()
    {
        if (frames == null || frames.Length == 0) return;
        _frame   = 0;
        _timer   = 0f;
        _playing = true;
        _img.sprite = frames[0];
    }

    public void Stop()
    {
        _playing = false;
        _frame   = 0;
        if (_img && frames != null && frames.Length > 0)
            _img.sprite = frames[0];
    }
}
