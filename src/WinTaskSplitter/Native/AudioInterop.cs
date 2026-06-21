using System.Runtime.InteropServices;

namespace WinTaskSplitter.Native;

/// <summary>
/// Minimal Core Audio (MMDevice / EndpointVolume) COM interop to read the
/// default render device's master volume and mute state.
/// </summary>
internal static class AudioInterop
{
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
        // Remaining methods unused.
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        // Remaining methods unused.
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out int pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, IntPtr ctx);
        int SetMasterVolumeLevelScalar(float fLevel, IntPtr ctx);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, IntPtr ctx);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, IntPtr ctx);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, IntPtr ctx);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        // Remaining methods unused.
    }

    private const int eRender = 0;
    private const int eMultimedia = 1;
    private const int CLSCTX_ALL = 23;

    /// <summary>
    /// Returns the default playback device's master volume (0..100) and mute state,
    /// or null if no audio endpoint is available.
    /// </summary>
    public static (int Level, bool Muted)? GetVolume()
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioEndpointVolume? volume = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            if (enumerator.GetDefaultAudioEndpoint(eRender, eMultimedia, out device) != 0 || device is null)
                return null;

            var iid = typeof(IAudioEndpointVolume).GUID;
            if (device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out object o) != 0 || o is null)
                return null;

            volume = (IAudioEndpointVolume)o;
            volume.GetMasterVolumeLevelScalar(out float level);
            volume.GetMute(out bool muted);
            return ((int)Math.Round(level * 100), muted);
        }
        catch { return null; }
        finally
        {
            if (volume is not null)     Marshal.ReleaseComObject(volume);
            if (device is not null)     Marshal.ReleaseComObject(device);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }
}
