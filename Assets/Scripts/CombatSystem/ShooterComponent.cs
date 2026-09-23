using Unity.Entities;

namespace CombatSystem
{
    public struct ShooterComponent : IComponentData
    {
        public Entity BulletPrefab;
        public float BulletSpread;
        public int NumberOfBulletSpawn;
        public float FireInterval;     // 설정: 발사 간격(초)
        public double NextFireTime;    // 런타임: 다음 발사 가능 시각
    }
}