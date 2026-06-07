using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using YawOnMouse.Blacklist;

namespace YawOnMouse;

public static class PluginInfo
{
    public const string PLUGIN_GUID = "YawOnMouse";
    public const string PLUGIN_NAME = "YawOnMouse";
    public const string PLUGIN_VERSION = "2.0.0";
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<bool> Enabled;
    public static ConfigEntry<AxisPatchType> AxisPatchType;
    public static ConfigEntry<bool> UseCraftWhitelist;
    public static ConfigEntry<KeyboardShortcut> ToggleKey;
    public WhitelistConfigManager WhitelistConfigManager;
    public static Plugin Instance;
    
    private bool _scanComplete = false;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        WhitelistConfigManager = new WhitelistConfigManager();

        Enabled = Config.Bind(
            "Config",
            "PlayerAxisControls_Patch",
            true,
            "Enable/Disable controller roll input patch");
        AxisPatchType = Config.Bind(
            "Config",
            "AxisPatchType",
            YawOnMouse.AxisPatchType.Yaw,
            "What you want the patch to do on the x-axis (can only be changed before startup not at runtime.)"
        );
        UseCraftWhitelist = Config.Bind(
            "Config",
            "UseCraftWhitelist",
            false,
            "When enabled the mod will only work on the aircraft specified in the whitelist"
            );
        ToggleKey = Config.Bind(
            "Config",
            "ToggleKey",
            new KeyboardShortcut(KeyCode.Y, KeyCode.LeftAlt),
            "When this keyboard shortcut is pressed the plugin will toggle itself on and off"
        );

        // Plugin startup logic
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }

    private void Update()
    {
        if (ToggleKey.Value.IsDown())
        {
            Enabled.Value = !Enabled.Value;
            Config.Save();
#if DEBUG
            Logger.LogInfo($"Plugin toggled: {(Enabled.Value ? "Enabled" : "Disabled")}");
#endif
        }
        
        // really dirty ik, but only runs when plugin is first ran
        if (!_scanComplete)
        {
            _scanComplete = WhitelistConfigManager.TryScanForAircraft();
        }
    }
}