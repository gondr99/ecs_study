using CombatSystem;
using Unity.Entities;
using UnityEngine;

namespace Effects
{
    //MeshFilter + MeshRenderer가 있는 프리팹에 붙인다. 렌더러는 Entities Graphics가 알아서 bake한다.
    public class FlipBookEffectAuthoring : MonoBehaviour
    {
        [Min(1)] public int frameCount = 12;
        [Min(0.01f)] public float frameRate = 24f;

        private class FlipBookEffectAuthoringBaker : Baker<FlipBookEffectAuthoring>
        {
            public override void Bake(FlipBookEffectAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new FlipBookAnimation
                {
                    FrameCount = authoring.frameCount,
                    FrameRate = authoring.frameRate
                });
                AddComponent<FlipBookFrame>(entity);

                //한 번 재생하고 사라지는 이펙트라 기존 Lifetime -> DestroyEntityFlag 흐름을 그대로 재사용한다.
                AddComponent(entity, new LifetimeComponent
                {
                    RemainingLifetime = authoring.frameCount / authoring.frameRate
                });
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}
