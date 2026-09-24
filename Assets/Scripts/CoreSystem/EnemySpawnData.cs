using Unity.Entities;

namespace CoreSystem
{
    public struct EnemySpawnData : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public float SpawnDistance;
        public int SpawnCount;
    }
}