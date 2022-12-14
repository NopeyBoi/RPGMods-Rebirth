using System.Collections.Generic;

namespace RPGMods.Utils;

public class ItemKit
{
    public string Name { get; }
    public Dictionary<int, int> PrefabGUIDs { get; }

    public ItemKit(string name, Dictionary<int, int> prefabGuids)
    {
        Name = name;
        PrefabGUIDs = prefabGuids;
    }
}
