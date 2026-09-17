using Unity.Entities;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Run
{
    /// <summary>
    /// 게임플레이 시스템(이동·무기·피해·사망·재스폰)을 모아 두고, <see cref="RunState"/> 가
    /// <see cref="RunPhase.Playing"/> 일 때만 통째로 돌린다.
    ///
    /// 왜 그룹 게이트인가 (Time.timeScale = 0 대신):
    /// - timeScale 은 dt 만 0 으로 만들 뿐 시스템과 잡은 계속 돈다. dt 를 안 쓰는 로직(재스폰 등)은 멈추지도 않는다.
    /// - UI 연출까지 멈춰서 레벨업 화면이 unscaled time 을 써야 한다.
    /// - 그룹을 건너뛰면 잡 스케줄 비용까지 0 이 되고, 변환·렌더링은 계속 돌아 화면이 유지된다.
    ///
    /// 왜 ComponentSystemGroup(managed) 인가:
    /// 시스템 그룹은 class 로만 만들 수 있다. 그룹 자체는 프레임당 1 회 분기만 하므로 비용은 무시할 수준이다.
    ///
    /// TransformSystemGroup 보다 먼저 도는 이유:
    /// 이번 프레임에 옮긴 위치가 같은 프레임의 LocalToWorld(렌더 행렬)에 반영되어야 1 프레임 밀리지 않는다.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public sealed partial class GameplaySystemGroup : ComponentSystemGroup
    {
        protected override void OnUpdate()
        {
            // 싱글턴이 없을 때(월드 초기화 직후)는 막지 않는다. 기존 동작을 바꾸지 않기 위함.
            if (SystemAPI.TryGetSingleton(out RunState runState) && runState.Phase != RunPhase.Playing)
            {
                return;
            }

            base.OnUpdate();
        }
    }
}
