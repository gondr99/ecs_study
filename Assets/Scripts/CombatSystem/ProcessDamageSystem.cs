using Agents;
using Unity.Burst;
using Unity.Entities;

namespace CombatSystem
{
    public partial struct ProcessDamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (hitPoint,damageBuffer, entity)
                     in SystemAPI.Query<RefRW<CurrentHitPoint>, DynamicBuffer<DamageThisFrame>>()
                         .WithDisabled<DestroyEntityFlag>() //아직 사망하지 않은 적들
                         .WithEntityAccess())
            {
                if(damageBuffer.IsEmpty ) continue;

                foreach (DamageThisFrame damage in damageBuffer)
                {
                    hitPoint.ValueRW.Value -= damage.Value;
                }
                damageBuffer.Clear();

                if (hitPoint.ValueRO.Value <= 0)
                {
                    SystemAPI.SetComponentEnabled<DestroyEntityFlag>(entity, true);
                }
            }
        }

    }
}