using System.ComponentModel;

namespace SoraBatteryStatus;

public partial class BatteryStatusTray : Form
{
    private const bool ShowFormDisplay = false;
    private NotifyIcon _batteryStatus;
    private Container _component;

    public BatteryStatusTray()
    {
        InitializeComponent();
        
        _component = new Container();
        
        _batteryStatus = new NotifyIcon(_component);

        _batteryStatus.Icon = new Icon("assets/smoothie.ico");

        _batteryStatus.Visible = true;
    }

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(ShowFormDisplay && value);
    }
}