﻿using HarmonyLib;
using ProjectM;
using RPGMods.Systems;
using Unity.Collections;
using Unity.Entities;

namespace RPGMods.Hooks;

[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
public class VampireDownedServerEventSystem_Patch
{
    public static void Postfix(VampireDownedServerEventSystem __instance)
    {
        if (__instance.__OnUpdate_LambdaJob0_entityQuery == null) return;

        EntityManager em = __instance.EntityManager;
        NativeArray<Entity> EventsQuery = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity entity in EventsQuery)
        {
            _ = VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, em, out Entity Victim);
            Entity Source = em.GetComponentData<VampireDownedBuff>(entity).Source;
            _ = VampireDownedServerEventSystem.TryFindRootOwner(Source, 1, em, out Entity Killer);

            //var GUID = Helper.GetPrefabGUID(Killer);
            //var Prefab_Name = Helper.GetPrefabName(GUID);
            //Plugin.Logger.LogWarning($"1 Depth - eID: {GUID.GetHashCode()} | eC: {Prefab_Name}");

            //VampireDownedServerEventSystem.TryFindRootOwner(Source, 2, em, out Killer);
            //GUID = Helper.GetPrefabGUID(Killer);
            //Prefab_Name = Helper.GetPrefabName(GUID);
            //Plugin.Logger.LogWarning($"2 Depth - eID: {GUID.GetHashCode()} | eC: {Prefab_Name}");

            //-- Update PvP Stats & Check
            if (em.HasComponent<PlayerCharacter>(Killer) && em.HasComponent<PlayerCharacter>(Victim) && !Killer.Equals(Victim))
            {
                PvPSystem.Monitor(Killer, Victim);
                if (PvPSystem.isPunishEnabled) PvPSystem.PunishCheck(Killer, Victim);
            }
            //-- ------------------------

            //-- Reduce EXP on Death by Mob/Suicide
            if (em.HasComponent<PlayerCharacter>(Victim) && (!em.HasComponent<PlayerCharacter>(Killer) || Killer.Equals(Victim)))
            {
                if (ExperienceSystem.isEXPActive) ExperienceSystem.LoseEXP(Victim);
            }
            //-- ----------------------------------
        }
    }
}
