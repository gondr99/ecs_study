using Agents;
using CoreSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Players
{
    public partial struct PlayerControlSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InputComponent input = SystemAPI.GetSingleton<InputComponent>();

            foreach (var (localTrm, moveDirection) 
                     in SystemAPI.Query<RefRW<LocalTransform>, 
                         RefRW<MoveDirectionComponent>>()
                         .WithAll<PlayerTag>())
            {
                moveDirection.ValueRW.Value = input.Movement;
                
                float2 direction = input.AimWorldPosition - localTrm.ValueRO.Position.xy;
                localTrm.ValueRW.Rotation = quaternion.RotateZ(math.atan2(direction.y, direction.x) - math.PI * 0.5f);
            }
        }
    }
}