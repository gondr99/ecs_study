using Unity.Entities;
using Unity.Mathematics;

namespace Agents
{
    public struct MoveDirectionComponent : IComponentData
    {
        public float2 Value;
    }
}