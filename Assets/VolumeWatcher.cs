using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class VolumeWatcher : MonoBehaviour
{
    [Range(1, 30)] public int pollFrames = 5;

    public float Volume  { get; private set; }
    public bool  IsMuted { get; private set; }

    // ── COM GUIDs ─────────────────────────────────────────────────────────────

    static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
    static readonly Guid IID_IMMDeviceEnumerator   = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
    static readonly Guid IID_IAudioEndpointVolume  = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsCtx,
                                       ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    // ── COM interfaces ────────────────────────────────────────────────────────

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint clsCtx, IntPtr pParams,
                                   [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr p);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr p);
        [PreserveSig] int GetChannelCount(out uint n);
        [PreserveSig] int SetMasterVolumeLevel(float db, ref Guid ctx);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid ctx);
        [PreserveSig] int GetMasterVolumeLevel(out float db);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint ch, float db, ref Guid ctx);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint ch, float level, ref Guid ctx);
        [PreserveSig] int GetChannelVolumeLevel(uint ch, out float db);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint ch, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid ctx);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }

    // ── State ─────────────────────────────────────────────────────────────────

    IAudioEndpointVolume _endpoint;
    int _frameCounter;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        try
        {
            var clsid = CLSID_MMDeviceEnumerator;
            var iid   = IID_IMMDeviceEnumerator;
            CoCreateInstance(ref clsid, IntPtr.Zero, 0x17u, ref iid, out object obj);

            var enumerator = (IMMDeviceEnumerator)obj;
            enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice device); // eRender, eMultimedia

            var epIid = IID_IAudioEndpointVolume;
            device.Activate(ref epIid, 0x17u, IntPtr.Zero, out object ep);
            _endpoint = (IAudioEndpointVolume)ep;

            Poll();
        }
        catch { _endpoint = null; }
    }

    void Update()
    {
        if (_endpoint == null) return;
        if (++_frameCounter < pollFrames) return;
        _frameCounter = 0;
        Poll();
    }

    void Poll()
    {
        try
        {
            _endpoint.GetMasterVolumeLevelScalar(out float vol);
            _endpoint.GetMute(out bool muted);
            Volume  = vol;
            IsMuted = muted;
        }
        catch { _endpoint = null; }
    }
}
