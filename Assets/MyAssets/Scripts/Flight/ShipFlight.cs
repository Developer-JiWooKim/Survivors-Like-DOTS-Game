using Unity.Entities;

namespace SurvivorsLikeDOTS.Flight
{
    /// <summary>
    /// 우주선 비행 성능. 플레이어와 (나중의) 적 우주선이 같이 쓴다.
    ///
    /// 비행 모델 = 보조 관성형 (2026-10-16 결정):
    /// 속도는 "입력이 원하는 속도" 를 향해 <see cref="Acceleration"/> 만큼씩만 따라간다.
    /// 기수를 돌리면 원하는 속도의 방향도 돌지만 실제 속도는 바로 따라오지 못해 잠깐 미끄러진다.
    /// - Acceleration 이 크면 → 아케이드형 (도는 즉시 그 방향으로 감)
    /// - Acceleration 이 작으면 → 관성형에 가까움 (오래 미끄러짐)
    /// 값 하나로 손맛을 조절하려고 이렇게 묶었다.
    /// </summary>
    public struct ShipFlight : IComponentData
    {
        /// <summary>전진 최고 속도 (m/s).</summary>
        public float ForwardSpeed;

        /// <summary>후진 최고 속도 (m/s).</summary>
        public float ReverseSpeed;

        /// <summary>좌우·상하 이동 최고 속도 (m/s).</summary>
        public float StrafeSpeed;

        /// <summary>속도가 원하는 속도를 따라가는 빠르기 (m/s²). 미끄러짐의 양을 정한다.</summary>
        public float Acceleration;

        /// <summary>기수가 목표 방향으로 도는 최대 각속도 (rad/s).</summary>
        public float TurnRate;

        /// <summary>롤 입력 1 일 때의 롤 각속도 (rad/s).</summary>
        public float RollRate;
    }
}
