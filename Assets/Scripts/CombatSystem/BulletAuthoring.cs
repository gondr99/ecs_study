using Unity.Entities;
using UnityEngine;

namespace CombatSystem
{
    public class BulletAuthoring : MonoBehaviour
    {
        public float bulletSpeed;
        public float bulletLifeTime;

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
                    Speed = authoring.bulletSpeed
                });
                
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}