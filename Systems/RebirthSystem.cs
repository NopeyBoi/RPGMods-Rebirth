using ProjectM;
using ProjectM.Network;
using RPGMods.Utils;
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
        Database.rebirths[SteamID] += 1;
        ExperienceSystem.SetLevel(ctx.Event.SenderCharacterEntity, ctx.Event.SenderUserEntity, SteamID);
        //Output.SendLore(userEntity, $"You've been defeated,<color=#ffffffff> {EXPLostOnDeath * 100}%</color> experience is lost.");
    }

    public static int getLevel(ulong steamId)
        => Database.rebirths[steamId];
}
