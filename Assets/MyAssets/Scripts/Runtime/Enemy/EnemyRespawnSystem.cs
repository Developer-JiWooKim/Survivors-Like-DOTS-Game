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
    /// 풀에서 쉬고 있는(죽은) 적을 플레이어 주변 재스폰 링에서 체력을 채워 되살린다.
    ///
    /// 풀 크기 = 목표 동시 적 수 이므로, 꺼진 적을 전부 되살리면 살아있는 적 수가 항상 목표치로 유지된다.
    /// 죽은 프레임에는 사라져 있다가 다음 프레임 시작에 링에서 나타난다.
    /// 시간에 따라 되살리는 양을 조절하는 예산 스폰 디렉터(기획서 6.2)가 M2 이후 이 자리를 대신한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(PlayerMoveSystem))]
    public partial struct EnemyRespawnSystem : ISystem
    {
        private EntityQuery _deadQuery;
        private EntityQuery _playerQuery;
        private uint _frame;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _deadQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement>()
                .WithAllRW<LocalTransform, Health>()
                .WithDisabledRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            _playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerMovement, LocalTransform>()
                .Build();

            state.RequireForUpdate<EnemySpawner>();
            state.RequireForUpdate(_playerQuery);
            state.RequireForUpdate(_deadQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            EnemySpawner spawner = SystemAPI.GetSingleton<EnemySpawner>();
            float3 playerPosition = _playerQuery.GetSingleton<LocalTransform>().Position;

            new EnemyRespawnJob
            {
                Center = playerPosition.xy,
                MinRadius = spawner.RespawnMinRadius,
                MaxRadius = spawner.RespawnMaxRadius,
                // 프레임마다 다른 시드여야 같은 엔티티가 매번 같은 자리에서 되살아나지 않는다.
                Seed = math.hash(new uint2(spawner.RandomSeed, ++_frame)),
            }.ScheduleParallel(_deadQuery);
        }
    }

    [BurstCompile]
    internal partial struct EnemyRespawnJob : IJobEntity
    {
        public float2 Center;
        public float MinRadius;
        public float MaxRadius;
        public uint Seed;

        private void Execute(
            [EntityIndexInQuery] int index,
            ref LocalTransform transform,
            ref Health health,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            // 워커 스레드마다 Random 을 공유하면 레이스가 된다. 엔티티마다 결정적으로 새로 만든다.
            var random = Random.CreateFromIndex(Seed ^ (uint)index);
            float2 offset = RingSampler.Sample(ref random, MinRadius, MaxRadius);

            // Z 와 스케일은 프리팹 값을 보존한다.
            transform.Position.xy = Center + offset;
            health.Current = health.Max;

            active.ValueRW = true;
            visible.ValueRW = true;
        }
    }
}
