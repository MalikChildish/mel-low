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

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const int  GWL_EXSTYLE              = -20;
    const long WS_EX_LAYERED            = 0x00080000L;
    const long WS_EX_TRANSPARENT        = 0x00000020L;

    IntPtr _hWnd;
    bool   _clickThrough;

    void Start()
    {
#if !UNITY_EDITOR
        Application.runInBackground = true;
        _hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { leftWidth = -1 };
        DwmExtendFrameIntoClientArea(_hWnd, ref margins);
        BringWindowToTop(_hWnd);

        int screenW = GetSystemMetrics(0);
        int screenH = GetSystemMetrics(1);
        SetWindowPos(_hWnd, HWND_TOPMOST, 0, 0, screenW, screenH, 0);

        SetClickThrough(true);
#endif
    }

    void Update()
    {
#if !UNITY_EDITOR
        GetCursorPos(out POINT p);
        float screenY = Screen.height - p.Y;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(p.X, screenY, 0));
        bool overAvatar = Physics.Raycast(ray);
        if (overAvatar != !_clickThrough)
            SetClickThrough(!overAvatar);
#endif
    }

    void SetClickThrough(bool on)
    {
        _clickThrough = on;
        long style = WS_EX_LAYERED | (on ? WS_EX_TRANSPARENT : 0);
        SetWindowLongPtr(_hWnd, GWL_EXSTYLE, (IntPtr)style);
    }
}
