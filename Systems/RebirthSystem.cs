using Il2CppSystem.IO;
using ProjectM;
using RPGMods.Utils;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Entities;
using Wetstone.API;

namespace RPGMods.Systems;

public static class RebirthSystem
{
    private static EntityManager entityManager = VWorld.Server.EntityManager;

    public static int MaxRebirthLevel = 10;

    public static void Rebirth(Context ctx)
    {
        ulong SteamID = ctx.Event.User.PlatformId;
        Database.player_experience[SteamID] = 0;
        if (Database.rebirths.ContainsKey(SteamID)) Database.rebirths[SteamID] += 1;
        else Database.rebirths.Add(SteamID, 1);
        ExperienceSystem.SetLevel(ctx.Event.SenderCharacterEntity, ctx.Event.SenderUserEntity, SteamID);
    }

    public static List<ModifyUnitStatBuff_DOTS> GetBonusStats(int level)
        => new()
        {
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.PhysicalCriticalStrikeChance, Value = level * Database.BonusPhysicalCritChance, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.PhysicalCriticalStrikeDamage, Value = level * Database.BonusPhysicalCritDamage, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.SpellCriticalStrikeChance, Value = level * Database.BonusSpellCritChance, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.SpellCriticalStrikeDamage, Value = level * Database.BonusSpellCritDamage, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.AttackSpeed, Value = level * Database.BonusAttackSpeed, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.SpellPower, Value = level * Database.BonusSpellPower, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.PassiveHealthRegen, Value = level * Database.BonusHealthRegen, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
            new ModifyUnitStatBuff_DOTS() { StatType = UnitStatType.PhysicalPower, Value = level * Database.BonusPhysicalPower, ModificationType = ModificationType.Add, Id = new ModificationId(0) },
        };

    public static int getLevel(ulong steamId)
    {
        if (Database.rebirths.TryGetValue(steamId, out int level))
            return level;
        return 0;
    }

    public static void SaveRebirths()
    {
        File.WriteAllText("BepInEx/config/RPGMods/Saves/rebirths.json", JsonSerializer.Serialize(Database.rebirths, Database.JSON_options));
    }

    public static void LoadRebirths()
    {
        if (!File.Exists("BepInEx/config/RPGMods/Saves/rebirths.json"))
        {
            FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/rebirths.json");
            stream.Dispose();
        }
        string json = File.ReadAllText("BepInEx/config/RPGMods/Saves/rebirths.json");
        try
        {
            Database.rebirths = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json);
            Plugin.Logger.LogWarning("Rebirths DB Populated.");
        }
        catch
        {
            Database.rebirths = new Dictionary<ulong, int>();
            Plugin.Logger.LogWarning("Rebirths DB Created.");
        }
    }
}
