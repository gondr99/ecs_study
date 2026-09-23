using CombatSystem;
using Unity.Entities;
using UnityEngine;

namespace Players
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float moveSpeed = 5f;
        public GameObject bulletPrefab;
        public int numOfBulletToSpawn = 50;
        [Range(0, 10f)] public float bulletSpread = 5f;
        [Min(0f)] public float fireInterval = 0.1f;

        private class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(playerEntity, new PlayerComponent
                {
                    MoveSpeed = authoring.moveSpeed
                });

                AddComponent(playerEntity, new ShooterComponent
                {
                    BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
                    BulletSpread = authoring.bulletSpread,
                    NumberOfBulletSpawn = authoring.numOfBulletToSpawn,
                    FireInterval = authoring.fireInterval,
                    NextFireTime = 0
                });
            }
        }
    }
}