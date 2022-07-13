using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using Stunlock.Network;
using System;

namespace RPGMods.Hooks;

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
public class OnUserConnected_Patch
{
    public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
    {
        try
        {
            Unity.Entities.EntityManager em = __instance.EntityManager;
            int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
            ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
            Unity.Entities.Entity userEntity = serverClient.UserEntity;
            User userData = __instance.EntityManager.GetComponentData<User>(userEntity);
            bool isNewVampire = userData.CharacterName.IsEmpty;

            if (!isNewVampire)
            {
                if (WeaponMasterSystem.isDecaySystemEnabled && WeaponMasterSystem.isMasteryEnabled)
                {
                    WeaponMasterSystem.DecayMastery(userEntity);
                }
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
public static class OnUserDisconnected_Patch
{
    private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason, string extraData)
    {
        bool process;
        switch (connectionStatusReason)
        {
            case ConnectionStatusChangeReason.IncorrectPassword:
                process = false;
                break;
            case ConnectionStatusChangeReason.Unknown:
                process = false;
                break;
            case ConnectionStatusChangeReason.NoFreeSlots:
                process = false;
                break;
            case ConnectionStatusChangeReason.Banned:
                process = false;
                break;
            default:
                process = true;
                break;
        }
        if (process)
        {
            try
            {
                int userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                ServerBootstrapSystem.ServerClient serverClient = __instance._ApprovedUsersLookup[userIndex];
                User userData = __instance.EntityManager.GetComponentData<User>(serverClient.UserEntity);
                bool isNewVampire = userData.CharacterName.IsEmpty;

                if (!isNewVampire)
                {
                    if (WeaponMasterSystem.isDecaySystemEnabled)
                    {
                        Database.player_decaymastery_logout[userData.PlatformId] = DateTime.Now;
                    }
                }
            }
            catch { };
        }
    }
}

[HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnDestroy))]
public static class OnDestroy_Patch
{
    private static void Prefix()
    {
        AutoSaveSystem.SaveDatabase();
    }
}