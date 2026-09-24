using Enemies;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace CombatSystem
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct BulletAttackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var attackJob = new BulletAttackJob
            {
                EnemyLookup = SystemAPI.GetComponentLookup<EnemyTag>(true),
                BulletLookup = SystemAPI.GetComponentLookup<BulletComponent>(true),
                DamageBufferLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
                DestroyEntityFlagLookup = SystemAPI.GetComponentLookup<DestroyEntityFlag>()
            };

            //물리 시뮬레이션 싱글톤에 잡을 등록한다.
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            state.Dependency = attackJob.Schedule(simulationSingleton, state.Dependency);
        }

    }
}