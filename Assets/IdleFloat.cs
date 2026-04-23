using UnityEngine;

public class IdleFloat : MonoBehaviour
{
    [Range(0f, 0.1f)] public float amplitude = 0.02f;
    [Range(0.1f, 2f)] public float speed = 0.8f;

    Vector3 _initialPos;
    bool _initialized;

    void Update()
    {
        if (!_initialized)
        {
            _initialPos = transform.position;
            _initialized = true;
            return;
        }

        if (AvatarDrag.IsDragging)
        {
            // Track position while dragging so float resumes from new spot
            _initialPos = transform.position;
            return;
        }

        float offset = Mathf.Sin(Time.time * speed) * amplitude;
        transform.position = _initialPos + new Vector3(0f, offset, 0f);
    }
}
