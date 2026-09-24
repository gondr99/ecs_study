using Enemies;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;


namespace CombatSystem
{
    public struct BulletAttackJob : ITriggerEventsJob
    {
        //잡에서는 직접적으로 컴포넌트를 가져올 수 없어서 LookUp을 만들어서 전달해줘야 한다.
        [ReadOnly] public ComponentLookup<EnemyTag> EnemyLookup;
        [ReadOnly] public ComponentLookup<BulletComponent> BulletLookup;
    
        //위의 2개는 무엇인 총알이고 무엇이 적인지 판별하기 위해 필요하고, 아래 2개는 데미지를 기록하고 총알을 없애기 위해 필요
        public BufferLookup<DamageThisFrame> DamageBufferLookup;
        public ComponentLookup<DestroyEntityFlag> DestroyEntityFlagLookup;
        
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity bulletEntity, enemyEntity;

            //뭐가 뭔지 모르는 채로 충돌한 2개가 잡을 통해 들어오기때문에 컴포넌트 소유여부로 판단해야 한다.
            if (BulletLookup.HasComponent(triggerEvent.EntityA)
                && EnemyLookup.HasComponent(triggerEvent.EntityB))
            {
                bulletEntity = triggerEvent.EntityA;
                enemyEntity = triggerEvent.EntityB;
            }else if (BulletLookup.HasComponent(triggerEvent.EntityB)
                      && EnemyLookup.HasComponent(triggerEvent.EntityA))
            {
                bulletEntity = triggerEvent.EntityB;
                enemyEntity = triggerEvent.EntityA;
            }
            else
            {
                return; //총알과 적과의 충돌이 아니니 무시해야 한다.
            }
            
            //같은 스텝에 총알 하나가 적 여럿과 겹치면 이벤트가 여러 개 온다. 이미 명중 처리된 총알이면 무시.
            //(Schedule로 단일 스레드 실행이라 안전. ScheduleParallel이면 race가 생긴다)
            if (DestroyEntityFlagLookup.IsComponentEnabled(bulletEntity))
                return;

            //적에게 데미지 버퍼가 없으면(AgentAuthoring이 빠진 적) 예외 대신 무시한다.
            if (!DamageBufferLookup.TryGetBuffer(enemyEntity, out DynamicBuffer<DamageThisFrame> enemyDamageBuffer))
                return;
            
            //룩업은 Entity.Index로 (청크, 청크 내 인덱스)를 배열에서 바로 찾고,
            //아키타입에서 T의 위치를 찾아 포인터를 계산한다. 해시 비용은 없지만 청크가 흩어져 있으면
            //캐시 미스가 나는 랜덤 액세스라 쿼리 순회보다 느리다.
            int damage = BulletLookup[bulletEntity].Damage;
            enemyDamageBuffer.Add(new DamageThisFrame{Value = damage});
            
            DestroyEntityFlagLookup.SetComponentEnabled(bulletEntity, true);
        }

    }
}