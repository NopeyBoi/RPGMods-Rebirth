using RPGMods.Systems;
using RPGMods.Utils;
using System.Linq;
using Wetstone.API;

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
                    ctx.Event.User.SendSystemMessage("Congratulations! You just rebirthed!");
                    ctx.Event.User.SendSystemMessage($"Your Rebirth Level: {rebirthLevel} -> {rebirthLevel + 1}");
                }
                else
                {
                    ctx.Event.User.SendSystemMessage($"Your Rebirth Level: {rebirthLevel}");
                    ctx.Event.User.SendSystemMessage($"Reach Level {ExperienceSystem.MaxLevel} to rebirth!");
                }
            }
            else Output.CustomErrorMessage(ctx, "You have not reached max level yet.");
        }
        else if (ctx.Args[0].ToLower() == "top")
        {
            if (Database.rebirths.Count > 0)
            {
                ctx.Event.User.SendSystemMessage($"Top Rebirth Level Users:");
                var sorted = Database.rebirths.OrderByDescending(x => x.Value).ToList();
                foreach (var rebirth in sorted)
                {
                    string name = Helper.GetNameFromSteamID(rebirth.Key);
                    int level = rebirth.Value;


                }
            }
            else ctx.Event.User.SendSystemMessage("No one has done a rebirth yet.");
        }
    }
}
