using UnityEngine;
using UnityEngine.InputSystem;

public class EyeTracking : MonoBehaviour
{
    public Transform leftEye;
    public Transform rightEye;
    public Transform leftPupil;
    public Transform rightPupil;
    public Transform blink;
    [Range(1f, 20f)]  public float smoothSpeed = 8f;
    [Range(0f, 0.1f)] public float maxOffset   = 0.03f;

    Vector3 _leftInitialWorld;
    Vector3 _rightInitialWorld;
    Vector3 _blinkInitialWorld;
    bool _initialized;

    void Update()
    {
        if (!_initialized)
        {
            if (leftPupil)  _leftInitialWorld  = leftPupil.position;
            if (rightPupil) _rightInitialWorld = rightPupil.position;
            if (blink)      _blinkInitialWorld = blink.position;
            _initialized = true;
            return;
        }

        Vector3 mouseWorld = MouseToWorldPoint();
        Vector3 center = leftEye && rightEye
            ? (leftEye.position + rightEye.position) * 0.5f
            : transform.position;

        Vector3 dir = (mouseWorld - center).normalized;
        float offsetX = Mathf.Clamp(dir.x, -1f, 1f) * maxOffset;
        float offsetY = Mathf.Clamp(dir.y, -1f, 1f) * maxOffset;

        if (leftPupil)
        {
            Vector3 target = new Vector3(_leftInitialWorld.x + offsetX, _leftInitialWorld.y + offsetY, _leftInitialWorld.z);
            leftPupil.position = Vector3.Lerp(leftPupil.position, target, Time.deltaTime * smoothSpeed);
        }
        if (rightPupil)
        {
            Vector3 target = new Vector3(_rightInitialWorld.x + offsetX, _rightInitialWorld.y + offsetY, _rightInitialWorld.z);
            rightPupil.position = Vector3.Lerp(rightPupil.position, target, Time.deltaTime * smoothSpeed);
        }
        if (blink)
        {
            Vector3 target = new Vector3(_blinkInitialWorld.x + offsetX, _blinkInitialWorld.y + offsetY, _blinkInitialWorld.z);
            blink.position = Vector3.Lerp(blink.position, target, Time.deltaTime * smoothSpeed);
        }
    }

    Vector3 MouseToWorldPoint()
    {
        float depth = Camera.main.WorldToScreenPoint(transform.position).z;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        return Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, depth));
    }
}
