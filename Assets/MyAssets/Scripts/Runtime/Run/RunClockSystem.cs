using Unity.Burst;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Run
{
    /// <summary>
    /// 런 경과 시간을 올린다 (기획서 8.2 의 2번 "타이머").
    ///
    /// World 의 ElapsedTime 을 쓰지 않는 이유: 그건 일시정지 중에도 흐른다.
    /// 게임플레이 그룹 안에 두면 Paused / GameOver 동안 자동으로 멈춘다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup), OrderFirst = true)]
    public partial struct RunClockSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RunState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RunState>().ValueRW.ElapsedSeconds += SystemAPI.Time.DeltaTime;
        }
    }
}
