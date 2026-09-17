using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 시간 기반 예산 스폰 디렉터 (기획서 8.2 의 3·5번).
    /// 목표 수(<see cref="SpawnDirector"/>)보다 살아있는 적이 적으면, 모자란 만큼 풀에서 꺼내
    /// 플레이어 주변 링에 체력을 채워 켠다. 죽은 적의 재등장도 같은 경로다.
    ///
    /// 보충 속도 상한은 두지 않았다 (기획서에 없음). 모자란 만큼 한 프레임에 켠다.
    /// 링이 화면 밖이라 한꺼번에 나타나도 보이지 않는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(PlayerMoveSystem))]
    public partial struct EnemySpawnDirectorSystem : ISystem
    {
        private EntityQuery _aliveQuery;
        private EntityQuery _pooledQuery;
        private uint _frame;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _aliveQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active>()
                .Build();

            _pooledQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement>()
                .WithAllRW<LocalTransform, Health>()
                .WithDisabledRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate<EnemySpawner>();
            state.RequireForUpdate<SpawnDirector>();
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<PlayerExperience>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EnemySpawner spawner = SystemAPI.GetSingleton<EnemySpawner>();
            RefRW<SpawnDirector> director = SystemAPI.GetSingletonRW<SpawnDirector>();
            float elapsed = SystemAPI.GetSingleton<RunState>().ElapsedSeconds;

            int target = math.min(director.ValueRO.BudgetAt(elapsed), spawner.PoolSize);

            // enabled 비트를 세므로 Active 를 쓰는 잡들(지난 프레임 사망·투사체·젬)을 기다린다.
            // 프레임 시작이라 대부분 끝나 있어 대기는 짧다.
            int alive = _aliveQuery.CalculateEntityCount();

            // 기본 체력은 프리팹 값을 그대로 쓴다 — 적에게 "기본 체력" 필드를 따로 둘 필요가 없다.
            // 프리팹은 어떤 잡도 쓰지 않고, 경험치 합산 잡도 프레임 시작이라 끝나 있어 대기가 거의 없다.
            float baseHealth = SystemAPI.GetComponent<Health>(spawner.Prefab).Max;
            int playerLevel = SystemAPI.GetSingleton<PlayerExperience>().Level;
            float spawnHealth = director.ValueRO.HealthAt(baseHealth, playerLevel);

            director.ValueRW.Target = target;
            director.ValueRW.Alive = alive;
            director.ValueRW.SpawnHealth = spawnHealth;

            int deficit = target - alive;
            if (deficit <= 0)
            {
                return;
            }

            // PlayerPosition 은 메인 스레드에서만 쓰므로 기다릴 잡이 없다 (지난 프레임 위치 — 링 중심으로 충분).
            float2 center = SystemAPI.GetSingleton<PlayerPosition>().Value;

            new EnemySpawnJob
            {
                Center = center,
                MinRadius = spawner.RingMinRadius,
                MaxRadius = spawner.RingMaxRadius,
                Limit = deficit,
                MaxHealth = spawnHealth,
                // 프레임마다 다른 시드여야 같은 엔티티가 매번 같은 자리에서 나타나지 않는다.
                Seed = math.hash(new uint2(spawner.RandomSeed, ++_frame)),
            }.ScheduleParallel(_pooledQuery);
        }
    }

    [BurstCompile]
    internal partial struct EnemySpawnJob : IJobEntity
    {
        public float2 Center;
        public float MinRadius;
        public float MaxRadius;
        public uint Seed;

        /// <summary>
        /// 이번 프레임에 켤 수. 쿼리 내 순번이 이보다 작은 엔티티만 켠다.
        /// 병렬 잡에서 "N 개만" 을 지키는 가장 단순한 방법 — 순번은 워커 간에 겹치지 않는다.
        /// 나머지 수만 개는 순번 비교만 하고 넘어가므로 비용이 작다.
        /// </summary>
        public int Limit;

        /// <summary>
        /// 이번에 스폰되는 적의 최대 체력 (플레이어 레벨 반영).
        /// 이미 살아있는 적은 건드리지 않는다 — 새로 들어오는 적부터 자연스럽게 바뀐다 (사용자 결정).
        /// </summary>
        public float MaxHealth;

        private void Execute(
            [EntityIndexInQuery] int index,
            ref LocalTransform transform,
            ref Health health,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            if (index >= Limit)
            {
                return;
            }

            // 워커 스레드마다 Random 을 공유하면 레이스가 된다. 엔티티마다 결정적으로 새로 만든다.
            var random = Random.CreateFromIndex(Seed ^ (uint)index);
            float2 offset = RingSampler.Sample(ref random, MinRadius, MaxRadius);

            // Z 와 스케일은 프리팹 값을 보존한다.
            transform.Position.xy = Center + offset;
            health.Max = MaxHealth;
            health.Current = MaxHealth;

            active.ValueRW = true;
            visible.ValueRW = true;
        }
    }
}
