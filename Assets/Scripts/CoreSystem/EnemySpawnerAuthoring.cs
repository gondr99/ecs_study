using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CoreSystem
{
    public struct SpawnerState : IComponentData
    {
        public float SpawnTimer;
        public Random Random; //유니티 매스 랜덤.
    }


    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public float spawnInterval = 0.3f;
        public int spawnCount = 50;
        public float spawnDistance = 10;
        public uint randomSeed;

        private class EnemySpawnerAuthoringBaker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                //데이터 생성 전용 게임오브젝트라 None으로 가져온다.
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new EnemySpawnData
                {
                    EnemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                    SpawnCount = authoring.spawnCount,
                    SpawnInterval = authoring.spawnInterval,
                    SpawnDistance = authoring.spawnDistance
                });

                AddComponent(entity, new SpawnerState
                {
                    SpawnTimer = 0f,
                    Random = Random.CreateFromIndex(authoring.randomSeed)
                });
            }
        }
    }
}