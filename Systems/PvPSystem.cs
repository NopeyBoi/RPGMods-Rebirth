using ProjectM;
using ProjectM.Network;
using RPGMods.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Entities;
using Wetstone.API;

namespace RPGMods.Systems;

public class PvPSystem
{
    public static bool announce_kills = true;

    public static bool isLadderEnabled = true;
    public static bool isPvPToggleEnabled = true;
    public static bool isPunishEnabled = true;
    public static int PunishLevelDiff = -10;
    public static float PunishDuration = 1800f;
    public static int OffenseLimit = 3;
    public static float Offense_Cooldown = 300f;

    public static EntityManager em = VWorld.Server.EntityManager;

    private static ModifyUnitStatBuff_DOTS PResist = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.PhysicalResistance,
        Value = -15,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS FResist = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.FireResistance,
        Value = -15,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS HResist = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.HolyResistance,
        Value = -15,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS SPResist = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.SpellResistance,
        Value = -15,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS SunResist = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.SunResistance,
        Value = -15,
        ModificationType = ModificationType.Add,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS PPower = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.PhysicalPower,
        Value = 0.75f,
        ModificationType = ModificationType.Multiply,
        Id = ModificationId.NewId(0)
    };

    private static ModifyUnitStatBuff_DOTS SPPower = new ModifyUnitStatBuff_DOTS()
    {
        StatType = UnitStatType.SpellPower,
        Value = 0.75f,
        ModificationType = ModificationType.Multiply,
        Id = ModificationId.NewId(0)
    };

    public static void Monitor(Entity KillerEntity, Entity VictimEntity)
    {
        PlayerCharacter killer = em.GetComponentData<PlayerCharacter>(KillerEntity);
        Entity killer_userEntity = killer.UserEntity._Entity;
        User killer_user = em.GetComponentData<User>(killer_userEntity);
        string killer_name = killer_user.CharacterName.ToString();
        ulong killer_id = killer_user.PlatformId;

        PlayerCharacter victim = em.GetComponentData<PlayerCharacter>(VictimEntity);
        Entity victim_userEntity = victim.UserEntity._Entity;
        User victim_user = em.GetComponentData<User>(victim_userEntity);
        string victim_name = victim_user.CharacterName.ToString();
        ulong victim_id = victim_user.PlatformId;

        victim_user.SendSystemMessage($"<color=#c90e21ff>You've been killed by \"{killer_name}\"</color>");

        _ = Database.pvpkills.TryGetValue(killer_id, out int KillerKills);
        _ = Database.pvpdeath.TryGetValue(victim_id, out int VictimDeath);

        Database.pvpkills[killer_id] = KillerKills + 1;
        Database.pvpdeath[victim_id] = VictimDeath + 1;

        //-- Update K/D
        UpdateKD(killer_id, victim_id);

        //-- Announce Kills
        if (announce_kills) ServerChatUtils.SendSystemMessageToAllClients(em, $"Vampire \"{killer_name}\" has killed \"{victim_name}\"!");
    }

    public static void UpdateKD(ulong killer_id, ulong victim_id)
    {
        bool isExist = Database.pvpdeath.TryGetValue(killer_id, out _);
        if (!isExist) Database.pvpdeath[killer_id] = 0;

        isExist = Database.pvpkills.TryGetValue(victim_id, out _);
        if (!isExist) Database.pvpkills[victim_id] = 0;

        if (Database.pvpdeath[killer_id] != 0) Database.pvpkd[killer_id] = (double)Database.pvpkills[killer_id] / Database.pvpdeath[killer_id];
        else Database.pvpkd[killer_id] = Database.pvpkills[killer_id];

        if (Database.pvpkills[victim_id] != 0) Database.pvpkd[victim_id] = (double)Database.pvpkills[victim_id] / Database.pvpdeath[victim_id];
        else Database.pvpkd[victim_id] = 0;
    }

    public static void PunishCheck(Entity Killer, Entity Victim)
    {
        Entity KillerUser = em.GetComponentData<PlayerCharacter>(Killer).UserEntity._Entity;
        ulong KillerSteamID = em.GetComponentData<User>(KillerUser).PlatformId;
        Equipment KillerGear = em.GetComponentData<Equipment>(Killer);
        float KillerLevel = KillerGear.ArmorLevel + KillerGear.WeaponLevel + KillerGear.SpellLevel;

        Equipment VictimGear = em.GetComponentData<Equipment>(Victim);
        float VictimLevel = VictimGear.ArmorLevel + VictimGear.WeaponLevel + VictimGear.SpellLevel;

        if (VictimLevel - KillerLevel <= PunishLevelDiff)
        {
            _ = Cache.punish_killer_last_offense.TryGetValue(KillerSteamID, out DateTime last_offense);
            TimeSpan timeSpan = DateTime.Now - last_offense;
            if (timeSpan.TotalSeconds > Offense_Cooldown) Cache.punish_killer_offense[KillerSteamID] = 1;
            else Cache.punish_killer_offense[KillerSteamID] += 1;
            Cache.punish_killer_last_offense[KillerSteamID] = DateTime.Now;

            if (Cache.punish_killer_offense[KillerSteamID] >= OffenseLimit)
            {
                Helper.ApplyBuff(Killer, KillerUser, Database.buff.Severe_GarlicDebuff);
            }
        }
    }

    public static void BuffReceiver(Entity BuffEntity, PrefabGUID GUID)
    {
        if (GUID.Equals(Database.buff.Severe_GarlicDebuff))
        {
            LifeTime lifeTime_component = em.GetComponentData<LifeTime>(BuffEntity);
            lifeTime_component.Duration = PvPSystem.PunishDuration;
            em.SetComponentData(BuffEntity, lifeTime_component);

            DynamicBuffer<ModifyUnitStatBuff_DOTS> Buffer = em.AddBuffer<ModifyUnitStatBuff_DOTS>(BuffEntity);
            _ = Buffer.Add(PPower);
            _ = Buffer.Add(SPPower);
            _ = Buffer.Add(HResist);
            _ = Buffer.Add(FResist);
            _ = Buffer.Add(SPResist);
            _ = Buffer.Add(PResist);
        }
    }

    public static void SavePvPStat()
    {
        File.WriteAllText("BepInEx/config/RPGMods/Saves/pvpkills.json", JsonSerializer.Serialize(Database.pvpkills, Database.JSON_options));
        File.WriteAllText("BepInEx/config/RPGMods/Saves/pvpdeath.json", JsonSerializer.Serialize(Database.pvpdeath, Database.JSON_options));
        File.WriteAllText("BepInEx/config/RPGMods/Saves/pvpkd.json", JsonSerializer.Serialize(Database.pvpkd, Database.JSON_options));
    }

    public static void LoadPvPStat()
    {
        if (!File.Exists("BepInEx/config/RPGMods/Saves/pvpkills.json"))
        {
            FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/pvpkills.json");
            stream.Dispose();
        }
        string json = File.ReadAllText("BepInEx/config/RPGMods/Saves/pvpkills.json");
        try
        {
            Database.pvpkills = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json);
            Plugin.Logger.LogWarning("PvPKills DB Populated.");
        }
        catch
        {
            Database.pvpkills = new Dictionary<ulong, int>();
            Plugin.Logger.LogWarning("PvPKills DB Created.");
        }

        if (!File.Exists("BepInEx/config/RPGMods/Saves/pvpdeath.json"))
        {
            FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/pvpdeath.json");
            stream.Dispose();
        }
        json = File.ReadAllText("BepInEx/config/RPGMods/Saves/pvpdeath.json");
        try
        {
            Database.pvpdeath = JsonSerializer.Deserialize<Dictionary<ulong, int>>(json);
            Plugin.Logger.LogWarning("PvPDeath DB Populated.");
        }
        catch
        {
            Database.pvpdeath = new Dictionary<ulong, int>();
            Plugin.Logger.LogWarning("PvPDeath DB Created.");
        }

        if (!File.Exists("BepInEx/config/RPGMods/Saves/pvpkd.json"))
        {
            FileStream stream = File.Create("BepInEx/config/RPGMods/Saves/pvpkd.json");
            stream.Dispose();
        }
        json = File.ReadAllText("BepInEx/config/RPGMods/Saves/pvpkd.json");
        try
        {
            Database.pvpkd = JsonSerializer.Deserialize<Dictionary<ulong, double>>(json);
            Plugin.Logger.LogWarning("PvPKD DB Populated.");
        }
        catch
        {
            Database.pvpkd = new Dictionary<ulong, double>();
            Plugin.Logger.LogWarning("PvPKD DB Created.");
        }
    }
}
