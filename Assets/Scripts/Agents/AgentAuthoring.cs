using CombatSystem;
using Unity.Entities;
using UnityEngine;

namespace Agents
{
    public class AgentAuthoring : MonoBehaviour
    {
        public float moveSpeed = 4f;
        private class AgentAuthoringBaker : Baker<AgentAuthoring>
        {
            public override void Bake(AgentAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<MoveDirectionComponent>(entity);
                AddComponent(entity, new MoveSpeedComponent
                {
                    Value = authoring.moveSpeed
                });
                
                AddComponent<DestroyEntityFlag>(entity);
                SetComponentEnabled<DestroyEntityFlag>(entity, false);
            }
        }
    }
}