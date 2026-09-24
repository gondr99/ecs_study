using Agents;
using Unity.Entities;
using UnityEngine;

namespace CombatSystem
{
    public class BulletAuthoring : MonoBehaviour
    {
        public float bulletSpeed;
        public float bulletLifeTime;
        public int damage;

        private class BulletAuthoringBaker : Baker<BulletAuthoring>
        {
            public override void Bake(BulletAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new LifetimeComponent
                {
                    RemainingLifetime = authoring.bulletLifeTime
                });
                
                AddComponent(entity, new BulletComponent
                {
                    Damage = authoring.damage
                });
                
                AddComponent(entity, new MoveSpeedComponent
                {
                    Value = authoring.bulletSpeed
                });
                
                AddComponent<MoveDirectionComponent>(entity);
                
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}