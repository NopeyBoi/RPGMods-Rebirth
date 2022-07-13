using ProjectM;
using RPGMods.Utils;
using Unity.Entities;
using Wetstone.API;

namespace RPGMods.Commands;

[Command("resetcooldown, cd", Usage = "resetcooldown [<Player Name>]", Description = "Instantly cooldown all ability & skills for the player.")]
public static class ResetCooldown
{
    public static void Initialize(Context ctx)
    {
        Entity PlayerCharacter = ctx.Event.SenderCharacterEntity;
        string CharName = ctx.Event.User.CharacterName.ToString();
        EntityManager entityManager = VWorld.Server.EntityManager;

        if (ctx.Args.Length >= 1)
        {
            string name = string.Join(' ', ctx.Args);
            if (Helper.FindPlayer(name, true, out Entity targetEntity, out _))
            {
                PlayerCharacter = targetEntity;
                CharName = name;
            }
            else
            {
                Utils.Output.CustomErrorMessage(ctx, $"Could not find the specified player \"{name}\".");
                return;
            }
        }

        DynamicBuffer<AbilityGroupSlotBuffer> AbilityBuffer = entityManager.GetBuffer<AbilityGroupSlotBuffer>(PlayerCharacter);
        for (int i = 0; i < AbilityBuffer.Length; i++)
        {
            Entity AbilitySlot = AbilityBuffer[i].GroupSlotEntity._Entity;
            AbilityGroupSlot ActiveAbility = entityManager.GetComponentData<AbilityGroupSlot>(AbilitySlot);
            Entity ActiveAbility_Entity = ActiveAbility.StateEntity._Entity;

            PrefabGUID b = Helper.GetPrefabGUID(ActiveAbility_Entity);
            if (b.GuidHash == 0) continue;

            DynamicBuffer<AbilityStateBuffer> AbilityStateBuffer = entityManager.GetBuffer<AbilityStateBuffer>(ActiveAbility_Entity);
            for (int c_i = 0; c_i < AbilityStateBuffer.Length; c_i++)
            {
                Entity abilityState = AbilityStateBuffer[c_i].StateEntity._Entity;
                AbilityCooldownState abilityCooldownState = entityManager.GetComponentData<AbilityCooldownState>(abilityState);
                abilityCooldownState.CooldownEndTime = 0;
                entityManager.SetComponentData(abilityState, abilityCooldownState);
            }
        }
        ctx.Event.User.SendSystemMessage($"Player \"{CharName}\" cooldown resetted.");
    }
}