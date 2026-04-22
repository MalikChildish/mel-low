using UnityEngine;
using System.Runtime.InteropServices;

public class EyeTracking : MonoBehaviour
{
    public Transform leftEye;
    public Transform rightEye;
    public Transform leftPupil;
    public Transform rightPupil;
    public Transform blink;
    [Range(1f, 20f)]  public float smoothSpeed = 8f;
    [Range(0f, 0.1f)] public float maxOffset   = 0.03f;

    Vector3 _leftInitialLocal;
    Vector3 _rightInitialLocal;
    Vector3 _blinkInitialLocal;
    bool    _initialized;

    [DllImport("user32.dll")] static extern bool GetCursorPos(out TransparentWindow.POINT lpPoint);

    void Update()
    {
        if (!_initialized)
        {
            if (leftPupil)  _leftInitialLocal  = leftPupil.localPosition;
            if (rightPupil) _rightInitialLocal = rightPupil.localPosition;
            if (blink)      _blinkInitialLocal = blink.localPosition;
            _initialized = true;
            return;
        }

        Vector3 mouseWorld = MouseToWorldPoint();
        Vector3 center = leftEye && rightEye
            ? (leftEye.position + rightEye.position) * 0.5f
            : transform.position;

        Vector3 dir      = (mouseWorld - center).normalized;
        float   offsetX  = Mathf.Clamp(Vector3.Dot(dir, Camera.main.transform.right), -1f, 1f) * maxOffset;
        float   offsetY  = Mathf.Clamp(Vector3.Dot(dir, Camera.main.transform.up),    -1f, 1f) * maxOffset;
        Vector3 screenOffset = Camera.main.transform.right * offsetX + Camera.main.transform.up * offsetY;

        if (leftPupil)
        {
            Vector3 rest = leftEye.TransformPoint(_leftInitialLocal);
            leftPupil.position = Vector3.Lerp(leftPupil.position, rest + screenOffset, Time.deltaTime * smoothSpeed);
        }
        if (rightPupil)
        {
            Vector3 rest = rightEye.TransformPoint(_rightInitialLocal);
            rightPupil.position = Vector3.Lerp(rightPupil.position, rest + screenOffset, Time.deltaTime * smoothSpeed);
        }
        if (blink)
        {
            Vector3 rest = blink.parent.TransformPoint(_blinkInitialLocal);
            blink.position = Vector3.Lerp(blink.position, rest + screenOffset, Time.deltaTime * smoothSpeed);
        }
    }

    Vector3 MouseToWorldPoint()
    {
        GetCursorPos(out TransparentWindow.POINT p);
        float screenY = Screen.height - p.Y;
        float depth   = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(new Vector3(p.X, screenY, depth));
    }
}
