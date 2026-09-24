using Unity.Entities;

namespace UI
{
    //게임 전체에 하나만 있는 점수 싱글톤. 지금은 죽인 적 수만 센다.
    public struct GameScore : IComponentData
    {
        public int KillCount;
    }
}
