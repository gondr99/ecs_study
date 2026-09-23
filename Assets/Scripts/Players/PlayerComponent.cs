using Unity.Entities;

namespace Players
{
    public struct PlayerComponent : IComponentData
    {
        public float MoveSpeed;
        public Entity BulletPrefab;
        public int NumberOfBulletSpawn;
        public float BulletSpread;
    }
}