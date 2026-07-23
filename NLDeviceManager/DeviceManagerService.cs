using System.Text;
using NLDeviceManager.Devices;

namespace NLDeviceManager;

/// <summary>
/// The action selected from the main menu.
/// </summary>
public enum MenuAction
{
    ListUsbDevices,
    ListPrinters,
    Exit,
    Invalid,
}

/// <summary>
/// The rendered printer list together with the printer names it describes.
/// </summary>
public sealed record PrinterListing(string Output, IReadOnlyList<string> Printers);

/// <summary>
/// Holds the platform-independent presentation and validation logic for the
/// device manager. All device/printer access is delegated to the injected
/// providers so the logic can be exercised without touching real hardware.
/// </summary>
public sealed class DeviceManagerService
{
    private readonly IUsbDeviceProvider _usbDeviceProvider;
    private readonly IPrinterProvider _printerProvider;

    public DeviceManagerService(IUsbDeviceProvider usbDeviceProvider, IPrinterProvider printerProvider)
    {
        _usbDeviceProvider = usbDeviceProvider;
        _printerProvider = printerProvider;
    }

    /// <summary>
    /// Whether the most recent operation completed successfully. It is false
    /// when a provider threw or an operation could not be completed (printer
    /// not found, or the OS refused to set the default printer). Rendering
    /// methods still return user-facing text on failure; this lets the caller
    /// distinguish success from failure and set a non-zero exit code.
    /// </summary>
    public bool LastOperationSucceeded { get; private set; } = true;

    /// <summary>
    /// The exception thrown by the most recent operation, if any. Provider
    /// exceptions are swallowed into the rendered output (only the message is
    /// shown to the user); this preserves the full exception so the caller can
    /// log the stack trace and inner exceptions instead of losing them.
    /// </summary>
    public Exception? LastError { get; private set; }

    /// <summary>
    /// Maps a raw menu selection to the action it represents.
    /// </summary>
    public static MenuAction ParseMenuChoice(string? choice) => choice switch
    {
        "1" => MenuAction.ListUsbDevices,
        "2" => MenuAction.ListPrinters,
        "3" => MenuAction.Exit,
        _ => MenuAction.Invalid,
    };

    /// <summary>
    /// Builds the text describing the connected USB devices.
    /// </summary>
    public string RenderUsbDevices()
    {
        ResetLastOperation();

        var builder = new StringBuilder();
        builder.AppendLine("Connected USB Devices:");
        builder.Append(ConsoleUi.Separator);

        IReadOnlyList<UsbDevice> devices;
        try
        {
            devices = _usbDeviceProvider.GetUsbDevices();
        }
        catch (Exception ex)
        {
            Fail(ex);
            builder.AppendLine();
            builder.Append($"Error enumerating USB devices: {ex.Message}");
            return builder.ToString();
        }

        for (int i = 0; i < devices.Count; i++)
        {
            UsbDevice device = devices[i];
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine($"[{i + 1}] {device.Name}");
            builder.AppendLine($"    Device ID: {device.DeviceId}");
            builder.Append($"    Status: {device.Status}");
        }

        ConsoleUi.AppendCountSummary(builder, devices.Count, "USB devices");

        return builder.ToString();
    }

    /// <summary>
    /// Builds the text describing the installed printers and returns the
    /// printer names in the same order they are listed.
    /// </summary>
    public PrinterListing RenderPrinters()
    {
        ResetLastOperation();

        var builder = new StringBuilder();
        builder.AppendLine(ConsoleUi.Separator);
        builder.AppendLine("Connected Printers:");
        builder.Append(ConsoleUi.Separator);

        IReadOnlyList<string> printers;
        try
        {
            printers = _printerProvider.GetInstalledPrinters();
        }
        catch (Exception ex)
        {
            Fail(ex);
            builder.AppendLine();
            builder.Append($"Error listing printers: {ex.Message}");
            return new PrinterListing(builder.ToString(), Array.Empty<string>());
        }

        for (int i = 0; i < printers.Count; i++)
        {
            string printer = printers[i];
            bool isDefault = _printerProvider.IsDefaultPrinter(printer);
            builder.AppendLine();
            builder.Append($"[{i + 1}] {printer}{(isDefault ? " (Default)" : string.Empty)}");
        }

        ConsoleUi.AppendCountSummary(builder, printers.Count, "printers");

        return new PrinterListing(builder.ToString(), printers);
    }

    /// <summary>
    /// Validates a user-entered printer number against the available count.
    /// A valid entry yields the zero-based index into the printer list.
    /// </summary>
    public static bool TryParsePrinterSelection(string? input, int printerCount, out int index)
    {
        if (int.TryParse(input, out int printerNumber) && printerNumber >= 1 && printerNumber <= printerCount)
        {
            index = printerNumber - 1;
            return true;
        }

        index = -1;
        return false;
    }

    /// <summary>
    /// Sets the default printer after confirming the printer exists, matching
    /// on name case-insensitively and preserving the installed casing.
    /// </summary>
    public string SetDefaultPrinter(string printerName)
    {
        ResetLastOperation();

        var builder = new StringBuilder();
        builder.Append($"Attempting to set '{printerName}' as default printer...");

        try
        {
            bool printerExists = false;
            foreach (string printer in _printerProvider.GetInstalledPrinters())
            {
                if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                {
                    printerExists = true;
                    printerName = printer;
                    break;
                }
            }

            if (!printerExists)
            {
                Fail();
                builder.AppendLine();
                builder.Append($"Error: Printer '{printerName}' not found.");
                return builder.ToString();
            }

            builder.AppendLine();
            builder.Append(ApplyDefaultPrinter(printerName));
        }
        catch (Exception ex)
        {
            Fail(ex);
            builder.AppendLine();
            builder.Append($"Error setting default printer: {ex.Message}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Sets the default printer without an existence check, used once a
    /// printer has already been selected from the listed set.
    /// </summary>
    public string SetDefaultPrinterByNumber(string printerName)
    {
        ResetLastOperation();

        var builder = new StringBuilder();
        builder.Append($"Attempting to set '{printerName}' as default printer...");

        try
        {
            builder.AppendLine();
            builder.Append(ApplyDefaultPrinter(printerName));
        }
        catch (Exception ex)
        {
            Fail(ex);
            builder.AppendLine();
            builder.Append($"Error setting default printer: {ex.Message}");
        }

        return builder.ToString();
    }

    private string ApplyDefaultPrinter(string printerName)
    {
        if (_printerProvider.SetDefaultPrinter(printerName))
        {
            return $"Successfully set '{printerName}' as the default printer.";
        }

        Fail();
        return $"Failed to set default printer. Error code: {_printerProvider.GetLastError()}";
    }

    private void ResetLastOperation()
    {
        LastOperationSucceeded = true;
        LastError = null;
    }

    private void Fail(Exception? error = null)
    {
        LastOperationSucceeded = false;
        LastError = error;
    }
}
