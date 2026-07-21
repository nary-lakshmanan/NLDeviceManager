using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace NLDeviceManager;

/// <summary>
/// Helpers for working with installed printers and the Windows default printer.
/// </summary>
internal static class PrinterManager
{
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetDefaultPrinter")]
    private static extern bool SetDefaultPrinterWin32(string printerName);

    /// <summary>
    /// Sets the default printer after verifying that it is installed (matching case-insensitively).
    /// </summary>
    public static void SetDefaultPrinter(string printerName)
    {
        try
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyDefaultPrinter(printer); // Use exact casing
                    return;
                }
            }

            Console.WriteLine($"Error: Printer '{printerName}' not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting default printer: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the default printer to a name that has already been resolved from the installed list.
    /// </summary>
    public static void SetDefaultPrinterByNumber(string printerName) => ApplyDefaultPrinter(printerName);

    private static void ApplyDefaultPrinter(string printerName)
    {
        Console.WriteLine($"\nAttempting to set '{printerName}' as default printer...");

        try
        {
            if (SetDefaultPrinterWin32(printerName))
            {
                Console.WriteLine($"Successfully set '{printerName}' as the default printer.");
            }
            else
            {
                Console.WriteLine($"Failed to set default printer. Error code: {Marshal.GetLastWin32Error()}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting default printer: {ex.Message}");
        }
    }
}
