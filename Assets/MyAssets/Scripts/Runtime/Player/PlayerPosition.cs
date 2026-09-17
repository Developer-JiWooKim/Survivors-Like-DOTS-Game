using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// 플레이어 위치의 사본. <see cref="PlayerMoveSystem"/> 이 이동 직후 갱신한다.
    ///
    /// LocalTransform 이 있는데 왜 따로 두나:
    /// 1. 다른 엔티티의 LocalTransform 에 **쓰는** 병렬 잡(젬 흡인 등)은 플레이어 LocalTransform 을 룩업으로 읽을 수 없다.
    ///    같은 컴포넌트 타입을 한 잡에서 읽기 전용 + 쓰기로 동시에 잡으면 안전 검사에 걸린다.
    /// 2. 메인 스레드에서 LocalTransform 을 읽으면 적 이동 잡 같은 쓰기 잡이 끝날 때까지 기다린다.
    ///    이 컴포넌트는 메인 스레드에서만 쓰므로 읽어도 기다릴 잡이 없다.
    /// </summary>
    public struct PlayerPosition : IComponentData
    {
        public float2 Value;
    }
}
