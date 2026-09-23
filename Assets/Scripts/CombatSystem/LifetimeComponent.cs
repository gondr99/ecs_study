using Unity.Entities;

namespace CombatSystem
{
    public struct LifetimeComponent : IComponentData
    {
        public float RemainingLifetime;
    }
}