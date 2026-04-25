using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class TransparentWindow : MonoBehaviour
{
    public struct MARGINS
    {
        public int leftWidth, rightWidth, topHeight, bottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }

    [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] static extern int BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("Dwmapi.dll")] static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int nIndex);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT lpPoint);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const int  GWL_EXSTYLE              = -20;
    const long WS_EX_LAYERED            = 0x00080000L;
    const long WS_EX_TRANSPARENT        = 0x00000020L;

    static IntPtr _hwnd;
    bool          _clickThrough;
    SettingsUI    _settingsUI;

    public static Vector2 CursorWindowPos()
    {
        GetCursorPos(out POINT p);
        if (_hwnd != IntPtr.Zero) ScreenToClient(_hwnd, ref p);
        return new Vector2(p.X, Screen.height - p.Y);
    }

    void Start()
    {
#if !UNITY_EDITOR
        Application.runInBackground = true;
        _hwnd = GetActiveWindow();

        MARGINS margins = new MARGINS { leftWidth = -1 };
        DwmExtendFrameIntoClientArea(_hwnd, ref margins);
        BringWindowToTop(_hwnd);

        int screenW = GetSystemMetrics(0);
        int screenH = GetSystemMetrics(1);
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, screenW, screenH, 0);

        SetClickThrough(true);
        _settingsUI = FindObjectOfType<SettingsUI>();
#endif
    }

    void Update()
    {
#if !UNITY_EDITOR
        Vector2 cp  = CursorWindowPos();
        Ray     ray = Camera.main.ScreenPointToRay(new Vector3(cp.x, cp.y, 0));
        bool overAvatar = Physics.Raycast(ray)
                       || (_settingsUI != null && _settingsUI.IsPointerOverUI())
                       || AvatarDrag.IsDragging;
        if (overAvatar != !_clickThrough)
            SetClickThrough(!overAvatar);
#endif
    }

    void OnApplicationQuit() => PlayerPrefs.Save();

    void SetClickThrough(bool on)
    {
        _clickThrough = on;
        long style = WS_EX_LAYERED | (on ? WS_EX_TRANSPARENT : 0);
        SetWindowLongPtr(_hwnd, GWL_EXSTYLE, (IntPtr)style);
    }
}
