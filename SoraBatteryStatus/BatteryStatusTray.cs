using System.ComponentModel;
using HidLibrary;
using SoraBatteryStatus.DTO;

namespace SoraBatteryStatus;

public partial class BatteryStatusTray : Form
{
    // consts for Sora V2 IDs
    // make this configurable, for now they are hard coded
    private const ushort soraVid = 0x1915;
    private const ushort soraPidWireless = 0xAE1C;
    private const ushort soraPidWired = 0xAE11;
    private const ushort soraUsagePage = 0xFFA0;
    
    // shows the form display
    private const bool ShowFormDisplay = false;
    
    // seconds for polling interval
    private const int PollingInterval = 5;
    
    private NotifyIcon _batteryStatus;
    private Container _component;

    public BatteryStatusTray()
    {
        InitializeComponent();
        
        // don't show main window for now when program runs
        base.SetVisibleCore(ShowFormDisplay);
        
        SetUpBatteryIcon();
        var device = GetSpecificDevice();
        PollForMouseStats(device);
    }
    
    /// <summary>
    /// Retrieves a specific device, in thise case, our sora mouse.
    /// </summary>
    /// <returns></returns>
    private static HidDevice? GetSpecificDevice()
    {
        var hidDevices = HidDevices.Enumerate(soraVid, soraPidWireless);

        foreach (var device in hidDevices)
        {
            var usagePage = device.Capabilities.UsagePage;
            var hexValue = usagePage.ToString("X");
            var hexUsagePage = Convert.ToInt32(hexValue, 16);
            
            if (hexUsagePage == soraUsagePage)
            {
                return device;
            }
        }

        return null;
    }

    private void SetUpBatteryIcon()
    {
        _component = new Container();
        
        _batteryStatus = new NotifyIcon(_component);

        _batteryStatus.Icon = new Icon("assets/smoothie.ico");

        _batteryStatus.Visible = true;
    }

    private async Task PollForMouseStats(HidDevice? device)
    {
        // polling time
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingInterval));
        
        while (await timer.WaitForNextTickAsync())
        {
            device?.OpenDevice();
            
            byte[] report = new byte[32];
            
            var response = new byte[device.Capabilities.OutputReportByteLength];
            
            // needed bytes to send to the report
            report[0] = 5;
            report[1] = 21;
            report[4] = 1;
            
            // Read the response
            if (device.WriteFeatureData(report))
            {
                device.ReadFeatureData(out response, report[0]);
            }
            
            // get the specific values that contain our values for battery and info
            var batteryStatusDto = new BatteryStatus()
            {
                CurrentBatteryLevel = response[9],
                ChargingState = response[10],
                FullyChargedState = response[11],
                OnlineState = response[12],
            };
        }
    }
}