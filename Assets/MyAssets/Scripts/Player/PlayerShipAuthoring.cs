using SurvivorsLikeDOTS.Flight;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SurvivorsLikeDOTS.Player
{
    public sealed class PlayerShipAuthoring : MonoBehaviour
    {
        // 기본값은 우주선 폭 약 13m 기준의 첫 추정치 (2026-10-16). 플레이 테스트로 조정한다.
        [Header("Ship Flight Settings")]
        [Tooltip("전진 최고 속도 (m/s)")]
        [SerializeField] private float _forwardSpeed = 60f;

        [Tooltip("후진 최고 속도 (m/s). 후진은 감속용")]
        [SerializeField] private float _reverseSpeed = 20f;

        [Tooltip("좌우·상하 이동 최고 속도 (m/s). 회피용")]
        [SerializeField] private float _strafeSpeed = 30f;

        [Tooltip("속도가 원하는 속도를 따라가는 빠르기 (m/s²). 클수록 아케이드, 작을수록 관성으로 미끄러진다")]
        [SerializeField] private float _acceleration = 80f;

        [Tooltip("기수가 목표 방향으로 도는 최대 각속도 (°/s). 베이킹 때 rad/s 로 변환")]
        [SerializeField] private float _turnRate = 90f;

        [Tooltip("롤 입력 1 일 때의 롤 각속도 (°/s). 베이킹 때 rad/s 로 변환")]
        [SerializeField] private float _rollRate = 120f;

        [Header("Aim Settings")]
        [SerializeField] private float _radiansPerPixel;
        [SerializeField] private float _rollFollowRate;


        private sealed class PlayerShipBaker : Baker<PlayerShipAuthoring>
        {
            public override void Bake(PlayerShipAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<PlayerTag>(entity);

                AddComponent(entity, new ShipFlight
                {
                    ForwardSpeed = authoring._forwardSpeed,
                    ReverseSpeed = authoring._reverseSpeed,
                    StrafeSpeed = authoring._strafeSpeed,
                    Acceleration = authoring._acceleration,
                    TurnRate = math.radians(authoring._turnRate),
                    RollRate = math.radians(authoring._rollRate),
                });

                // 정지 상태로 시작
                AddComponent(entity, new ShipVelocity
                {
                    Linear = float3.zero,
                });

                // 첫 프레임의 목표 방향을 배치된 방향으로 둔다. 0 이면 어느 쪽으로 돌지 정해지지 않는다.
                // authoring.transform 대신 Baker.GetComponent 를 거쳐야 씬에서 돌렸을 때 재베이킹 의존성이 잡힌다.
                float3 desiredForward = GetComponent<Transform>().forward;
                AddComponent(entity, new ShipControl
                {
                    DesiredForward = desiredForward,
                    Thrust = float3.zero,
                    Roll = 0,
                });

                quaternion rotation = GetComponent<Transform>().rotation;
                AddComponent(entity, new PlayerAim
                {
                    Rotation = rotation,
                });

                AddComponent(entity, new PlayerAimSettings
                {
                    RadiansPerPixel = math.radians(authoring._radiansPerPixel),
                    RollFollowRate = authoring._rollFollowRate,
                });
            }
        }
    }
}