using Unity.Entities;
using Unity.Rendering;

namespace Effects
{
    //[MaterialProperty]를 붙이면 Entities Graphics가 이 값을 셰이더의 _Frame(DOTS instanced property)으로 매 프레임 올려준다.
    //MaterialPropertyBlock 없이, 엔티티마다 다른 값을 줄 수 있는 ECS 방식의 per-instance 머티리얼 값.
    [MaterialProperty("_Frame")]
    public struct FlipBookFrame : IComponentData
    {
        public float Value;
    }

    public struct FlipBookAnimation : IComponentData
    {
        public int FrameCount;
        public float FrameRate;
        public float Elapsed;
    }
}
