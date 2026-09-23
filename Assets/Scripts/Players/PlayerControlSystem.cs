using CoreSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Players
{
    public partial struct PlayerControlSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<PlayerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InputComponent input = SystemAPI.GetSingleton<InputComponent>();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (localTrm, playerComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerComponent>>())
            {
                localTrm.ValueRW.Position += new float3(input.Movement * playerComponent.ValueRO.MoveSpeed * dt, 0f);
                
                float2 direction = input.AimWorldPosition - localTrm.ValueRO.Position.xy;
                localTrm.ValueRW.Rotation = quaternion.RotateZ(math.atan2(direction.y, direction.x) - math.PI * 0.5f);
            }
        }
    }
}