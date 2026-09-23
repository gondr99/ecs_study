using Agents;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Enemies
{
    [BurstCompile]
    [WithAll(typeof(EnemyTag))]
    public partial struct MoveToPlayerJob : IJobEntity
    {
        public float2 PlayerPosition;
        
        //in은 readonly 참조전달.
        private void Execute(ref MoveDirectionComponent moveDirection, in LocalTransform trm)
        {
            float2 direction = PlayerPosition - trm.Position.xy;
            moveDirection.Value = math.normalizesafe(direction); //거리 0일때 안전
        }
    }
}