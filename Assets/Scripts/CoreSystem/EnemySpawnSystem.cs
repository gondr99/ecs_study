using Players;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CoreSystem
{
    public partial struct EnemySpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);

            Entity playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
            float3 playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

            foreach (var (spawnState, spawnData)
                     in SystemAPI.Query<RefRW<SpawnerState>, RefRO<EnemySpawnData>>())
            {
                spawnState.ValueRW.SpawnTimer -= dt;
                if(spawnState.ValueRO.SpawnTimer > 0f) continue;

                spawnState.ValueRW.SpawnTimer = spawnData.ValueRO.SpawnInterval; //타이머 리셋

                for (int i = 0; i < spawnData.ValueRO.SpawnCount; i++)
                {
                    Entity newEnemy = ecb.Instantiate(spawnData.ValueRO.EnemyPrefab);
                    float spawnAngle = spawnState.ValueRW.Random.NextFloat(0, math.TAU); //TAU == 2Pi
                    float3 spawnPoint = new float3
                    {
                        x = math.sin(spawnAngle),
                        y = math.cos(spawnAngle),
                        z = 0
                    };
                    
                    spawnPoint *= spawnData.ValueRO.SpawnDistance;
                    spawnPoint += playerPosition;
                    
                    ecb.SetComponent(newEnemy, LocalTransform.FromPosition(spawnPoint));
                }
                
            }

        }

    }
}