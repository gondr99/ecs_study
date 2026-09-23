using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Agents
{
    public partial struct AgentMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (velocity, direction, speed)
                     in SystemAPI
                         .Query<RefRW<PhysicsVelocity>, 
                             RefRO<MoveDirectionComponent>, 
                             RefRO<MoveSpeedComponent>>() )
            {
                float2 moveVelocity = direction.ValueRO.Value * speed.ValueRO.Value;
                velocity.ValueRW.Linear = new float3(moveVelocity, 0);
            }
        }
    }
}