namespace SoraBatteryStatus.DTO;

public class BatteryStatus
{
    public int CurrentBatteryLevel { get; set; }
    public int ChargingState { get; set; }
    public int FullyChargedState { get; set; }
    public int OnlineState { get; set; }
}