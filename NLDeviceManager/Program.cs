using System.Runtime.Versioning;
using NLDeviceManager;
using NLDeviceManager.Devices;

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("NLDeviceManager is only supported on Windows.");
    return;
}

RunWindows();

[SupportedOSPlatform("windows")]
static void RunWindows()
{
    var service = new DeviceManagerService(new WindowsUsbDeviceProvider(), new WindowsPrinterProvider());
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

        switch (DeviceManagerService.ParseMenuChoice(choice))
        {
            case MenuAction.ListUsbDevices:
                Console.WriteLine();
                Console.WriteLine(service.RenderUsbDevices());
                break;
            case MenuAction.ListPrinters:
                Console.WriteLine();
                ListPrinters(service);
                break;
            case MenuAction.Exit:
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
}

static void ListPrinters(DeviceManagerService service)
{
    PrinterListing listing = service.RenderPrinters();
    Console.WriteLine(listing.Output);

    if (listing.Printers.Count == 0)
    {
        return;
    }

    Console.Write("\nWould you like to set a default printer? (y/n): ");
    string? response = Console.ReadLine();

    if (response?.Trim().ToLower() != "y")
    {
        return;
    }

    Console.Write($"Enter printer number (1-{listing.Printers.Count}) to set as default: ");
    string? input = Console.ReadLine();

    if (DeviceManagerService.TryParsePrinterSelection(input, listing.Printers.Count, out int index))
    {
        Console.WriteLine();
        Console.WriteLine(service.SetDefaultPrinterByNumber(listing.Printers[index]));
    }
    else
    {
        Console.WriteLine("Invalid printer number.");
    }
}
