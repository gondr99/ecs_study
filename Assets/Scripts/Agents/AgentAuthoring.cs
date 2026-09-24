using CombatSystem;
using Effects;
using Unity.Entities;
using UnityEngine;

namespace Agents
{
    public class AgentAuthoring : MonoBehaviour
    {
        public float moveSpeed = 4f;
        public int maxHitPoint = 30;
        public GameObject deathEffectPrefab;
        
        private class AgentAuthoringBaker : Baker<AgentAuthoring>
        {
            public override void Bake(AgentAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<InitAgentFlag>(entity);
                AddComponent<MoveDirectionComponent>(entity);
                AddComponent(entity, new MoveSpeedComponent
                {
                    Value = authoring.moveSpeed
                });
                AddComponent(entity, new MaxHitPoint{Value = authoring.maxHitPoint});
                AddComponent(entity, new CurrentHitPoint{Value = authoring.maxHitPoint});

                AddBuffer<DamageThisFrame>(entity);
                
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);

                if (authoring.deathEffectPrefab != null)
                {
                    AddComponent(entity, new ExplosionComponent
                    {
                        Prefab = GetEntity(authoring.deathEffectPrefab, TransformUsageFlags.Dynamic)
                    });
                }
            }
        }
    }
}