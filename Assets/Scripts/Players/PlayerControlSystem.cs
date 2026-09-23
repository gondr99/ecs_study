using CoreSystem;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;


namespace Players
{
    public partial struct PlayerControlSystem : ISystem
    {
        private Random _random; 
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<PlayerComponent>();
            _random = Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InputComponent input = SystemAPI.GetSingleton<InputComponent>();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (localTrm, playerComponent) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerComponent>>())
            {
                localTrm.ValueRW.Position += new float3(input.Movement * playerComponent.ValueRO.MoveSpeed * dt, 0f);
                
                float2 direction = input.AimWorldPosition - localTrm.ValueRO.Position.xy;
                localTrm.ValueRW.Rotation = quaternion.RotateZ(math.atan2(direction.y, direction.x) - math.PI * 0.5f);
                
                if (input.Shoot)
                {
                    var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
                    EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            
                    Entity newBullet = ecb.Instantiate(playerComponent.ValueRO.BulletPrefab);
                    

                    float spread = playerComponent.ValueRO.BulletSpread;
                    float randomOffset = _random.NextFloat(-spread, spread);
                    var trm = localTrm.ValueRO;
                    float3 spawnPosition = trm.Position + trm.Up() * 1.5f + trm.Right() * randomOffset;
                    
                    ecb.SetComponent(newBullet, LocalTransform.FromPositionRotation(spawnPosition, localTrm.ValueRW.Rotation));
                }
            }

        }
    }
}