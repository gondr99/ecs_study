using Unity.Burst;
using Unity.Entities;

namespace CombatSystem
{
    //해당 그룹에서 가장 마지막에 실행해줘라. 단 시뮬레이션 종료 버퍼가 실행되기전에는 해야한다.
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]

    public partial struct DestroyEntitySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var endEcbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var endEcb = endEcbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (_,entity) in SystemAPI.Query<RefRO<DestroyEntityFlag>>().WithEntityAccess())
            {
                //여기서 만약 특정 컴포넌트나 태그를 가진 녀석이 있다면 추가적인 로직을 수행해줄 수 도 있다.
                endEcb.DestroyEntity(entity);
                
                //또한 사망시 무언가 아이템을 드랍한다면 여기에 Begin도 필요하다.
            }

        }

    }
}