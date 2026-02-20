namespace YawOnMouse.Helpers;

public static class BlacklistHelper
{
    public static bool IsBlacklisted()
    {
        if (!Plugin.UseCraftBlackList.Value) return false;

        if (!GameManager.GetLocalAircraft(out Aircraft aircraft)) return false; 
        
        var aircraftName = aircraft.gameObject.name;
        return Plugin.Instance.BlacklistConfigManager.Config.Enabled(aircraftName);
    }
}