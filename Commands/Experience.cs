using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using Unity.Entities;
using Wetstone.API;

namespace RPGMods.Commands;

[Command("experience, exp, xp", Usage = "experience [<log> <on>|<off>]", Description = "Shows your currect experience and progression to next level, or toggle the exp gain notification.")]
public static class Experience
{
    private static EntityManager entityManager = VWorld.Server.EntityManager;
    public static void Initialize(Context ctx)
    {
        User user = ctx.Event.User;
        string CharName = user.CharacterName.ToString();
        ulong SteamID = user.PlatformId;
        Entity PlayerCharacter = ctx.Event.SenderCharacterEntity;
        Entity UserEntity = ctx.Event.SenderUserEntity;

        if (!ExperienceSystem.isEXPActive)
        {
            Output.CustomErrorMessage(ctx, "Experience system is not enabled.");
            return;
        }

        if (ctx.Args.Length >= 2)
        {
            bool isAllowed = ctx.Event.User.IsAdmin || PermissionSystem.PermissionCheck(ctx.Event.User.PlatformId, "experience_args");
            if (ctx.Args[0].Equals("set") && isAllowed && int.TryParse(ctx.Args[1], out int value))
            {
                if (ctx.Args.Length == 3)
                {
                    string name = ctx.Args[2];
                    if (Helper.FindPlayer(name, true, out Entity targetEntity, out Entity targetUserEntity))
                    {
                        CharName = name;
                        SteamID = entityManager.GetComponentData<User>(targetUserEntity).PlatformId;
                        PlayerCharacter = targetEntity;
                        UserEntity = targetUserEntity;
                    }
                    else
                    {
                        Output.CustomErrorMessage(ctx, $"Could not find specified player \"{name}\".");
                        return;
                    }
                }
                Database.player_experience[SteamID] = value;
                ExperienceSystem.SetLevel(PlayerCharacter, UserEntity, SteamID);
                user.SendSystemMessage($"Player \"{CharName}\" Experience is now set to be<color=#ffffffff> {ExperienceSystem.getXp(SteamID)}</color>");
            }
            else if (ctx.Args[0].ToLower().Equals("log"))
            {
                if (ctx.Args[1].ToLower().Equals("on"))
                {
                    Database.player_log_exp[SteamID] = true;
                    user.SendSystemMessage($"Experience gain is now logged.");
                    return;
                }
                else if (ctx.Args[1].ToLower().Equals("off"))
                {
                    Database.player_log_exp[SteamID] = false;
                    user.SendSystemMessage($"Experience gain is no longer being logged.");
                    return;
                }
            }
            else
            {
                Output.InvalidArguments(ctx);
                return;
            }
        }
        else
        {
            int userLevel = ExperienceSystem.getLevel(SteamID);
            user.SendSystemMessage($"-- <color=#ffffffff>{CharName}</color> --");
            user.SendSystemMessage(
                $"Level:<color=#ffffffff> {userLevel}</color> (<color=#ffffffff>{ExperienceSystem.getLevelProgress(SteamID)}%</color>) " +
                $" [ XP:<color=#ffffffff> {ExperienceSystem.getXp(SteamID)}</color>/<color=#ffffffff>{ExperienceSystem.convertLevelToXp(userLevel + 1)}</color> ]");
        }
    }
}
