using CombatSystem;
using Enemies;
using Unity.Burst;
using Unity.Entities;

namespace UI
{
    //DestroyEntitySystem이 파괴 명령을 기록하기 전에, 이번 프레임에 죽은 적 수를 점수에 더한다.
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DestroyEntitySystem))]
    public partial struct KillCountSystem : ISystem
    {
        private EntityQuery _deadEnemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            //점수 싱글톤은 초기에 저장할 데이터가 없어(항상 0)서 SubScene에 bake하지 않고 여기서 직접 만든다.
            state.EntityManager.CreateSingleton<GameScore>("GameScore");

            //DestroyEntityFlag는 enableable이라 WithAll에는 켜진(=이번 프레임에 죽는) 적만 걸린다.
            _deadEnemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag, DestroyEntityFlag>().Build();
            state.RequireForUpdate(_deadEnemyQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //CalculateEntityCount도 enabled bit를 반영한다. 적을 하나씩 돌 필요가 없다.
            int killed = _deadEnemyQuery.CalculateEntityCount();
            SystemAPI.GetSingletonRW<GameScore>().ValueRW.KillCount += killed;
        }
    }
}
