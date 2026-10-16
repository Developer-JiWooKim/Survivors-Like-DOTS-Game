using Unity.Entities;
using Unity.Mathematics;

namespace SurvivorsLikeDOTS.Flight
{
    /// <summary>
    /// 이번 프레임에 우주선이 "하고 싶은 것". <see cref="ShipFlightSystem"/> 이 이걸 읽어 실제로 움직인다.
    ///
    /// 왜 입력(ShipInput)을 비행 시스템이 직접 읽지 않나:
    /// 플레이어는 마우스 조준으로, 적 우주선은 AI 로 이 값을 채운다. 비행 시스템은 누가 조종하는지 몰라도 된다.
    /// 플레이어 쪽은 <c>PlayerAimSystem</c> 이 채운다.
    /// </summary>
    public struct ShipControl : IComponentData
    {
        /// <summary>기수를 돌리고 싶은 월드 방향. 길이 무관 (정규화해서 쓴다).</summary>
        public float3 DesiredForward;

        /// <summary>기체 기준 추력. x = 좌우, y = 상하, z = 전후. 길이 0~1.</summary>
        public float3 Thrust;

        /// <summary>롤 입력. +1 = 오른쪽.</summary>
        public float Roll;
    }
}
