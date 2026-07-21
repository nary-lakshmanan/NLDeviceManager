using System.Management;
using System.Drawing.Printing;
using NLDeviceManager;

bool running = true;

while (running)
{
    Console.Clear();
    ConsoleUi.PrintBanner("    Device Manager");
    Console.WriteLine("\nPlease select an option:");
    Console.WriteLine("1. List Connected USB Devices");
    Console.WriteLine("2. List Connected Printers");
    Console.WriteLine("3. Exit");
    ConsoleUi.PrintSeparator();
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
    Console.WriteLine();
    ConsoleUi.PrintBanner("Connected USB Devices:");

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

            ConsoleUi.PrintCountSummary(count, "USB devices");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error enumerating USB devices: {ex.Message}");
    }
}

static void ListConnectedPrinters()
{
    Console.WriteLine();
    ConsoleUi.PrintBanner("Connected Printers:");

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

        ConsoleUi.PrintCountSummary(count, "printers");

        if (count > 0)
        {
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
                    PrinterManager.SetDefaultPrinterByNumber(selectedPrinter);
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
