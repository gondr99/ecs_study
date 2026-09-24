using Unity.Entities;

namespace CombatSystem
{
    public struct DamageThisFrame : IBufferElementData
    {
        public int Value;
    }
}