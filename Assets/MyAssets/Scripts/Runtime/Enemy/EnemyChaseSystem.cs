using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Assets.MyAssets.Scripts.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적이 플레이어를 향해 이동하면서, 주변 적과 겹치지 않게 서로 밀어낸다 (기획서 8.2 의 8번).
    ///
    /// 주변 적은 <see cref="EnemySpatialHash"/> 에서 **자기 셀과 이웃 셀만** 조회한다.
    /// 전체 적과 비교하면 O(N²) — 1,000 마리면 프레임당 100만 번, 1만 마리면 1억 번이라 감당이 안 된다.
    /// 셀 조회는 적 1 마리당 주변 몇 마리만 보므로 O(N) 에 가깝다.
    ///
    /// 병렬화가 여전히 안전한 이유:
    /// 이웃 위치는 해시에 복사된 **이번 프레임 시작 시점 스냅샷**에서 읽고, 쓰기는 자기 LocalTransform 에만 한다.
    /// 이웃이 이번 프레임에 어디로 움직이는지는 보지 않는다 — 한 프레임 늦은 정보로 피하지만 체감되지 않는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
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
            state.RequireForUpdate<EnemySpatialHash>();
            state.RequireForUpdate<EnemySeparation>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            LocalTransform playerTransform = _playerQuery.GetSingleton<LocalTransform>();
            EnemySeparation separation = SystemAPI.GetSingleton<EnemySeparation>();
            RefRW<EnemySpatialHash> hash = SystemAPI.GetSingletonRW<EnemySpatialHash>();

            var job = new ChaseJob
            {
                Target = playerTransform.Position.xy,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Neighbors = hash.ValueRO.Map,
                SeparationStrength = separation.Strength,
                MaxNeighbors = separation.MaxNeighbors,
            };

            // 해시 재구축 잡이 끝난 뒤에 돌아야 한다. 컨테이너가 컴포넌트 안에 있어 ECS 가 이 의존성을 모른다.
            JobHandle handle = job.ScheduleParallel(
                JobHandle.CombineDependencies(state.Dependency, hash.ValueRO.BuildHandle));

            hash.ValueRW.RegisterReader(handle);
            state.Dependency = handle;
        }
    }

    // 죽어서 풀에 들어간 적은 움직이지 않는다.
    [BurstCompile]
    [WithAll(typeof(Active))]
    internal partial struct ChaseJob : IJobEntity
    {
        public float2 Target;
        public float DeltaTime;
        public float SeparationStrength;
        public int MaxNeighbors;

        [ReadOnly] public NativeParallelMultiHashMap<int, AgentRef> Neighbors;

        private void Execute(Entity self, ref LocalTransform transform, in EnemyMovement movement, in HitRadius radius)
        {
            float2 position = transform.Position.xy;
            float maxStep = movement.Speed * DeltaTime;

            // 1) 추격 — 길이 1 이하의 방향
            // 2D 게임이라 Z 는 추격 방향에 반영하지 않는다 (xy 만 사용).
            float2 chase = float2.zero;
            float2 toTarget = Target - position;
            float distance = math.length(toTarget);

            // 플레이어와 사실상 같은 위치면 방향 계산이 불안정해진다(0 으로 나누기).
            if (distance > 1e-3f)
            {
                chase = toTarget / distance;

                // 이번 프레임 이동량이 남은 거리보다 크면 플레이어를 지나쳤다 되돌아오며 떨린다. 남은 거리만큼으로 줄인다.
                if (maxStep > distance)
                {
                    chase *= distance / maxStep;
                }
            }

            // 2) 분리 — 겹친 이웃에게서 멀어지는 방향의 합
            float2 push = ComputeSeparation(self, position, radius.Value);

            // 3) 합성. 길이를 1 로 자르는 이유: 사방에서 밀리는 적이 자기 속도보다 빨리 튀어나가지 않게 한다.
            float2 direction = chase + push * SeparationStrength;
            float lengthSquared = math.lengthsq(direction);
            if (lengthSquared > 1f)
            {
                direction /= math.sqrt(lengthSquared);
            }

            transform.Position.xy += direction * maxStep;
        }

        private float2 ComputeSeparation(Entity self, float2 position, float selfRadius)
        {
            float2 push = float2.zero;
            int examined = 0;

            // 분리 반경(두 적의 반경 합)이 셀 크기보다 작으면 이웃 8칸이면 충분하다 (현재 ±1).
            // 반경이 커지면 검사 범위를 넓혀야 누락이 없다 — 그래서 고정 1 이 아니라 계산한다.
            int range = EnemySpatialHash.CellRangeFor(selfRadius);
            int2 center = EnemySpatialHash.CellOf(position);

            for (int y = -range; y <= range; y++)
            {
                for (int x = -range; x <= range; x++)
                {
                    int key = EnemySpatialHash.KeyOf(center + new int2(x, y));
                    if (!Neighbors.TryGetFirstValue(key, out AgentRef other, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        continue;
                    }

                    do
                    {
                        if (other.Entity == self)
                        {
                            continue;
                        }

                        float reach = selfRadius + other.Radius;
                        float2 away = position - other.Position;
                        float distanceSquared = math.lengthsq(away);
                        if (distanceSquared >= reach * reach)
                        {
                            continue;
                        }

                        float distance = math.sqrt(distanceSquared);
                        float2 direction = distance > 1e-4f
                            ? away / distance
                            : TieBreakDirection(self, other.Entity);

                        // 많이 겹칠수록 세게 민다 (닿기 직전 0 → 완전히 겹치면 1).
                        push += direction * (1f - distance / reach);

                        if (++examined >= MaxNeighbors)
                        {
                            return push;
                        }
                    }
                    while (Neighbors.TryGetNextValue(out other, ref iterator));
                }
            }

            return push;
        }

        /// <summary>
        /// 두 적이 정확히 같은 위치일 때 밀 방향. 재스폰 직후나 플레이어 중심에서 실제로 생긴다.
        /// 두 적이 **반대 방향**을 받아야 떨어진다 — 엔티티 쌍으로 방향을 정하고, 인덱스 크기로 부호를 나눈다.
        /// 각자 난수를 쓰면 같은 쪽으로 밀려 계속 붙어 있을 수 있다.
        /// </summary>
        private static float2 TieBreakDirection(Entity self, Entity other)
        {
            uint low = (uint)math.min(self.Index, other.Index);
            uint high = (uint)math.max(self.Index, other.Index);
            float angle = math.hash(new uint2(low, high)) / (float)uint.MaxValue * 2f * math.PI;
            float2 direction = new float2(math.cos(angle), math.sin(angle));
            return self.Index < other.Index ? direction : -direction;
        }
    }
}
