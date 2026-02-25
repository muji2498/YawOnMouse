namespace YawOnMouse.Helpers;

public static class WhitelistHelper
{
    public static bool IsWhitelisted()
    {
        if (!Plugin.UseCraftWhitelist.Value) return true;

        if (!GameManager.GetLocalAircraft(out Aircraft aircraft)) return false; 
        
        var aircraftName = aircraft.gameObject.name;
        return Plugin.Instance.WhitelistConfigManager.Config.Enabled(aircraftName);
    }
}