namespace NLDeviceManager.Devices;

/// <summary>
/// Supplies the USB devices currently connected to the machine.
/// </summary>
public interface IUsbDeviceProvider
{
    IReadOnlyList<UsbDevice> GetUsbDevices();
}
