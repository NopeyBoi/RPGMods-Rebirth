using ProjectM;
using ProjectM.Network;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Wetstone.API;
using static RPGMods.Utils.Database;

namespace RPGMods.Utils;

public static class Helper
{
    private static Entity empty_entity = new Entity();

    public static void ApplyBuff(Entity User, Entity Char, PrefabGUID GUID)
    {
        DebugEventsSystem des = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
        FromCharacter fromCharacter = new FromCharacter()
        {
            User = User,
            Character = Char
        };
        ApplyBuffDebugEvent buffEvent = new ApplyBuffDebugEvent()
        {
            BuffPrefabGUID = GUID
        };
        des.ApplyBuff(fromCharacter, buffEvent);
    }

    public static void RemoveBuff(Entity Char, PrefabGUID GUID)
    {
        if (BuffUtility.HasBuff(VWorld.Server.EntityManager, Char, GUID))
        {
            _ = BuffUtility.TryGetBuff(VWorld.Server.EntityManager, Char, GUID, out Entity BuffEntity_);
            _ = VWorld.Server.EntityManager.AddComponent<DestroyTag>(BuffEntity_);
            return;
        }
    }

    public static string GetNameFromSteamID(ulong SteamID)
    {
        NativeArray<Entity> UserEntities = VWorld.Server.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);
        foreach (Entity Entity in UserEntities)
        {
            User EntityData = VWorld.Server.EntityManager.GetComponentData<User>(Entity);
            if (EntityData.PlatformId == SteamID) return EntityData.CharacterName.ToString();
        }
        return null;
    }

    public static PrefabGUID GetGUIDFromName(string name)
    {
        GameDataSystem gameDataSystem = VWorld.Server.GetExistingSystem<GameDataSystem>();
        ManagedDataRegistry managed = gameDataSystem.ManagedDataRegistry;

        foreach (Unity.Collections.LowLevel.Unsafe.KeyValue<PrefabGUID, ItemData> entry in gameDataSystem.ItemHashLookupMap)
        {
            try
            {
                ManagedItemData item = managed.GetOrDefault<ManagedItemData>(entry.Key);
                if (item.PrefabName.StartsWith("Item_VBloodSource") || item.PrefabName.StartsWith("GM_Unit_Creature_Base") || item.PrefabName == "Item_Cloak_ShadowPriest") continue;
                if (item.Name.ToString().ToLower() == name.ToLower())
                {
                    return entry.Key;
                }
            }
            catch { }
        }

        return new PrefabGUID(0);
    }

    public static void KickPlayer(Entity userEntity)
    {
        EntityManager em = VWorld.Server.EntityManager;
        User userData = em.GetComponentData<User>(userEntity);
        int index = userData.Index;
        _ = em.GetComponentData<NetworkId>(userEntity);

        Entity entity = em.CreateEntity(
            ComponentType.ReadOnly<NetworkEventType>(),
            ComponentType.ReadOnly<SendEventToUser>(),
            ComponentType.ReadOnly<KickEvent>()
        );

        KickEvent KickEvent = new KickEvent()
        {
            PlatformId = userData.PlatformId
        };

        em.SetComponentData<SendEventToUser>(entity, new()
        {
            UserIndex = index
        });
        em.SetComponentData<NetworkEventType>(entity, new()
        {
            EventId = NetworkEvents.EventId_KickEvent,
            IsAdminEvent = false,
            IsDebugEvent = false
        });

        em.SetComponentData(entity, KickEvent);
    }

    public static Entity AddItemToInventory(Context ctx, PrefabGUID guid, int amount)
    {
        unsafe
        {
            GameDataSystem gameData = VWorld.Server.GetExistingSystem<GameDataSystem>();
            byte* bytes = stackalloc byte[Marshal.SizeOf<FakeNull>()];
            IntPtr bytePtr = new IntPtr(bytes);
            Marshal.StructureToPtr<FakeNull>(new()
            {
                value = 7,
                has_value = true
            }, bytePtr, false);
            IntPtr boxedBytePtr = IntPtr.Subtract(bytePtr, 0x10);
            Il2CppSystem.Nullable<int> hack = new Il2CppSystem.Nullable<int>(boxedBytePtr);
            bool hasAdded = InventoryUtilitiesServer.TryAddItem(ctx.EntityManager, gameData.ItemHashLookupMap, ctx.Event.SenderCharacterEntity, guid, amount, out _, out Entity e, default, hack);
            return e;
        }
    }

    public static BloodType GetBloodTypeFromName(string name)
    {
        BloodType type = BloodType.Frailed;
        if (Enum.IsDefined(typeof(BloodType), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name)))
            _ = Enum.TryParse(name, true, out type);
        return type;
    }

    public static PrefabGUID GetSourceTypeFromName(string name)
    {
        PrefabGUID type;
        name = name.ToLower();
        if (name.Equals("brute")) type = new PrefabGUID(-1464869978);
        else if (name.Equals("warrior")) type = new PrefabGUID(-1128238456);
        else if (name.Equals("rogue")) type = new PrefabGUID(-1030822544);
        else if (name.Equals("scholar")) type = new PrefabGUID(-700632469);
        else if (name.Equals("creature")) type = new PrefabGUID(1897056612);
        else if (name.Equals("worker")) type = new PrefabGUID(-1342764880);
        else type = new PrefabGUID();
        return type;
    }

    public static bool FindPlayer(string name, bool mustOnline, out Entity playerEntity, out Entity userEntity)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        foreach (Entity UsersEntity in entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp))
        {
            User target_component = entityManager.GetComponentData<User>(UsersEntity);
            if (mustOnline)
            {
                if (!target_component.IsConnected) continue;
            }


            string CharName = target_component.CharacterName.ToString();
            if (CharName.Equals(name))
            {
                userEntity = UsersEntity;
                playerEntity = target_component.LocalCharacter._Entity;
                return true;
            }
        }
        playerEntity = empty_entity;
        userEntity = empty_entity;
        return false;
    }

    public static bool IsPlayerInCombat(Entity player)
    {
        return BuffUtility.HasBuff(VWorld.Server.EntityManager, player, buff.InCombat) || BuffUtility.HasBuff(VWorld.Server.EntityManager, player, buff.InCombat_PvP);
    }

    public static bool IsPlayerHasBuff(Entity player, PrefabGUID BuffGUID)
    {
        return BuffUtility.HasBuff(VWorld.Server.EntityManager, player, BuffGUID);
    }

    public static void SetPvPShield(Entity character, bool value)
    {
        EntityManager em = VWorld.Server.EntityManager;
        UnitStats cUnitStats = em.GetComponentData<UnitStats>(character);
        DynamicBuffer<BoolModificationBuffer> cBuffer = em.GetBuffer<BoolModificationBuffer>(character);
        cUnitStats.PvPProtected.Set(value, cBuffer);
        em.SetComponentData(character, cUnitStats);
    }

    public static bool SpawnAtPosition(Entity user, string name, int count, float2 position, float minRange = 1, float maxRange = 2, float duration = -1)
    {
        bool isFound = database_units.TryGetValue(name, out PrefabGUID unit);
        if (!isFound) return false;

        Translation translation = VWorld.Server.EntityManager.GetComponentData<Translation>(user);
        float3 f3pos = new float3(position.x, translation.Value.y, position.y);
        VWorld.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, f3pos, count, minRange, maxRange, duration);
        return true;
    }

    public static bool SpawnAtPosition(Entity user, int GUID, int count, float2 position, float minRange = 1, float maxRange = 2, float duration = -1)
    {
        PrefabGUID unit = new PrefabGUID(GUID);

        Translation translation = VWorld.Server.EntityManager.GetComponentData<Translation>(user);
        float3 f3pos = new float3(position.x, translation.Value.y, position.y);
        try
        {
            VWorld.Server.GetExistingSystem<UnitSpawnerUpdateSystem>().SpawnUnit(empty_entity, unit, f3pos, count, minRange, maxRange, duration);
        }
        catch
        {
            return false;
        }
        return true;
    }

    public static PrefabGUID GetPrefabGUID(Entity entity)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        PrefabGUID guid;
        try
        {
            guid = entityManager.GetComponentData<PrefabGUID>(entity);
        }
        catch
        {
            guid.GuidHash = 0;
        }
        return guid;
    }

    public static string GetPrefabName(PrefabGUID hashCode)
    {
        PrefabCollectionSystem s = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>();
        string name = "Nonexistent";
        if (hashCode.GuidHash == 0)
        {
            return name;
        }
        try
        {
            name = s.PrefabNameLookupMap[hashCode].ToString();
        }
        catch
        {
            name = "NoPrefabName";
        }
        return name;
    }

    public static void TeleportTo(Context ctx, Float2 position)
    {
        Entity entity = ctx.EntityManager.CreateEntity(
                ComponentType.ReadWrite<FromCharacter>(),
                ComponentType.ReadWrite<PlayerTeleportDebugEvent>()
            );

        ctx.EntityManager.SetComponentData<FromCharacter>(entity, new()
        {
            User = ctx.Event.SenderUserEntity,
            Character = ctx.Event.SenderCharacterEntity
        });

        ctx.EntityManager.SetComponentData<PlayerTeleportDebugEvent>(entity, new()
        {
            Position = new float2(position.x, position.y),
            Target = PlayerTeleportDebugEvent.TeleportTarget.Self
        });
    }

    struct FakeNull
    {
        public int value;
        public bool has_value;
    }

    public enum BloodType
    {
        Frailed = -899826404,
        Creature = -77658840,
        Warrior = -1094467405,
        Rogue = 793735874,
        Brute = 581377887,
        Scholar = -586506765,
        Worker = -540707191
    }
}
