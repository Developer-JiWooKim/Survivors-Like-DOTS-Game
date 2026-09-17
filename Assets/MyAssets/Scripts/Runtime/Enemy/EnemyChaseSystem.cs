using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적이 플레이어를 향해 직진한다.
    ///
    /// 분리(separation)는 넣지 않았다:
    /// 이웃 적을 밀어내려면 주변 적을 찾아야 하고, 그러려면 공간 해시가 필요하다(기획서 8.3).
    /// 공간 해시는 M2 작업이므로 M1 에서는 순수 추격만 한다.
    /// 적들이 플레이어 위에 겹쳐 쌓이는 건 이 단계에서 정상이다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial struct EnemyChaseSystem : ISystem
    {
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 쿼리는 매 프레임 만들지 않고 한 번만 만들어 재사용한다.
            _playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerMovement, LocalTransform>()
                .Build();

            state.RequireForUpdate(_playerQuery);
            state.RequireForUpdate<EnemyMovement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LocalTransform playerTransform = _playerQuery.GetSingleton<LocalTransform>();

            var job = new ChaseJob
            {
                Target = playerTransform.Position,
                DeltaTime = SystemAPI.Time.DeltaTime,
            };

            // ScheduleParallel: 적 수천~수만을 워커 스레드에 나눠 처리한다.
            // 각 적이 자기 LocalTransform 만 건드리고 서로를 읽지 않아 병렬화가 안전하다.
            // (분리를 넣는 순간 이웃을 읽어야 해서 이 단순함이 깨진다 — M2 과제)
            job.ScheduleParallel();
        }
    }

    [BurstCompile]
    // 죽어서 풀에 들어간 적은 움직이지 않는다.
    [WithAll(typeof(Active))]
    internal partial struct ChaseJob : IJobEntity
    {
        public float3 Target;
        public float DeltaTime;

        private void Execute(ref LocalTransform transform, in EnemyMovement movement)
        {
            float3 delta = Target - transform.Position;

            // 2D 게임이라 Z 차이는 추격 방향에 반영하지 않는다.
            delta.z = 0f;

            float distanceSquared = math.lengthsq(delta);

            // 플레이어와 사실상 같은 위치면 방향 계산이 불안정해진다(0 으로 나누기).
            if (distanceSquared < 1e-6f)
            {
                return;
            }

            float distance = math.sqrt(distanceSquared);
            float3 direction = delta / distance;

            // 이번 프레임 이동량이 남은 거리보다 크면 플레이어를 지나쳐 버리고,
            // 다음 프레임에 다시 되돌아오면서 떨림이 생긴다. 남은 거리로 잘라준다.
            float step = math.min(movement.Speed * DeltaTime, distance);

            transform.Position += direction * step;
        }
    }
}
