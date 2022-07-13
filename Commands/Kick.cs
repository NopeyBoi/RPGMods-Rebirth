using RPGMods.Utils;
using Wetstone.API;

namespace RPGMods.Commands;

[Command("kick", Usage = "kick <playername>", Description = "Kick the specified player out of the server.")]
public static class Kick
{
    public static void Initialize(Context ctx)
    {
        string[] args = ctx.Args;
        if (args.Length < 1)
        {
            Output.MissingArguments(ctx);
            return;
        }

        string name = args[0];
        if (Helper.FindPlayer(name, true, out _, out Unity.Entities.Entity targetUserEntity))
        {
            Helper.KickPlayer(targetUserEntity);
            ctx.Event.User.SendSystemMessage($"Player \"{name}\" has been kicked from server.");
        }
        else
        {
            Output.CustomErrorMessage(ctx, "Specified player not found.");
        }
    }
}
