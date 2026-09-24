using Agents;
using CombatSystem;
using CoreSystem;
using Sounds;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Players
{
    //조준 회전이 반영된 뒤에 발사해야 총알이 현재 조준 방향으로 나간다.
    [UpdateAfter(typeof(PlayerControlSystem))]
    public partial struct PlayerShootSystem : ISystem
    {
        private Random _random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<InputComponent>();
            state.RequireForUpdate<ShooterComponent>();
            _random = Random.CreateFromIndex(1234);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InputComponent input = SystemAPI.GetSingleton<InputComponent>();
            if (!input.Shoot)
                return;

            double now = SystemAPI.Time.ElapsedTime;
            var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged);
            //사운드 싱글톤이 없어도 발사는 되어야 하므로 Require 대신 TryGet을 쓴다.
            bool hasSound = SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<PlaySoundRequest> soundRequests);

            foreach (var (shooter, localTrm) in SystemAPI.Query<RefRW<ShooterComponent>, RefRO<LocalTransform>>()
                         .WithAll<PlayerTag>())
            {
                //쿨다운 중에는 읽기만 하고, 실제로 발사할 때만 NextFireTime을 쓴다.
                if (now < shooter.ValueRO.NextFireTime)
                    continue;

                shooter.ValueRW.NextFireTime = now + shooter.ValueRO.FireInterval;

                //총알 개수와 상관없이 한 번 발사할 때 소리는 한 번만 요청한다.
                if (hasSound)
                    soundRequests.Add(new PlaySoundRequest { Type = SoundType.LaserShot });

                for (int i = 0; i < shooter.ValueRO.NumberOfBulletSpawn; i++)
                {
                    Entity newBullet = ecb.Instantiate(shooter.ValueRO.BulletPrefab);

                    float spreadValue = shooter.ValueRO.BulletSpread;
                    float spreadRad = math.radians( _random.NextFloat(-spreadValue, spreadValue));
                    LocalTransform trm = localTrm.ValueRO;
                    float3 spawnPosition = trm.Position + trm.Up() * 0.5f;

                    quaternion fireRot = math.mul(trm.Rotation, quaternion.RotateZ(spreadRad));
                    
                    ecb.SetComponent(newBullet, LocalTransform.FromPositionRotation(spawnPosition, fireRot));
                    ecb.SetComponent(newBullet, new MoveDirectionComponent
                    {
                        Value = math.mul(fireRot, math.up()).xy
                    });
                }

            }
        }
    }
}
