using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace Agents
{
    public struct InitAgentFlag : IComponentData, IEnableableComponent { }
    
    //물리가 돌기전에 넣어서 물리연산전에 관성이 무한대가 되도록 함.
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AgentInitSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (mass, shouldInit)
                     in SystemAPI.Query<RefRW<PhysicsMass>, EnabledRefRW<InitAgentFlag>>())
            {
                mass.ValueRW.InverseInertia = float3.zero;
                shouldInit.ValueRW = false;
            }
        }
    }
}