using System.Management;
using System.Runtime.Versioning;

namespace NLDeviceManager.Devices;

/// <summary>
/// Enumerates USB devices via WMI (<c>Win32_PnPEntity</c>). Windows only.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsUsbDeviceProvider : IUsbDeviceProvider
{
    public IReadOnlyList<UsbDevice> GetUsbDevices()
    {
        var results = new List<UsbDevice>();

        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass='USB'");
        foreach (ManagementObject device in searcher.Get())
        {
            using (device)
            {
                results.Add(new UsbDevice(
                    device["Name"]?.ToString(),
                    device["DeviceID"]?.ToString(),
                    device["Status"]?.ToString()));
            }
        }

        return results;
    }
}
