using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Effects
{
    public partial struct FlipBookAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FlipBookAnimationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct FlipBookAnimationJob : IJobEntity
    {
        public float DeltaTime;

        private void Execute(ref FlipBookAnimation animation, ref FlipBookFrame frame)
        {
            animation.Elapsed += DeltaTime;
            //컴포넌트 값만 바꾸면 렌더링 쪽 반영(GPU 업로드)은 Entities Graphics가 처리한다.
            int index = (int)(animation.Elapsed * animation.FrameRate);
            frame.Value = math.min(index, animation.FrameCount - 1);
        }
    }
}
