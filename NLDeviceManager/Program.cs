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

        // Null means the input stream reached end-of-file (piped or closed
        // input). Exit cleanly instead of looping forever on the invalid-choice
        // branch or crashing on ReadKey below.
        if (choice is null)
        {
            Console.WriteLine("\nNo more input. Exiting...");
            break;
        }

        switch (DeviceManagerService.ParseMenuChoice(choice))
        {
            case MenuAction.ListUsbDevices:
                Console.WriteLine();
                Console.WriteLine(service.RenderUsbDevices());
                PropagateFailure(service);
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
}

static void ListPrinters(DeviceManagerService service)
{
    PrinterListing listing = service.RenderPrinters();
    Console.WriteLine(listing.Output);
    PropagateFailure(service);

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
        PropagateFailure(service);
    }
    else
    {
        Console.WriteLine("Invalid printer number.");
    }
}

// Surfaces a failed operation: logs full exception detail to stderr (rather
// than losing everything but the message) and sets a non-zero process exit
// code so callers and scripts can detect that something went wrong.
static void PropagateFailure(DeviceManagerService service)
{
    if (service.LastOperationSucceeded)
    {
        return;
    }

    if (service.LastError is not null)
    {
        Console.Error.WriteLine(service.LastError);
    }

    if (Environment.ExitCode == 0)
    {
        Environment.ExitCode = 1;
    }
}
