using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NLDeviceManager.Devices;

/// <summary>
/// Accesses installed printers via <see cref="PrinterSettings"/> and sets the
/// default printer through the Win32 spooler API. Windows only.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsPrinterProvider : IPrinterProvider
{
    public IReadOnlyList<string> GetInstalledPrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }

        return printers;
    }

    public bool IsDefaultPrinter(string printerName)
    {
        var settings = new PrinterSettings { PrinterName = printerName };
        return settings.IsDefaultPrinter;
    }

    public bool SetDefaultPrinter(string printerName) => SetDefaultPrinterWin32(printerName);

    public int GetLastError() => Marshal.GetLastWin32Error();

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetDefaultPrinter")]
    private static extern bool SetDefaultPrinterWin32(string printerName);
}
