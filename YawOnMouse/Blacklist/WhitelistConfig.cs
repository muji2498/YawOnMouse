using System.Collections.Generic;

namespace YawOnMouse.Blacklist;

public class WhitelistConfig
{
    public Dictionary<string, bool> Whitelist = new();

    public bool Enabled(string name)
    {
        foreach (var entry in Whitelist)
        {
            // allows for partial match "COIN (Clone)"
            if (name.Contains(entry.Key) && entry.Value) return true;
        }
        return false;
    }

    public void Add(string name, bool enabled)
    {
        Whitelist.Add(name, enabled);
    }

    public void Remove(string name)
    {
        Whitelist.Remove(name);
    }
}