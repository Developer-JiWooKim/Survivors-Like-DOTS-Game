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
using Random = Unity.Mathematics.Random;

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
    ///
    /// **리사이클** (기획서 6.2)도 여기서 한다: 플레이어에게서 너무 멀어진 적은 추격 대신
    /// 플레이어 기준 **반대편** 스폰 링으로 순간이동한다. 죽이지 않으므로 체력·스폰 비용이 그대로다.
    /// 적 전체를 도는 루프를 하나 더 만들지 않으려고 추격 잡에 합쳤다 (거리 계산도 공유).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial struct EnemyChaseSystem : ISystem
    {
        private uint _frame;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<EnemyMovement>();
            state.RequireForUpdate<EnemySpatialHash>();
            state.RequireForUpdate<EnemySeparation>();
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // PlayerPosition 은 바로 앞의 PlayerMoveSystem 이 메인 스레드에서 갱신한 이번 프레임 값이다.
            // LocalTransform 을 읽으면 LocalTransform 에 쓰는 잡들을 기다려야 한다.
            float2 player = SystemAPI.GetSingleton<PlayerPosition>().Value;
            EnemySeparation separation = SystemAPI.GetSingleton<EnemySeparation>();
            EnemySpawner spawner = SystemAPI.GetSingleton<EnemySpawner>();
            RefRW<EnemySpatialHash> hash = SystemAPI.GetSingletonRW<EnemySpatialHash>();

            var job = new ChaseJob
            {
                Target = player,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Neighbors = hash.ValueRO.Map,
                SeparationStrength = separation.Strength,
                MaxNeighbors = separation.MaxNeighbors,
                RecycleDistance = spawner.RecycleDistance,
                RingMinRadius = spawner.RingMinRadius,
                RingMaxRadius = spawner.RingMaxRadius,
                Seed = math.hash(new uint2(spawner.RandomSeed ^ 0xA5A5A5A5u, ++_frame)),
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

        public float RecycleDistance;
        public float RingMinRadius;
        public float RingMaxRadius;
        public uint Seed;

        [ReadOnly] public NativeParallelMultiHashMap<int, AgentRef> Neighbors;

        private void Execute(Entity self, ref LocalTransform transform, in EnemyMovement movement, in HitRadius radius)
        {
            float2 position = transform.Position.xy;
            float maxStep = movement.Speed * DeltaTime;

            // 2D 게임이라 Z 는 추격 방향에 반영하지 않는다 (xy 만 사용).
            float2 toTarget = Target - position;
            float distance = math.length(toTarget);

            // 0) 리사이클 — 너무 멀어졌으면 반대편 링으로 옮기고 이번 프레임 이동은 건너뛴다.
            if (distance > RecycleDistance)
            {
                transform.Position.xy = RecyclePosition(self, toTarget / distance);
                return;
            }

            // 1) 추격 — 길이 1 이하의 방향
            float2 chase = float2.zero;

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

        /// <summary>
        /// 플레이어 기준 반대편 링 위의 위치.
        /// 적 → 플레이어 방향(<paramref name="towardPlayer"/>)으로 플레이어를 지나 링 반경만큼 간 지점 =
        /// 플레이어가 달려가는 쪽 앞에 다시 나타난다. 뒤처진 적이 앞쪽 밀도로 돌아오는 효과.
        /// </summary>
        private float2 RecyclePosition(Entity self, float2 towardPlayer)
        {
            // 링 반경만 무작위. 워커 간 Random 공유는 레이스라 엔티티마다 결정적으로 만든다.
            var random = Random.CreateFromIndex(math.hash(new uint2(Seed, (uint)self.Index)));
            float ring = random.NextFloat(RingMinRadius, RingMaxRadius);
            return Target + towardPlayer * ring;
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
                    int2 cell = center + new int2(x, y);
                    if (!Neighbors.TryGetFirstValue(EnemySpatialHash.KeyOf(cell), out AgentRef other, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        continue;
                    }

                    do
                    {
                        // 키 충돌로 섞여 든 다른 셀의 적을 거른다 — 안 그러면 같은 이웃에게 두 번 밀린다.
                        if (other.Entity == self || !EnemySpatialHash.IsInCell(other, cell))
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
