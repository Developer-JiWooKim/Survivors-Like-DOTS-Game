using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Input
{
    /// <summary>
    /// 이번 프레임의 플레이어 입력. 싱글턴으로 하나만 존재한다.
    ///
    /// 왜 싱글턴 컴포넌트로 옮기나:
    /// Input System 은 매니지드 API라 Burst 잡 안에서 직접 읽을 수 없다.
    /// 매니지드 영역에서 한 번 읽어 여기에 써두면, 이후 모든 시스템은 Burst 로 컴파일된 채
    /// 이 값을 읽을 수 있다. **ECS 와 매니지드 사이의 동기화 경계를 이 한 곳으로 몰아넣는 것**이
    /// 플레이어를 엔티티로 둔 이유이기도 하다.
    /// </summary>
    public struct PlayerInputState : IComponentData
    {
        /// <summary>정규화된 이동 입력. 길이는 0~1.</summary>
        public float2 Move;
    }
}
