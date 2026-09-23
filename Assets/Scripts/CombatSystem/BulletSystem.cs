using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace CombatSystem
{
    public partial struct BulletSystem : ISystem
    {
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
            float dt = SystemAPI.Time.DeltaTime;
            foreach(var(bullet, trm, lifetime, destroyFlag) 
                    in SystemAPI
                        .Query<RefRO<BulletComponent>, RefRW<LocalTransform>, RefRW<LifetimeComponent>,
                            EnabledRefRW<DestroyEntityFlag>>()
                        .WithPresent<DestroyEntityFlag>())
            {
                trm.ValueRW.Position += bullet.ValueRO.Speed * dt * trm.ValueRW.Up();
                lifetime.ValueRW.RemainingLifetime -= dt;

                if (lifetime.ValueRO.RemainingLifetime <= 0f)
                {
                    destroyFlag.ValueRW = true;
                }
            }
        }

    }
}