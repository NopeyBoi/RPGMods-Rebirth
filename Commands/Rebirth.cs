using RPGMods.Systems;
using RPGMods.Utils;

namespace RPGMods.Commands;

[Command("rebirth", Usage = "rebirth [top]", Description = "Resets your level to add bonus stats.")]
public static class Rebirth
{
    public static void Initialize(Context ctx)
    {
        if (ctx.Args.Length == 0)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            int level = ExperienceSystem.getLevel(steamId);
            int rebirthLevel = RebirthSystem.getLevel(steamId);

            if (level >= ExperienceSystem.MaxLevel)
            {
                if (rebirthLevel < RebirthSystem.MaxRebirthLevel)
                {
                    // Do the level reset process in here
                    RebirthSystem.Rebirth(ctx);
                }
                else Output.CustomErrorMessage(ctx, "You have already reached the highest rebirth level!");
            }
            else Output.CustomErrorMessage(ctx, "You have not reached max level yet.");
        }
        else if (ctx.Args[0].ToLower() == "top")
        {
            // show top rebirth leaderboards
        }
    }
}
