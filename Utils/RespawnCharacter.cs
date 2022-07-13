using ProjectM;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Wetstone.API;

namespace RPGMods.Utils;

struct NullableFloat
{
    public float3 value;
    public bool has_value;
}
public class RespawnCharacter
{
    private static EntityManager entityManager = VWorld.Server.EntityManager;
    public static void Respawn(Entity VictimEntity, PlayerCharacter player, Entity userEntity)
    {
        EntityCommandBufferSystem bufferSystem = VWorld.Server.GetOrCreateSystem<EntityCommandBufferSystem>();
        EntityCommandBufferSafe commandBufferSafe = new EntityCommandBufferSafe(Allocator.Temp)
        {
            Unsafe = bufferSystem.CreateCommandBuffer()
        };

        unsafe
        {
            float2 playerLocation = player.LastValidPosition;

            byte* bytes = stackalloc byte[Marshal.SizeOf<NullableFloat>()];
            IntPtr bytePtr = new IntPtr(bytes);
            Marshal.StructureToPtr<NullableFloat>(new()
            {
                value = new float3(playerLocation.x, 0, playerLocation.y),
                has_value = true
            }, bytePtr, false);
            IntPtr boxedBytePtr = IntPtr.Subtract(bytePtr, 0x10);

            Il2CppSystem.Nullable<float3> spawnLocation = new Il2CppSystem.Nullable<float3>(boxedBytePtr);
            ServerBootstrapSystem server = VWorld.Server.GetOrCreateSystem<ServerBootstrapSystem>();

            _ = server.RespawnCharacter(commandBufferSafe, userEntity, customSpawnLocation: spawnLocation, previousCharacter: VictimEntity, fadeOutEntity: userEntity);
        }
    }
}
