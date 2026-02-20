using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Rewired.Utils.Libraries.TinyJson;

namespace YawOnMouse.Blacklist;

public class BlacklistConfigManager
{
    private readonly string _blacklistConfigPath;
    public BlacklistConfig Config { get; private set; }

    public BlacklistConfigManager()
    {
        _blacklistConfigPath = Path.Combine(Paths.ConfigPath, "AircraftBlacklistConfig.json");
        LoadOrCreateConfig();
    }

    public bool TryScanForAircraft()
    {
        var aircraftObjects = UnityEngine.Resources.FindObjectsOfTypeAll<Aircraft>();
        Plugin.Logger.LogInfo($"Aircraft scan: found {aircraftObjects?.Length ?? 0}");
        if (aircraftObjects == null || aircraftObjects.Length == 0) return false;

        bool dirty = false;
        foreach (var aircraft in aircraftObjects)
        {
            var name = aircraft.gameObject.name.Replace("(Clone)", "").Trim();

            if (!Config.Blacklist.ContainsKey(name))
            {
                Config.Blacklist.Add(name, false);
                Plugin.Logger.LogInfo($"Discovered {name}");
                dirty = true;
            }
        }
        
        if (dirty) SaveConfig();
        return true;
    }

    private void LoadOrCreateConfig()
    {
        if (!File.Exists(_blacklistConfigPath))
        {
            Config = GenerateDefaultConfig();
            SaveConfig();
            Plugin.Logger.LogInfo($"Config file {_blacklistConfigPath} has been created!");
        }
        else
        {
            try
            {
                var json = File.ReadAllText(_blacklistConfigPath);
                Config = JsonParser.FromJson<BlacklistConfig>(json);
                Plugin.Logger.LogInfo("permission config loaded.");
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"failed to load permission config: {e.Message}");
                Config = GenerateDefaultConfig();
                SaveConfig();
            }
        }
    }

    private void SaveConfig()
    {
        try
        {
            var json = JsonWriter.ToJson(Config);
            File.WriteAllText(_blacklistConfigPath, json);
            Plugin.Logger.LogInfo("permission config saved.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"failed to save permission config: {e.Message}");
        }   
    }

    private BlacklistConfig GenerateDefaultConfig()
    {
        return new BlacklistConfig
        {
            Blacklist = new Dictionary<string, bool>()
            // {
            //     ["CAS1"] = false, // brawler
            //     ["Darkreach"] = false, // darkreach
            //     ["Multirole1"] = false, // ifrit
            //     ["Fighter1"] = false, // revoker
            //     ["QuadVTOL1"] = false, // tarantula
            //     ["AttackHelo1"] = false, // chicane
            //     ["SmallFighter1"] = false, // vortex
            //     ["trainer"] = false, // compass
            //     ["EW1"] = false, // medusa
            //     ["COIN"] = false, // cricket
            //     ["UtilityHelo1"] = false, // ibis
            //     ["fastBomber1"] = false, // whatever the name of that aircraft will be
            // }
        };
    }
}