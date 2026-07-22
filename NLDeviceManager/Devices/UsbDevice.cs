namespace NLDeviceManager.Devices;

/// <summary>
/// A connected USB device as reported by the operating system.
/// </summary>
public sealed record UsbDevice(string? Name, string? DeviceId, string? Status);
