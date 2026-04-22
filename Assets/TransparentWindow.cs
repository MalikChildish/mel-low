using System;
using UnityEngine;
using System.Runtime.InteropServices;

public class TransparentWindow : MonoBehaviour
{
    public struct MARGINS
    {
        public int leftWidth, rightWidth, topHeight, bottomHeight;
    }

    [DllImport("user32.dll")] public static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll")] public static extern int BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("Dwmapi.dll")] public static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int nIndex);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const int GWL_EXSTYLE = -20;
    const long WS_EX_LAYERED = 0x00080000L;

    IntPtr hWnd;

    void Start()
    {
#if !UNITY_EDITOR
        Application.runInBackground = true;
        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { leftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        SetWindowLongPtr(hWnd, GWL_EXSTYLE, (IntPtr)WS_EX_LAYERED);
        BringWindowToTop(hWnd);

        int screenW = GetSystemMetrics(0);
        int screenH = GetSystemMetrics(1);
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, screenW, screenH, 0);
#endif
    }
}