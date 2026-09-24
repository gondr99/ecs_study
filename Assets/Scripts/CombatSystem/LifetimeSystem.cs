using Unity.Burst;
using Unity.Entities;

namespace CombatSystem
{
    public partial struct LifetimeSystem : ISystem
    {
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
            float dt = SystemAPI.Time.DeltaTime;
            foreach(var(lifetime, destroyFlag) 
                    in SystemAPI .Query<RefRW<LifetimeComponent>,
                            EnabledRefRW<DestroyEntityFlag>>()
                        .WithPresent<DestroyEntityFlag>())
            {
                lifetime.ValueRW.RemainingLifetime -= dt;

                if (lifetime.ValueRO.RemainingLifetime <= 0f)
                {
                    destroyFlag.ValueRW = true;
                }
            }
        }

    }
}