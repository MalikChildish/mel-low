using UnityEngine;
using System.Runtime.InteropServices;

public class HeadTilt : MonoBehaviour
{
    public Transform headBone;
    [Range(1f, 20f)]  public float smoothSpeed = 3f;
    [Range(0f, 30f)]  public float maxTilt     = 12f;
    [Range(0f, 30f)]  public float maxTurn     = 15f;
    [Range(0f, 30f)]  public float maxNod      = 10f;

    [DllImport("user32.dll")] static extern bool GetCursorPos(out TransparentWindow.POINT p);

    Quaternion _initialRot;
    bool       _initialized;

    void Update()
    {
        if (!headBone) return;

        if (!_initialized)
        {
            _initialRot  = headBone.localRotation;
            _initialized = true;
            return;
        }

        GetCursorPos(out TransparentWindow.POINT p);
        float screenCenterX = Screen.width  * 0.5f;
        float screenCenterY = Screen.height * 0.5f;

        float normX = Mathf.Clamp((p.X - screenCenterX) / screenCenterX, -1f, 1f);
        float normY = Mathf.Clamp((screenCenterY - p.Y) / screenCenterY, -1f, 1f);

        float tilt = normX  * maxTilt;
        float turn = -normX * maxTurn;
        float nod  = -normY * maxNod;

        Quaternion target = _initialRot * Quaternion.Euler(nod, turn, tilt);
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, target, Time.deltaTime * smoothSpeed);
    }
}
