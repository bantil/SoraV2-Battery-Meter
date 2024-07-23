namespace SoraBatteryStatus.DTO;

public class MouseConfiguration
{
    public string? DeviceName { get; set; }
    public ushort DeviceVid { get; set; }
    public ushort DevicePidWireless { get; set; }
    public ushort DevicePidWired { get; set; }
    public ushort DeviceUsagePage { get; set; }
}