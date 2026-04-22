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
        Console.ReadKey();
    }
}

static void ListConnectedUsbDevices()
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
                count++;
                string? name = device["Name"]?.ToString();
                string? deviceId = device["DeviceID"]?.ToString();
                string? status = device["Status"]?.ToString();

                Console.WriteLine($"\n[{count}] {name}");
                Console.WriteLine($"    Device ID: {deviceId}");
                Console.WriteLine($"    Status: {status}");
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error enumerating USB devices: {ex.Message}");
    }
}

static void ListConnectedPrinters()
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
        }
        else
        {
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
                    SetDefaultPrinterByNumber(selectedPrinter);
                }
                else
                {
                    Console.WriteLine("Invalid printer number.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error listing printers: {ex.Message}");
    }
}

static void SetDefaultPrinter(string printerName)
{
    Console.WriteLine($"\nAttempting to set '{printerName}' as default printer...");

    try
    {
        // Verify the printer exists
        bool printerExists = false;
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
            {
                printerExists = true;
                printerName = printer; // Use exact casing
                break;
            }
        }

        if (!printerExists)
        {
            Console.WriteLine($"Error: Printer '{printerName}' not found.");
            return;
        }

        // Use Windows API to set default printer
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

static void SetDefaultPrinterByNumber(string printerName)
{
    Console.WriteLine($"\nAttempting to set '{printerName}' as default printer...");

    try
    {
        // Use Windows API to set default printer
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

[DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "SetDefaultPrinter")]
static extern bool SetDefaultPrinterWin32(string printerName);

