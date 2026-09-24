using CombatSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Effects
{
    //DestroyEntitySystem이 파괴 명령을 기록하기 전에, 이번 프레임에 죽은 엔티티 자리에 이펙트를 만든다.
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(DestroyEntitySystem))]
    public partial struct SpawnExplosionEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //Begin(다음 프레임 시작)에 생성해야 TransformSystemGroup이 LocalToWorld를 계산한 뒤 렌더링된다.
            //End에서 만들면 첫 프레임은 프리팹 원래 위치(LocalToWorld)로 한 번 그려진다.
            var ecbSystem = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            //DestroyEntityFlag가 켜진(=이번 프레임에 죽는) 엔티티만 쿼리된다.(WithAll은 켜진 녀석만)
            foreach (var (expEffect, localTrm)
                     in SystemAPI.Query<RefRO<ExplosionComponent>, RefRO<LocalTransform>>()
                         .WithAll<DestroyEntityFlag>())
            {
                Entity prefab = expEffect.ValueRO.Prefab;
                Entity effect = ecb.Instantiate(prefab);

                //프리팹의 회전/스케일은 유지하고 위치만 죽은 자리로 옮긴다.
                LocalTransform effectTrm = SystemAPI.GetComponent<LocalTransform>(prefab);
                effectTrm.Position = localTrm.ValueRO.Position;
                ecb.SetComponent(effect, effectTrm);
            }
        }
    }
}
