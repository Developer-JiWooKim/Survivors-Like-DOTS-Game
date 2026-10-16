using Unity.Entities;
using Unity.Mathematics;

namespace SurvivorsLikeDOTS.Input
{
    /// <summary>
    /// 이번 프레임의 플레이어 입력. 싱글턴으로 하나만 존재한다.
    ///
    /// 왜 싱글턴 컴포넌트로 옮기나:
    /// Input System 은 매니지드 API 라 Burst 잡 안에서 직접 읽을 수 없다.
    /// <see cref="ShipInputSystem"/> 이 매니지드 영역에서 한 번 읽어 여기에 써 두면,
    /// 이후 시스템은 전부 Burst 로 컴파일된 채 이 값을 읽는다. 매니지드 경계를 이 한 곳으로 몬다.
    ///
    /// 이름이 PlayerInput 이 아닌 이유: Input System 의 <c>UnityEngine.InputSystem.PlayerInput</c> 과 겹친다.
    /// </summary>
    public struct ShipInput : IComponentData
    {
        /// <summary>기체 기준 추력 방향. x = 좌우, y = 상하, z = 전후. 길이 0~1.</summary>
        public float3 Thrust;

        /// <summary>롤 입력. +1 = 오른쪽으로 기울임 (E), -1 = 왼쪽 (Q).</summary>
        public float Roll;

        /// <summary>이번 프레임 마우스 이동량 (픽셀). 커서가 풀려 있으면 0.</summary>
        public float2 AimDelta;

        /// <summary>주무기 발사 버튼을 누르고 있는가. WU-01 에서는 읽는 곳이 없다 (입력만 미리 잡아 둠).</summary>
        public bool Fire;

        /// <summary>부스트 버튼을 누르고 있는가. WU-01 에서는 읽는 곳이 없다.</summary>
        public bool Boost;
    }
}
