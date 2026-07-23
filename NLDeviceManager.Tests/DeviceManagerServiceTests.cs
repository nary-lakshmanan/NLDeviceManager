using NLDeviceManager;
using NLDeviceManager.Devices;

namespace NLDeviceManager.Tests;

public class DeviceManagerServiceTests
{
    private static DeviceManagerService CreateService(
        IUsbDeviceProvider? usb = null,
        IPrinterProvider? printers = null)
    {
        return new DeviceManagerService(
            usb ?? new FakeUsbDeviceProvider(Array.Empty<UsbDevice>()),
            printers ?? new FakePrinterProvider(Array.Empty<string>()));
    }

    // ---- ParseMenuChoice ----

    [Theory]
    [InlineData("1", MenuAction.ListUsbDevices)]
    [InlineData("2", MenuAction.ListPrinters)]
    [InlineData("3", MenuAction.Exit)]
    [InlineData("0", MenuAction.Invalid)]
    [InlineData("", MenuAction.Invalid)]
    [InlineData(" 1", MenuAction.Invalid)]
    [InlineData(null, MenuAction.Invalid)]
    public void ParseMenuChoice_MapsInputToAction(string? choice, MenuAction expected)
    {
        Assert.Equal(expected, DeviceManagerService.ParseMenuChoice(choice));
    }

    // ---- RenderUsbDevices ----

    [Fact]
    public void RenderUsbDevices_NoDevices_ReportsNoneFound()
    {
        DeviceManagerService service = CreateService(
            usb: new FakeUsbDeviceProvider(Array.Empty<UsbDevice>()));

        string output = service.RenderUsbDevices();

        Assert.Contains("Connected USB Devices:", output);
        Assert.Contains("No USB devices found.", output);
        Assert.DoesNotContain("Total USB devices found", output);
    }

    [Fact]
    public void RenderUsbDevices_WithDevices_NumbersAndCountsThem()
    {
        var devices = new[]
        {
            new UsbDevice("Keyboard", "USB\\VID_1", "OK"),
            new UsbDevice("Mouse", "USB\\VID_2", "Error"),
        };
        DeviceManagerService service = CreateService(usb: new FakeUsbDeviceProvider(devices));

        string output = service.RenderUsbDevices();

        Assert.Contains("[1] Keyboard", output);
        Assert.Contains("Device ID: USB\\VID_1", output);
        Assert.Contains("Status: OK", output);
        Assert.Contains("[2] Mouse", output);
        Assert.Contains("Total USB devices found: 2", output);
    }

    [Fact]
    public void RenderUsbDevices_ProviderThrows_ReportsError()
    {
        DeviceManagerService service = CreateService(
            usb: new FakeUsbDeviceProvider(new InvalidOperationException("wmi down")));

        string output = service.RenderUsbDevices();

        Assert.Contains("Error enumerating USB devices: wmi down", output);
    }

    // ---- RenderPrinters ----

    [Fact]
    public void RenderPrinters_NoPrinters_ReportsNoneFound()
    {
        DeviceManagerService service = CreateService(
            printers: new FakePrinterProvider(Array.Empty<string>()));

        PrinterListing listing = service.RenderPrinters();

        Assert.Empty(listing.Printers);
        Assert.Contains("Connected Printers:", listing.Output);
        Assert.Contains("No printers found.", listing.Output);
    }

    [Fact]
    public void RenderPrinters_MarksDefaultAndCounts()
    {
        var provider = new FakePrinterProvider(new[] { "PDF", "Laser" }) { DefaultPrinter = "Laser" };
        DeviceManagerService service = CreateService(printers: provider);

        PrinterListing listing = service.RenderPrinters();

        Assert.Equal(new[] { "PDF", "Laser" }, listing.Printers);
        Assert.Contains("[1] PDF", listing.Output);
        Assert.DoesNotContain("[1] PDF (Default)", listing.Output);
        Assert.Contains("[2] Laser (Default)", listing.Output);
        Assert.Contains("Total printers found: 2", listing.Output);
    }

    [Fact]
    public void RenderPrinters_ProviderThrows_ReportsErrorAndEmptyList()
    {
        DeviceManagerService service = CreateService(
            printers: new FakePrinterProvider(new InvalidOperationException("spooler off")));

        PrinterListing listing = service.RenderPrinters();

        Assert.Empty(listing.Printers);
        Assert.Contains("Error listing printers: spooler off", listing.Output);
    }

    // ---- TryParsePrinterSelection ----

    [Theory]
    [InlineData("1", 3, true, 0)]
    [InlineData("3", 3, true, 2)]
    [InlineData("0", 3, false, -1)]
    [InlineData("4", 3, false, -1)]
    [InlineData("abc", 3, false, -1)]
    [InlineData("", 3, false, -1)]
    [InlineData(null, 3, false, -1)]
    [InlineData("2", 0, false, -1)]
    public void TryParsePrinterSelection_ValidatesRange(string? input, int count, bool expectedValid, int expectedIndex)
    {
        bool valid = DeviceManagerService.TryParsePrinterSelection(input, count, out int index);

        Assert.Equal(expectedValid, valid);
        Assert.Equal(expectedIndex, index);
    }

    // ---- SetDefaultPrinter (with existence check) ----

    [Fact]
    public void SetDefaultPrinter_ExistingPrinter_Succeeds()
    {
        var provider = new FakePrinterProvider(new[] { "Office Laser" }) { SetDefaultPrinterResult = true };
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinter("Office Laser");

        Assert.Contains("Successfully set 'Office Laser' as the default printer.", output);
        Assert.Equal("Office Laser", provider.LastSetDefaultRequest);
    }

    [Fact]
    public void SetDefaultPrinter_MatchesCaseInsensitively_UsesInstalledCasing()
    {
        var provider = new FakePrinterProvider(new[] { "Office Laser" });
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinter("office laser");

        Assert.Contains("Successfully set 'Office Laser' as the default printer.", output);
        Assert.Equal("Office Laser", provider.LastSetDefaultRequest);
    }

    [Fact]
    public void SetDefaultPrinter_MissingPrinter_ReportsNotFound()
    {
        var provider = new FakePrinterProvider(new[] { "Office Laser" });
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinter("Ghost");

        Assert.Contains("Error: Printer 'Ghost' not found.", output);
        Assert.Null(provider.LastSetDefaultRequest);
    }

    [Fact]
    public void SetDefaultPrinter_ApiFails_ReportsErrorCode()
    {
        var provider = new FakePrinterProvider(new[] { "Office Laser" })
        {
            SetDefaultPrinterResult = false,
            LastError = 1801,
        };
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinter("Office Laser");

        Assert.Contains("Failed to set default printer. Error code: 1801", output);
    }

    [Fact]
    public void SetDefaultPrinter_ProviderThrows_ReportsError()
    {
        DeviceManagerService service = CreateService(
            printers: new FakePrinterProvider(new InvalidOperationException("boom")));

        string output = service.SetDefaultPrinter("Anything");

        Assert.Contains("Error setting default printer: boom", output);
    }

    // ---- SetDefaultPrinterByNumber (no existence check) ----

    [Fact]
    public void SetDefaultPrinterByNumber_Succeeds()
    {
        var provider = new FakePrinterProvider(Array.Empty<string>()) { SetDefaultPrinterResult = true };
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinterByNumber("Some Printer");

        Assert.Contains("Attempting to set 'Some Printer' as default printer...", output);
        Assert.Contains("Successfully set 'Some Printer' as the default printer.", output);
        Assert.Equal("Some Printer", provider.LastSetDefaultRequest);
    }

    [Fact]
    public void SetDefaultPrinterByNumber_ApiFails_ReportsErrorCode()
    {
        var provider = new FakePrinterProvider(Array.Empty<string>())
        {
            SetDefaultPrinterResult = false,
            LastError = 5,
        };
        DeviceManagerService service = CreateService(printers: provider);

        string output = service.SetDefaultPrinterByNumber("Some Printer");

        Assert.Contains("Failed to set default printer. Error code: 5", output);
    }
}
