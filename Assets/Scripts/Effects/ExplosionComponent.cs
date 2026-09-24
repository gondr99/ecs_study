using Unity.Entities;

namespace Effects
{
    //죽을 때(DestroyEntityFlag가 켜질 때) 이 자리에 생성할 이펙트 프리팹 엔티티.
    public struct ExplosionComponent : IComponentData
    {
        public Entity Prefab;
    }
}
