using Players;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Enemies
{
    public partial struct EnemyMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            float2 playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xy;
            
            var moveToJob = new MoveToPlayerJob
            {
                PlayerPosition = playerPosition
            };

            state.Dependency = moveToJob.ScheduleParallel(state.Dependency);

        }

    }
}