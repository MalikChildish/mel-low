using UnityEngine;

public class AvatarAnchor : MonoBehaviour
{
    [Range(0f, 1f)] public float viewportX = 0.85f;
    [Range(0f, 1f)] public float viewportY = 0.2f;

    void Start()
    {
        Camera cam = Camera.main;
        float dist = Vector3.Distance(cam.transform.position, transform.position);
        transform.position = cam.ViewportToWorldPoint(new Vector3(viewportX, viewportY, dist));
    }
}
