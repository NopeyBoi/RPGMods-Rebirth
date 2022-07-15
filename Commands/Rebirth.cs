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
                    RebirthSystem.Rebirth(ctx);
                    ctx.Event.User.SendSystemMessage("<color=#ffffff>Congratulations!</color> You just rebirthed!");
                    ctx.Event.User.SendSystemMessage($"Your Rebirth Level: <color=#ffffff>{rebirthLevel}</color> -> <color=#ffffff>{rebirthLevel + 1}</color>");
                }
                else ctx.Event.User.SendSystemMessage($"Your Rebirth Level: <color=#ffffff>{rebirthLevel}</color> (<color=#ffffff>MAX</color>)");
            }
            else
            {
                if (rebirthLevel < RebirthSystem.MaxRebirthLevel)
                {
                    ctx.Event.User.SendSystemMessage($"Your Rebirth Level: <color=#ffffff>{rebirthLevel}</color>");
                    ctx.Event.User.SendSystemMessage($"Reach Level <color=#ffffff>{ExperienceSystem.MaxLevel}</color> to rebirth!");
                }
                else ctx.Event.User.SendSystemMessage($"Your Rebirth Level: <color=#ffffff>{rebirthLevel}</color> (<color=#ffffff>MAX</color>)");
            }
        }
        else if (ctx.Args[0].ToLower() == "top")
        {
            if (Database.rebirths.Count > 0)
            {
                ctx.Event.User.SendSystemMessage($"Top Rebirth Level Users:");
                System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong, int>> sorted = Database.rebirths.OrderByDescending(x => x.Value).ToList();
                for (int i = 0; i < 10; i++)
                {
                    if (sorted.Count <= i) break;
                    System.Collections.Generic.KeyValuePair<ulong, int> rebirth = sorted[i];

                    string name = Helper.GetNameFromSteamID(rebirth.Key);
                    int level = rebirth.Value;

                    ctx.Event.User.SendSystemMessage($"<color=#ffffff>#{i + 1}</color> - <color=#ffffff>{name}</color> - <color=#ffffff>{level} Rebirth(s)</color>");
                }
            }
            else Output.CustomErrorMessage(ctx, "No one has done a rebirth yet.");
        }
    }
}
