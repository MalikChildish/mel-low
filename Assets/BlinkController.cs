using System.Collections;
using UnityEngine;

public class BlinkController : MonoBehaviour
{
    public GameObject leftEye;
    public GameObject rightEye;
    public GameObject blink;

    [Range(1f, 20f)] public float minBlinkInterval = 2f;
    [Range(3f, 20f)] public float maxBlinkInterval = 6f;
    [Range(0.05f, 0.2f)] public float blinkDuration = 0.12f;

    bool _blinking;

    void Start()
    {
        if (blink) blink.SetActive(false);
        StartCoroutine(BlinkLoop());
    }

    IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkInterval, maxBlinkInterval));
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

    void SetEyesVisible(bool visible)
    {
        if (leftEye)  leftEye.SetActive(visible);
        if (rightEye) rightEye.SetActive(visible);
    }
}
