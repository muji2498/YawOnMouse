using System.Collections.Generic;

namespace YawOnMouse.Blacklist;

public class BlacklistConfig
{
    public Dictionary<string, bool> Blacklist = new();

    public bool Enabled(string name)
    {
        foreach (var entry in Blacklist)
        {
            // allows for partial match "COIN (Clone)"
            if (name.Contains(entry.Key) && entry.Value) return true;
        }
        return false;
    }

    public void Add(string name, bool enabled)
    {
        Blacklist.Add(name, enabled);
    }

    public void Remove(string name)
    {
        Blacklist.Remove(name);
    }
}