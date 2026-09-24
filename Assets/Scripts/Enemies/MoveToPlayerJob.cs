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
        private void Execute(ref MoveDirectionComponent moveDirection, ref LocalTransform trm)
        {
            float2 direction = PlayerPosition - trm.Position.xy;
            moveDirection.Value = math.normalizesafe(direction); //거리 0일때 안전
            
            //거리가 매우 작다면 회전이 튀므로 회전은 건너뛴다.
            if(math.lengthsq(direction) < 0.0001f) return;
            
            float angle = math.atan2(direction.y, direction.x) + math.PI * 0.5f; //이미 그림이 -90 돌아가있어
            trm.Rotation = quaternion.RotateZ(angle);
        }
    }
}