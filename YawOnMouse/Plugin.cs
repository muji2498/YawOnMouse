using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace YawOnMouse;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "YawOnMouse";
    public const string PLUGIN_NAME = "YawOnMouse";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<AxisPatchType> AxisPatchType;
    public static Plugin Instance;

    private void Awake()
    {
        Enabled = Config.Bind(
            "Config",
            "PlayerAxisControls_Patch",
            true,
            "Enable/Disable controller roll input patch (can only be changed before startup not at runtime.)");
        AxisPatchType = Config.Bind(
            "Config",
            "AxisPatchType",
            YawOnMouse.AxisPatchType.Yaw,
            "What you want the patch to do on the x-axis (can only be changed before startup not at runtime.)"
        );
        
        
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}