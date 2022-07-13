using ProjectM.Network;
using RPGMods.Systems;
using RPGMods.Utils;
using System.Linq;
using Wetstone.API;

namespace RPGMods.Commands;

[Command("permission, perm", Usage = "permission <list>|<save>|<reload>|<set> <0-100> <playername>|<steamid>", Description = "Manage commands and user permissions level.")]
public static class Permission
{
    public static void Initialize(Context ctx)
    {
        string[] args = ctx.Args;

        if (args.Length == 1)
        {
            if (args[0].ToLower().Equals("list"))
            {
                System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong, int>> SortedPermission = Database.user_permission.ToList();
                SortedPermission.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));
                System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong, int>> ListPermission = SortedPermission;
                ctx.Event.User.SendSystemMessage($"===================================");
                int i = 0;
                foreach (System.Collections.Generic.KeyValuePair<ulong, int> result in ListPermission)
                {
                    i++;
                    ctx.Event.User.SendSystemMessage($"{i}. <color=#ffffffff>{Helper.GetNameFromSteamID(result.Key)} : {result.Value}</color>");
                }
                if (i == 0) ctx.Event.User.SendSystemMessage($"<color=#ffffffff>No Result</color>");
                ctx.Event.User.SendSystemMessage($"===================================");
            }
            else if (args[0].ToLower().Equals("save"))
            {
                PermissionSystem.SaveUserPermission();
                ctx.Event.User.SendSystemMessage("Saved user permission to JSON file.");
            }
            else if (args[0].ToLower().Equals("reload"))
            {
                PermissionSystem.LoadPermissions();
                ctx.Event.User.SendSystemMessage("Reloaded permission from JSON file.");
            }
            else
            {
                Output.MissingArguments(ctx);
            }
            return;
        }

        if (args.Length < 3)
        {
            Output.MissingArguments(ctx);
            return;
        }

        if (args[0].ToLower().Equals("set"))
        {
            bool tryParse = int.TryParse(args[1], out int level);
            if (!tryParse)
            {
                Output.InvalidArguments(ctx);
                return;
            }
            else
            {
                if (level < 0 || level > 100)
                {
                    Output.InvalidArguments(ctx);
                    return;
                }
            }

            bool tryParse_2 = ulong.TryParse(args[2], out ulong SteamID);
            string playerName = null;
            if (!tryParse_2)
            {
                bool tryFind = Helper.FindPlayer(args[2], false, out Unity.Entities.Entity target_playerEntity, out Unity.Entities.Entity target_userEntity);
                if (!tryFind)
                {
                    Output.CustomErrorMessage(ctx, $"Could not find specified player \"{args[2]}\".");
                    return;
                }
                playerName = args[2];
                SteamID = VWorld.Server.EntityManager.GetComponentData<User>(target_userEntity).PlatformId;
            }
            else
            {
                playerName = Helper.GetNameFromSteamID(SteamID);
                if (playerName == null)
                {
                    Output.CustomErrorMessage(ctx, $"Could not find specified player steam id \"{args[2]}\".");
                    return;
                }
            }

            if (level == 0) _ = Database.user_permission.Remove(SteamID);
            else Database.user_permission[SteamID] = level;

            ctx.Event.User.SendSystemMessage($"Player \"{playerName}\" permission is now set to<color=#ffffffff> {level}</color>.");
            return;
        }
        else
        {
            Output.InvalidArguments(ctx);
            return;
        }
    }
}
