using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;

public class AvatarDrag : MonoBehaviour
{
    public Transform avatarRoot;

    public static bool IsDragging { get; private set; }

    [DllImport("user32.dll")] static extern bool  GetCursorPos(out TransparentWindow.POINT p);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

    const int VK_LBUTTON = 0x01;

    TransparentWindow.POINT _prevCursor;
    bool _prevCursorValid;
    bool _prevButtonDown;

    void Update()
    {
        if (!avatarRoot || !Camera.main) return;

        bool buttonDown  = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
        bool justPressed = buttonDown && !_prevButtonDown;
        _prevButtonDown  = buttonDown;

        GetCursorPos(out TransparentWindow.POINT cur);

        if (justPressed && IsOverAvatar())
            IsDragging = true;

        if (!buttonDown)
            IsDragging = false;

        if (IsDragging && buttonDown && _prevCursorValid)
        {
            float dx =  (cur.X - _prevCursor.X);
            float dy = -(cur.Y - _prevCursor.Y); // Win32 Y is top-down; Unity is bottom-up
            ApplyDelta(dx, dy);
        }

        _prevCursor      = cur;
        _prevCursorValid = true;
    }

    bool IsOverAvatar()
    {
#if UNITY_EDITOR
        if (Mouse.current == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
#else
        GetCursorPos(out TransparentWindow.POINT p);
        float screenY = Screen.height - p.Y;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(p.X, screenY, 0));
#endif
        return Physics.Raycast(ray);
    }

    void ApplyDelta(float dx, float dy)
    {
        float   depth = Camera.main.WorldToScreenPoint(avatarRoot.position).z;
        Vector3 a     = Camera.main.ScreenToWorldPoint(new Vector3(0,  0,  depth));
        Vector3 b     = Camera.main.ScreenToWorldPoint(new Vector3(dx, dy, depth));
        avatarRoot.position += b - a;
    }
}
