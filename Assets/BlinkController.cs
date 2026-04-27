using System.Collections;
using UnityEngine;

public class BlinkController : MonoBehaviour
{
    [Header("References")]
    public GameObject leftEye;
    public GameObject rightEye;
    public GameObject blink;

    [Header("Blink")]
    [Range(1f, 20f)]   public float minBlinkInterval = 2f;
    [Range(3f, 20f)]   public float maxBlinkInterval = 6f;
    [Range(0.05f, 0.2f)] public float blinkDuration  = 0.12f;

    [Header("Happy Brows")]
    public GameObject   happyBrows;
    public BeatDetector beatDetector;
    [Range(0f,   1f)]  public float happyBrowsChance  = 0.5f;
    [Range(5f,  60f)]  public float happyBrowsMinRoll = 15f;
    [Range(10f, 120f)] public float happyBrowsMaxRoll = 35f;
    [Range(2f,  15f)]  public float happyBrowsMinTime = 4f;
    [Range(5f,  20f)]  public float happyBrowsMaxTime = 12f;

    bool  _blinking;
    float _happyBrowsRollTimer;
    float _happyBrowsActiveTimer;
    bool  _happyBrowsActive;

    void Start()
    {
        if (blink)       blink.SetActive(false);
        if (happyBrows)  happyBrows.SetActive(false);
        _happyBrowsRollTimer = Random.Range(happyBrowsMinRoll, happyBrowsMaxRoll);
        StartCoroutine(BlinkLoop());
    }

    void Update()
    {
        UpdateHappyBrows();
    }

    // ── Blink ─────────────────────────────────────────────────────────────────

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkInterval, maxBlinkInterval));
            if (!_happyBrowsActive)
                yield return StartCoroutine(DoBlink());
        }
    }

    IEnumerator DoBlink()
    {
        _blinking = true;

        SetEyesVisible(false);
        if (blink) blink.SetActive(true);

        yield return new WaitForSeconds(blinkDuration);

        if (blink) blink.SetActive(false);
        SetEyesVisible(true);

        _blinking = false;
    }

    void OnMouseDown()
    {
        if (!_blinking && !_happyBrowsActive) StartCoroutine(DoBlink());
    }

    void SetEyesVisible(bool visible)
    {
        if (leftEye)  leftEye.SetActive(visible);
        if (rightEye) rightEye.SetActive(visible);
    }

    // ── Happy Brows ───────────────────────────────────────────────────────────

    void UpdateHappyBrows()
    {
        bool isNodding = beatDetector != null
                      && beatDetector.reactToMusic
                      && beatDetector.Energy > 0.01f
                      && !AvatarDrag.IsDragging;

        if (_happyBrowsActive)
        {
            _happyBrowsActiveTimer -= Time.deltaTime;
            if (_happyBrowsActiveTimer <= 0f || !isNodding)
                SetHappyBrows(false);
            return;
        }

        if (!isNodding)
        {
            _happyBrowsRollTimer = Random.Range(happyBrowsMinRoll, happyBrowsMaxRoll);
            return;
        }

        _happyBrowsRollTimer -= Time.deltaTime;
        if (_happyBrowsRollTimer > 0f) return;

        _happyBrowsRollTimer = Random.Range(happyBrowsMinRoll, happyBrowsMaxRoll);

        if (Random.value <= happyBrowsChance)
            SetHappyBrows(true);
    }

    void SetHappyBrows(bool active)
    {
        _happyBrowsActive = active;

        if (happyBrows) happyBrows.SetActive(active);
        SetEyesVisible(!active);

        if (active)
            _happyBrowsActiveTimer = Random.Range(happyBrowsMinTime, happyBrowsMaxTime);
    }
}
