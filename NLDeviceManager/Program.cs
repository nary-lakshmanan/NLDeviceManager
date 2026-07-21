using System.Management;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

bool running = true;

while (running)
{
    Console.Clear();
    Console.WriteLine("================================");
    Console.WriteLine("    Device Manager");
    Console.WriteLine("================================");
    Console.WriteLine("\nPlease select an option:");
    Console.WriteLine("1. List Connected USB Devices");
    Console.WriteLine("2. List Connected Printers");
    Console.WriteLine("3. Exit");
    Console.WriteLine("================================");
    Console.Write("\nEnter your choice (1-3): ");

    string? choice = Console.ReadLine();

    // A null result means the input stream reached end-of-file (e.g. piped
    // input or a closed console). Treat it as a request to exit instead of
    // looping forever on the "invalid choice" branch.
    if (choice is null)
    {
        Console.WriteLine("\nNo more input. Exiting...");
        break;
    }

    switch (choice)
    {
        case "1":
            ListConnectedUsbDevices();
            break;
        case "2":
            ListConnectedPrinters();
            break;
        case "3":
            running = false;
            Console.WriteLine("\nExiting...");
            continue;
        default:
            Console.WriteLine("\nInvalid choice. Please try again.");
            break;
    }

    if (running)
    {
        Console.WriteLine("\nPress any key to return to menu...");
        if (Console.IsInputRedirected)
        {
            Console.ReadLine();
        }
        else
        {
            Console.ReadKey();
        }
    }
}

return Environment.ExitCode;

// Writes an error to standard error, including full exception details so that
// diagnostics (stack trace, inner exceptions) are not lost, and flags the
// process as failed so callers/scripts can detect it via the exit code.
static void ReportError(string message, Exception? ex = null)
{
    Console.Error.WriteLine(message);
    if (ex is not null)
    {
        Console.Error.WriteLine(ex);
    }

    // Preserve a non-zero exit code once an error has occurred.
    if (Environment.ExitCode == 0)
    {
        Environment.ExitCode = 1;
    }
}

static bool ListConnectedUsbDevices()
{
    Console.WriteLine("\nConnected USB Devices:");
    Console.WriteLine("================================");

    try
    {
        using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass='USB'"))
        {
            var devices = searcher.Get();
            int count = 0;

            foreach (ManagementObject device in devices)
            {
                using (device)
                {
                    count++;
                    string? name = device["Name"]?.ToString();
                    string? deviceId = device["DeviceID"]?.ToString();
                    string? status = device["Status"]?.ToString();

                    Console.WriteLine($"\n[{count}] {name}");
                    Console.WriteLine($"    Device ID: {deviceId}");
                    Console.WriteLine($"    Status: {status}");
                }
            }

            if (count == 0)
            {
                Console.WriteLine("No USB devices found.");
            }
            else
            {
                Console.WriteLine($"\nTotal USB devices found: {count}");
            }
        }

        return true;
    }
    catch (ManagementException ex)
    {
        ReportError($"Error querying USB devices via WMI: {ex.Message}", ex);
        return false;
    }
    catch (Exception ex)
    {
        ReportError($"Error enumerating USB devices: {ex.Message}", ex);
        return false;
    }
}

static bool ListConnectedPrinters()
{
    Console.WriteLine("\n================================");
    Console.WriteLine("Connected Printers:");
    Console.WriteLine("================================");

    try
    {
        var printers = PrinterSettings.InstalledPrinters;
        var printerList = new List<string>();
        int count = 0;

        foreach (string printer in printers)
        {
            count++;
            printerList.Add(printer);
            var settings = new PrinterSettings { PrinterName = printer };
            bool isDefault = settings.IsDefaultPrinter;
            Console.WriteLine($"\n[{count}] {printer}{(isDefault ? " (Default)" : "")}");
        }

        if (count == 0)
        {
            Console.WriteLine("No printers found.");
            return true;
        }

        Console.WriteLine($"\nTotal printers found: {count}");

        // Ask if user wants to set a default printer
        Console.Write("\nWould you like to set a default printer? (y/n): ");
        string? response = Console.ReadLine();

        if (response?.Trim().ToLower() == "y")
        {
            Console.Write($"Enter printer number (1-{count}) to set as default: ");
            string? input = Console.ReadLine();

            if (int.TryParse(input, out int printerNumber) && printerNumber >= 1 && printerNumber <= count)
            {
                string selectedPrinter = printerList[printerNumber - 1];
                return SetDefaultPrinterByNumber(selectedPrinter);
            }

            ReportError($"Invalid printer number: '{input}'.");
            return false;
        }

        return true;
    }
    catch (Exception ex)
    {
        ReportError($"Error listing printers: {ex.Message}", ex);
        return false;
    }
}

static bool SetDefaultPrinterByNumber(string printerName)
{
    Console.WriteLine($"\nAttempting to set '{printerName}' as default printer...");

    try
    {
        // Use Windows API to set default printer
        if (SetDefaultPrinterWin32(printerName))
        {
            Console.WriteLine($"Successfully set '{printerName}' as the default printer.");
            return true;
        }

        int errorCode = Marshal.GetLastWin32Error();
        ReportError($"Failed to set default printer. Error code: {errorCode}",
            new System.ComponentModel.Win32Exception(errorCode));
        return false;
    }
    catch (Exception ex)
    {
        ReportError($"Error setting default printer: {ex.Message}", ex);
        return false;
    }
}

[DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetDefaultPrinter")]
static extern bool SetDefaultPrinterWin32(string printerName);
