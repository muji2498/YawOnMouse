using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using YawOnMouse.Blacklist;

namespace YawOnMouse;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "YawOnMouse";
    public const string PLUGIN_NAME = "YawOnMouse";
    public const string PLUGIN_VERSION = "1.1.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<AxisPatchType> AxisPatchType;
    public static ConfigEntry<bool> UseCraftBlackList;
    public BlacklistConfigManager BlacklistConfigManager;
    public static Plugin Instance;
    
    private bool _scanComplete = false;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        BlacklistConfigManager = new BlacklistConfigManager();

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
        UseCraftBlackList = Config.Bind(
            "Config",
            "UseCraftBlackList",
            false,
            "When enabled the mod will not work if you are in the crafts specified in the blacklist"
            );

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    private void Update()
    {
        // really dirty ik, but only runs when plugin is first ran
        if (!_scanComplete)
        {
            _scanComplete = BlacklistConfigManager.TryScanForAircraft();
        }
    }
}