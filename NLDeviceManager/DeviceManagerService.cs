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
        var builder = new StringBuilder();
        builder.AppendLine("Connected USB Devices:");
        builder.Append("================================");

        IReadOnlyList<UsbDevice> devices;
        try
        {
            devices = _usbDeviceProvider.GetUsbDevices();
        }
        catch (Exception ex)
        {
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

        builder.AppendLine();
        if (devices.Count == 0)
        {
            builder.Append("No USB devices found.");
        }
        else
        {
            builder.AppendLine();
            builder.Append($"Total USB devices found: {devices.Count}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Builds the text describing the installed printers and returns the
    /// printer names in the same order they are listed.
    /// </summary>
    public PrinterListing RenderPrinters()
    {
        var builder = new StringBuilder();
        builder.AppendLine("================================");
        builder.AppendLine("Connected Printers:");
        builder.Append("================================");

        IReadOnlyList<string> printers;
        try
        {
            printers = _printerProvider.GetInstalledPrinters();
        }
        catch (Exception ex)
        {
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

        builder.AppendLine();
        if (printers.Count == 0)
        {
            builder.Append("No printers found.");
        }
        else
        {
            builder.AppendLine();
            builder.Append($"Total printers found: {printers.Count}");
        }

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
                builder.AppendLine();
                builder.Append($"Error: Printer '{printerName}' not found.");
                return builder.ToString();
            }

            builder.AppendLine();
            builder.Append(ApplyDefaultPrinter(printerName));
        }
        catch (Exception ex)
        {
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
        var builder = new StringBuilder();
        builder.Append($"Attempting to set '{printerName}' as default printer...");

        try
        {
            builder.AppendLine();
            builder.Append(ApplyDefaultPrinter(printerName));
        }
        catch (Exception ex)
        {
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

        return $"Failed to set default printer. Error code: {_printerProvider.GetLastError()}";
    }
}
