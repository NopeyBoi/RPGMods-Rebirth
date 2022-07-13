using ProjectM;
using ProjectM.Network;
using RPGMods.Utils;
using Wetstone.API;

namespace RPGMods.Commands;

[Command("health, hp", Usage = "health <percentage> [<player name>]", Description = "Sets your current Health")]
public static class Health
{
    public static void Initialize(Context ctx)
    {
        Unity.Collections.FixedString64 PlayerName = ctx.Event.User.CharacterName;
        int UserIndex = ctx.Event.User.Index;
        ProjectM.Health component = ctx.EntityManager.GetComponentData<ProjectM.Health>(ctx.Event.SenderCharacterEntity);
        int Value = 100;
        if (ctx.Args.Length != 0)
        {
            if (!int.TryParse(ctx.Args[0], out _))
            {
                Utils.Output.InvalidArguments(ctx);
                return;
            }
            else Value = int.Parse(ctx.Args[0]);
        }

        if (ctx.Args.Length == 2)
        {
            string targetName = ctx.Args[1];
            if (Helper.FindPlayer(targetName, true, out Unity.Entities.Entity targetEntity, out Unity.Entities.Entity targetUserEntity))
            {
                PlayerName = targetName;
                UserIndex = VWorld.Server.EntityManager.GetComponentData<User>(targetUserEntity).Index;
                component = VWorld.Server.EntityManager.GetComponentData<ProjectM.Health>(targetEntity);
            }
            else
            {
                Utils.Output.CustomErrorMessage(ctx, $"Player \"{targetName}\" not found.");
                return;
            }
        }

        float restore_hp = (component.MaxHealth / 100 * Value) - component.Value;

        ChangeHealthDebugEvent HealthEvent = new ChangeHealthDebugEvent()
        {
            Amount = (int)restore_hp
        };
        VWorld.Server.GetExistingSystem<DebugEventsSystem>().ChangeHealthEvent(UserIndex, ref HealthEvent);

        ctx.Event.User.SendSystemMessage($"Player \"{PlayerName}\" Health set to <color=#ffff00ff>{Value}%</color>");
    }
}
