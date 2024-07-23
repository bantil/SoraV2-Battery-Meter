using System.ComponentModel;
using HidLibrary;
using SoraBatteryStatus.DTO;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace SoraBatteryStatus;

public partial class BatteryStatusTray : Form
{
    // seconds for polling interval
    private const int PollingInterval = 60;
    
    // other components
    private NotifyIcon _batteryStatus = null!;
    private Container _component = null!;
    private ContextMenuStrip _contextMenuStrip = null!;
    private static MouseConfiguration _mouseConfig = null!;
    
    public BatteryStatusTray(MouseConfiguration mouseConfig)
    {
        // get our mouse configuration from our appsettings.json
        _mouseConfig = mouseConfig;
        
        InitializeComponent();
        
        SetUpBatteryIcon();
        
        _ = PollForMouseStats();
    }

    /// <summary>
    /// Hides the main window on startup.
    /// </summary>
    /// <param name="value"></param>
    protected override void SetVisibleCore(bool value)
    {
        if (!IsHandleCreated) {
            value = false;
            CreateHandle();
        }
        base.SetVisibleCore(value);
    }
    
    /// <summary>
    /// Retrieves a specific device, in this case, our sora mouse.
    /// </summary>
    /// <returns></returns>
    private static HidDevice? GetSpecificDevice()
    {
        var hidDevices = HidDevices.Enumerate(_mouseConfig.DeviceVid, [_mouseConfig.DevicePidWireless, _mouseConfig.DevicePidWired]);

        foreach (var device in hidDevices)
        {
            var usagePage = device.Capabilities.UsagePage;
            var hexValue = usagePage.ToString("X");
            var hexUsagePage = Convert.ToInt32(hexValue, 16);
            
            if (hexUsagePage == _mouseConfig.DeviceUsagePage)
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
        _batteryStatus.Visible = true;
        
        // create context menu strip
        _contextMenuStrip = new ContextMenuStrip();
        
        // label for the app name
        var appNameLabel = new ToolStripLabel("Sora V2 Battery Meter 1.0");
        appNameLabel.ForeColor = Color.Gray;
        appNameLabel.Enabled = false;
        _contextMenuStrip.Items.Add(appNameLabel);

        // menu items
        var quitMenuItem = new ToolStripMenuItem("Quit");
        var refreshMenuItem = new ToolStripMenuItem("Refresh");
        quitMenuItem.Click += QuitMenuItem_Click!;
        refreshMenuItem.Click += RefreshMenuItem_Click;
        
        // separator to separate
        _contextMenuStrip.Items.Add(new ToolStripSeparator());
        _contextMenuStrip.Items.Add(refreshMenuItem);
        _contextMenuStrip.Items.Add(quitMenuItem);
        
        _batteryStatus.ContextMenuStrip = _contextMenuStrip;
    }
    
    /// <summary>
    /// Refreshes the battery meter when refresh is clicked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void RefreshMenuItem_Click(object? sender, EventArgs e)
    {
        GetMouseStats(GetSpecificDevice());
    }

    private void QuitMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private void UpdateBatteryIcon(BatteryStatus batteryStatus)
    {
        // bitmap for icon
        const int width = 34;
        const int height = 48;
        var batteryBitmap = new Bitmap(width, height);

        var color = GetBatteryColor(batteryStatus);

        using (var graphics = Graphics.FromImage(batteryBitmap))
        {
            // Clear the bitmap with a transparent background
            graphics.Clear(Color.Transparent);

            if (batteryStatus.CurrentBatteryLevel == -1)
            {
                _batteryStatus.Icon = new Icon("assets/dc-icon.ico");
                _batteryStatus.Text = $"{_mouseConfig.DeviceName}: Device Not Found";
                return;
            }

            if (batteryStatus.FullyChargedState == 1)
            {
                _batteryStatus.Icon = new Icon("assets/check-icon.ico");
                _batteryStatus.Text = $"{_mouseConfig.DeviceName}: Fully Charged";
                return;
            }
        
            // draw the battery outline
            var batteryOutline = new Rectangle(2, 2, width - 4, height - 4);
            var batteryOutlinePen = new Pen(Color.Black, 2);
            graphics.DrawRectangle(batteryOutlinePen, batteryOutline);

            // draw the battery level
            var batteryLevelHeight = (int)((height - 4) * (batteryStatus.CurrentBatteryLevel / 100.0));
            var batteryLevelRect = new Rectangle(3, height - 3 - batteryLevelHeight, width - 6, batteryLevelHeight);
            var batteryLevelBrush = new SolidBrush(color);
            graphics.FillRectangle(batteryLevelBrush, batteryLevelRect);
        }

        // set the icon and text
        _batteryStatus.Icon = Icon.FromHandle(batteryBitmap.GetHicon());
        _batteryStatus.Text = $"{_mouseConfig.DeviceName}: {batteryStatus.CurrentBatteryLevel}%";
    }

    /// <summary>
    /// Returns the color for the battery meter.
    /// </summary>
    /// <param name="batteryStatus"></param>
    /// <returns></returns>
    private static Color GetBatteryColor(BatteryStatus batteryStatus)
    {
        // show battery indicator as blue when charging
        if (batteryStatus.ChargingState == 1)
        {
            // go dodgers!!!
            return Color.DodgerBlue;
        }
        
        switch (batteryStatus.CurrentBatteryLevel)
        {
            case <= 100 and > 41:
                return Color.ForestGreen;
            case <= 40 and > 21:
                return Color.Yellow;
            case <= 20 and > 1:
                return Color.Red;
            default:
                return Color.Transparent;
        }
    }

    /// <summary>
    /// Polls the device every 60 seconds to get a battery level.
    /// </summary>
    private async Task PollForMouseStats()
    {
        // polling time
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(PollingInterval));
        
        do
        {
            var device = GetSpecificDevice();
            
            if (device == null)
            {
                var batteryStatusDto = new BatteryStatus()
                {
                    CurrentBatteryLevel = -1
                };
                
                UpdateBatteryIcon(batteryStatusDto);
            }
            else
            {
                GetMouseStats(device);
            }
        } while (await timer.WaitForNextTickAsync());
    }

    /// <summary>
    /// This is specific to the Sora V2 configuration
    /// May want to update this to a factory or something for other models.
    /// feature report data was checked using free usb analyzer
    /// </summary>
    /// <param name="device"></param>
    private void GetMouseStats(HidDevice? device)
    {
        device?.OpenDevice();
            
        var report = new byte[32];
            
        var response = new byte[device!.Capabilities.OutputReportByteLength];
            
        // needed bytes to send to the feature report
        report[0] = 5; // report id
        report[1] = 21;
        report[4] = 1;
            
        // read the feature report response, takes an output var and report id.
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
            
        // Initialize the battery icon with a full level (for example)
        UpdateBatteryIcon(batteryStatusDto);
            
        // close the device after we've gotten what we need
        device.CloseDevice();
    }
}