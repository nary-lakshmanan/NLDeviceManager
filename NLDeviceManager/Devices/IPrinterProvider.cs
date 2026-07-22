namespace NLDeviceManager.Devices;

/// <summary>
/// Abstracts access to the machine's installed printers.
/// </summary>
public interface IPrinterProvider
{
    IReadOnlyList<string> GetInstalledPrinters();

    bool IsDefaultPrinter(string printerName);

    bool SetDefaultPrinter(string printerName);

    int GetLastError();
}
