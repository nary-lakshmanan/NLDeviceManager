using NLDeviceManager.Devices;

namespace NLDeviceManager.Tests;

/// <summary>
/// In-memory <see cref="IUsbDeviceProvider"/> that returns a preset list or
/// throws a preset exception, so USB rendering can be tested off Windows.
/// </summary>
internal sealed class FakeUsbDeviceProvider : IUsbDeviceProvider
{
    private readonly IReadOnlyList<UsbDevice> _devices;
    private readonly Exception? _exception;

    public FakeUsbDeviceProvider(IReadOnlyList<UsbDevice> devices) => _devices = devices;

    public FakeUsbDeviceProvider(Exception exception)
    {
        _devices = Array.Empty<UsbDevice>();
        _exception = exception;
    }

    public IReadOnlyList<UsbDevice> GetUsbDevices() => _exception is null ? _devices : throw _exception;
}

/// <summary>
/// In-memory <see cref="IPrinterProvider"/> that records the last printer
/// asked to be set as default and can simulate failures.
/// </summary>
internal sealed class FakePrinterProvider : IPrinterProvider
{
    private readonly IReadOnlyList<string> _printers;
    private readonly Exception? _exception;

    public FakePrinterProvider(IReadOnlyList<string> printers) => _printers = printers;

    public FakePrinterProvider(Exception exception)
    {
        _printers = Array.Empty<string>();
        _exception = exception;
    }

    public string? DefaultPrinter { get; set; }

    public bool SetDefaultPrinterResult { get; set; } = true;

    public int LastError { get; set; }

    public string? LastSetDefaultRequest { get; private set; }

    public IReadOnlyList<string> GetInstalledPrinters() =>
        _exception is null ? _printers : throw _exception;

    public bool IsDefaultPrinter(string printerName) =>
        string.Equals(printerName, DefaultPrinter, StringComparison.OrdinalIgnoreCase);

    public bool SetDefaultPrinter(string printerName)
    {
        LastSetDefaultRequest = printerName;
        return SetDefaultPrinterResult;
    }

    public int GetLastError() => LastError;
}
