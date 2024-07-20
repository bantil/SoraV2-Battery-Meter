using System.ComponentModel;
using HidLibrary;
using SoraBatteryStatus.DTO;
using ToolStripMenuItem = System.Windows.Forms.ToolStripMenuItem;

namespace SoraBatteryStatus;

public partial class BatteryStatusTray : Form
{
    // make this configurable, for now they are hard coded
    private const string DeviceName = "Sora V2";
    private const ushort SoraVid = 0x1915;
    private const ushort SoraPidWireless = 0xAE1C;
    private const ushort SoraPidWired = 0xAE11;
    private const ushort SoraUsagePage = 0xFFA0;
    
    // seconds for polling interval
    private const int PollingInterval = 60;
    
    // other components
    private NotifyIcon _batteryStatus = null!;
    private Container _component = null!;
    private ContextMenuStrip _contextMenuStrip = null!;

    public BatteryStatusTray()
    {
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
        var hidDevices = HidDevices.Enumerate(SoraVid, [SoraPidWireless, SoraPidWired]);

        foreach (var device in hidDevices)
        {
            var usagePage = device.Capabilities.UsagePage;
            var hexValue = usagePage.ToString("X");
            var hexUsagePage = Convert.ToInt32(hexValue, 16);
            
            if (hexUsagePage == SoraUsagePage)
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
        ToolStripLabel appNameLabel = new ToolStripLabel("Sora V2 Battery Meter 1.0");
        appNameLabel.ForeColor = Color.Gray; // Gray color for disabled appearance
        appNameLabel.Enabled = false; // Disable to make it non-clickable
        _contextMenuStrip.Items.Add(appNameLabel);

        // menu items
        ToolStripMenuItem quitMenuItem = new ToolStripMenuItem("Quit");
        ToolStripMenuItem refreshMenuItem = new ToolStripMenuItem("Refresh");
        quitMenuItem.Click += QuitMenuItem_Click!;
        refreshMenuItem.Click += RefreshMenuItem_Click!;
        
        // separator to separate
        _contextMenuStrip.Items.Add(new ToolStripSeparator());
        _contextMenuStrip.Items.Add(quitMenuItem);
        _contextMenuStrip.Items.Add(refreshMenuItem);
        
        _batteryStatus.ContextMenuStrip = _contextMenuStrip;
    }
    
    /// <summary>
    /// Refreshes the battery meter when refresh is clicked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
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
        var width = 34;
        var height = 48;
        var batteryBitmap = new Bitmap(width, height);

        var color = GetBatteryColor(batteryStatus);

        using (var graphics = Graphics.FromImage(batteryBitmap))
        {
            // Clear the bitmap with a transparent background
            graphics.Clear(Color.Transparent);

            if (batteryStatus.CurrentBatteryLevel == -1)
            {
                _batteryStatus.Icon = new Icon("assets/x-icon.ico");
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
        _batteryStatus.Text = $"{DeviceName}: {batteryStatus.CurrentBatteryLevel}%";
    }

    /// <summary>
    /// Returns the color for the battery meter.
    /// </summary>
    /// <param name="batteryStatus"></param>
    /// <returns></returns>
    private Color GetBatteryColor(BatteryStatus batteryStatus)
    {
        // show battery indicator as blue when charging
        if (batteryStatus.ChargingState == 1)
        {
            // go dodgers!!!
            return Color.DodgerBlue;
        }
        
        switch (batteryStatus.CurrentBatteryLevel)
        {
            case <= 100 and > 40:
                return Color.ForestGreen;
            case <= 39 and > 20:
                return Color.Yellow;
            case <= 19 and > 1:
                return Color.Red;
            default:
                return Color.Transparent;
        }
    }

    /// <summary>
    /// Polls the device every 60 seconds to get a battery level.
    /// </summary>
    /// <param name="device"></param>
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
    /// </summary>
    /// <param name="device"></param>
    private void GetMouseStats(HidDevice? device)
    {
        device?.OpenDevice();
            
        byte[] report = new byte[32];
            
        var response = new byte[device.Capabilities.OutputReportByteLength];
            
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