using Unity.Entities;
using Unity.Mathematics;

namespace CoreSystem
{
    public struct InputComponent : IComponentData
    {
        public float2 Movement;
        public float2 MousePosition;
        public float2 AimWorldPosition;
        public bool Shoot;
    }
}