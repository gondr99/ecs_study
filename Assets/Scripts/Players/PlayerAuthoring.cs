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

        private class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity playerEntity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(playerEntity, new PlayerComponent
                {
                    MoveSpeed = authoring.moveSpeed,
                    BulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
                    NumberOfBulletSpawn = authoring.numOfBulletToSpawn,
                    BulletSpread = authoring.bulletSpread
                });
            }
        }
    }
}