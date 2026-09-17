using Unity.Burst;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Run
{
    /// <summary>
    /// <see cref="RunState"/> 싱글턴을 소유한다. 지금은 생성만 하고,
    /// 런 타이머·페이즈 전환(기획서 8.2 의 2번)이 들어오면 여기서 갱신한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RunStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 시스템 엔티티에 붙여 두면 씬 로딩과 무관하게 월드가 살아있는 동안 존재한다.
            state.EntityManager.AddComponentData(state.SystemHandle, new RunState
            {
                Phase = RunPhase.Playing,
            });

            // 갱신할 게 아직 없다.
            state.Enabled = false;
        }
    }
}
