namespace YawOnMouse.Helpers;

public static class PatchHelper
{
    public static bool ShouldUseYaw()
    {
        return Plugin.Enabled.Value && Plugin.AxisPatchType.Value == AxisPatchType.Yaw && WhitelistHelper.IsWhitelisted();
    }
}