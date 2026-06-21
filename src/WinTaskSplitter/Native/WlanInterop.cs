using System.Runtime.InteropServices;

namespace WinTaskSplitter.Native;

/// <summary>
/// Minimal P/Invoke wrapper around wlanapi.dll to read the current WLAN signal quality.
/// </summary>
internal static class WlanInterop
{
    [DllImport("wlanapi.dll")]
    private static extern int WlanOpenHandle(uint dwClientVersion, IntPtr pReserved,
        out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

    [DllImport("wlanapi.dll")]
    private static extern int WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern int WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved,
        out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll")]
    private static extern int WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid,
        int OpCode, IntPtr pReserved, out int pdwDataSize, out IntPtr ppData, IntPtr pWlanOpCodeValueType);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);

    private const int wlan_intf_opcode_current_connection = 7;
    private const int wlan_interface_state_connected = 1;

    // CharSet.Unicode is required: strInterfaceDescription is WCHAR[256] (512 bytes).
    // Without it the struct size is miscalculated and isState is read from the wrong offset.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WLAN_INTERFACE_INFO
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strInterfaceDescription;
        public int isState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DOT11_SSID
    {
        public uint uSSIDLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ucSSID;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WLAN_ASSOCIATION_ATTRIBUTES
    {
        public DOT11_SSID dot11Ssid;
        public int        dot11BssType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public byte[]     dot11Bssid;
        public int        dot11PhyType;
        public uint       uDot11PhyIndex;
        public uint       wlanSignalQuality; // 0..100
        public uint       ulRxRate;
        public uint       ulTxRate;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WLAN_CONNECTION_ATTRIBUTES
    {
        public int isState;
        public int wlanConnectionMode;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strProfileName;
        public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
        // Trailing wlanSecurityAttributes intentionally omitted — not read here.
    }

    /// <summary>Connected, but signal quality is unavailable (Location Services off).</summary>
    public const int ConnectedUnknownSignal = -1;

    /// <summary>
    /// WLAN status of the first connected interface:
    ///   null  → no WLAN adapter or not connected,
    ///   -1    → connected but signal unknown (Windows gates SSID/signal behind Location Services),
    ///   0..100 → actual signal quality.
    /// The connected/disconnected state comes from WlanEnumInterfaces, which does not
    /// require Location permission; only the graduated signal does.
    /// </summary>
    public static int? GetSignalQuality()
    {
        IntPtr handle = IntPtr.Zero;
        IntPtr ifaceList = IntPtr.Zero;
        try
        {
            if (WlanOpenHandle(2, IntPtr.Zero, out _, out handle) != 0)
                return null;

            if (WlanEnumInterfaces(handle, IntPtr.Zero, out ifaceList) != 0)
                return null;

            // WLAN_INTERFACE_INFO_LIST: dwNumberOfItems (uint), dwIndex (uint), then array.
            int count = Marshal.ReadInt32(ifaceList);
            IntPtr arrayStart = ifaceList + 8;
            int infoSize = Marshal.SizeOf<WLAN_INTERFACE_INFO>();

            bool anyConnected = false;
            for (int i = 0; i < count; i++)
            {
                var info = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(arrayStart + i * infoSize);
                if (info.isState != wlan_interface_state_connected)
                    continue;

                anyConnected = true;

                // Signal needs Location permission; ACCESS_DENIED here is expected when it's off.
                var guid = info.InterfaceGuid;
                if (WlanQueryInterface(handle, ref guid, wlan_intf_opcode_current_connection,
                        IntPtr.Zero, out _, out IntPtr pData, IntPtr.Zero) != 0 || pData == IntPtr.Zero)
                    continue;

                try
                {
                    var conn = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(pData);
                    return (int)conn.wlanAssociationAttributes.wlanSignalQuality;
                }
                finally { WlanFreeMemory(pData); }
            }

            return anyConnected ? ConnectedUnknownSignal : null;
        }
        catch { return null; }
        finally
        {
            if (ifaceList != IntPtr.Zero) WlanFreeMemory(ifaceList);
            if (handle != IntPtr.Zero) WlanCloseHandle(handle, IntPtr.Zero);
        }
    }
}
