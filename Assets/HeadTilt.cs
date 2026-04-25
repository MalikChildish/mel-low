using UnityEngine;

public class HeadTilt : MonoBehaviour
{
    public Transform    headBone;
    public BeatDetector beatDetector;
    [Range(1f, 20f)]  public float smoothSpeed    = 3f;
    [Range(0f, 30f)]  public float maxTilt        = 12f;
    [Range(0f, 30f)]  public float maxTurn        = 15f;
    [Range(0f, 30f)]  public float maxNod         = 10f;
    [Range(0f, 10f)]  public float beatNodScale   = 4f;
    [Range(0f, 300f)] public float proximityRadius = 80f;

    Quaternion _initialRot;
    Quaternion _mouseRot;
    bool       _initialized;

    void Update()
    {
        if (!headBone) return;

        if (!_initialized)
        {
            _initialRot = headBone.localRotation;
            _mouseRot   = _initialRot;
            _initialized = true;
            return;
        }

        Vector2 cp           = TransparentWindow.CursorWindowPos();
        Vector2 avatarScreen = Camera.main.WorldToScreenPoint(headBone.position);
        float   influence    = Mathf.Clamp01(Vector2.Distance(cp, avatarScreen) / Mathf.Max(proximityRadius, 1f));

        float screenCenterX = Screen.width  * 0.5f;
        float screenCenterY = Screen.height * 0.5f;
        float normX = Mathf.Clamp((cp.x - screenCenterX) / screenCenterX, -1f, 1f);
        float normY = Mathf.Clamp((screenCenterY - cp.y) / screenCenterY, -1f, 1f);

        float tilt = normX  * maxTilt * influence;
        float turn = -normX * maxTurn * influence;
        float nod  = -normY * maxNod  * influence;

        Quaternion mouseTarget = _initialRot * Quaternion.Euler(nod, turn, tilt);
        _mouseRot = Quaternion.Slerp(_mouseRot, mouseTarget, Time.deltaTime * smoothSpeed);

        float beatNod  = beatDetector ? beatDetector.HeadNodAngle * beatNodScale : 0f;
        float beatTurn = beatDetector ? beatDetector.HeadNodTurn  * beatNodScale : 0f;
        headBone.localRotation = _mouseRot * Quaternion.Euler(beatNod, beatTurn, -beatTurn * 0.5f);
    }
}
