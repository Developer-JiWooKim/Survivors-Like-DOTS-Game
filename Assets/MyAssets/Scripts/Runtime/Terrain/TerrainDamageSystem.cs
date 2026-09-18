using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 연소·감전 타일 위에 서 있는 유닛에게 피해를 준다 (기획서 4.3·4.4).
    /// **지형 콤보가 실제로 적을 죽이는 지점**이라, 이게 들어와야 지형 시스템이 게임플레이가 된다.
    ///
    /// 왜 지형 틱(10Hz)이 아니라 매 프레임인가:
    /// 기획서가 정한 건 "**초당** 피해" 다. 프레임마다 `초당피해 × dt` 를 주면 프레임률과 무관하게
    /// 같은 총량이 들어가고, 지형 틱에 맞추느라 "이번 프레임에 틱이 돌았나" 를 공유할 필요도 없다.
    /// 타일 조회는 배열 인덱싱 하나라 적 1 만에도 병렬 잡으로 충분히 싸다.
    ///
    /// 왜 Health 를 직접 깎지 않고 데미지 버스를 쓰나 (기획서 8.3):
    /// 같은 적이 같은 프레임에 투사체에도 맞으면 두 워커가 같은 Health 에 쓰는 레이스가 된다.
    /// 발생원이 늘어도 적용은 한 곳(<see cref="DamageApplySystem"/>)이라는 규칙을 지킨다.
    ///
    /// 플레이어에게는 **무적 프레임을 적용하지 않는다.** 접촉 피해와 달리 지형 피해는 지속 피해라,
    /// 0.5 초 무적이 걸리면 불 위에 서 있어도 절반은 공짜가 된다 — 기획서 4.4 의 "자기 무덤" 이 성립하지 않는다.
    ///
    /// 〔M4 에서 붙일 예외〕 비행 적은 지형 효과를 전부 무시한다 (기획서 4.4).
    /// 지금은 적 타입이 추격형 하나뿐이라 분기가 없다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(TerrainEffectSystem))]
    [UpdateBefore(typeof(DamageApplySystem))]
    public partial struct TerrainDamageSystem : ISystem
    {
        private EntityQuery _enemyQuery;
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active, LocalTransform, Health>()
                .Build();

            _playerQuery = SystemAPI.QueryBuilder()
                .WithAll<PlayerMovement, LocalTransform, Health>()
                .Build();

            state.RequireForUpdate<TerrainGrid>();
            state.RequireForUpdate<TerrainTickSettings>();
            state.RequireForUpdate<DamageEventBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TerrainTickSettings settings = SystemAPI.GetSingleton<TerrainTickSettings>();

            Entity gridEntity = SystemAPI.GetSingletonEntity<TerrainGrid>();
            RefRW<TerrainGrid> grid = SystemAPI.GetComponentRW<TerrainGrid>(gridEntity);

            // 그리드에 쓰는 잡(확산 틱)이 끝난 뒤에 읽는다. 컨테이너가 컴포넌트 안에 있어 직접 건다.
            JobHandle dependency = JobHandle.CombineDependencies(state.Dependency, grid.ValueRO.WriteHandle);

            // 적과 플레이어를 **같은 잡 타입**으로 처리한다. 규칙이 동일하기 때문 (기획서 4.4 — 불은 플레이어도 태운다).
            JobHandle enemies = Schedule(ref state, _enemyQuery, grid.ValueRO, settings, dependency);
            JobHandle player = Schedule(ref state, _playerQuery, grid.ValueRO, settings, dependency);

            JobHandle combined = JobHandle.CombineDependencies(enemies, player);
            grid.ValueRW.RegisterReader(combined);
            state.Dependency = combined;
        }

        /// <summary>
        /// 정적 메서드가 아닌 이유: SystemAPI 는 시스템 인스턴스에 묶여 코드 생성되므로
        /// static 안에서 쓰면 컴파일이 막힌다 (error EA0006).
        /// </summary>
        private JobHandle Schedule(
            ref SystemState state,
            EntityQuery query,
            in TerrainGrid grid,
            in TerrainTickSettings settings,
            JobHandle dependency)
        {
            int chunkCount = query.CalculateChunkCountWithoutFiltering();
            if (chunkCount == 0)
            {
                return dependency;
            }

            var stream = new NativeStream(chunkCount, Allocator.TempJob);

            JobHandle handle = new TerrainDamageJob
            {
                Tiles = grid.Tiles,
                Width = grid.Width,
                Height = grid.Height,
                Origin = grid.Origin,
                Settings = settings,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Damage = stream.AsWriter(),
            }.ScheduleParallel(query, dependency);

            // 스트림 해제는 DamageApplySystem 의 몫이다 (등록한 스트림은 거기서 드레인하고 정리한다).
            SystemAPI.GetSingletonRW<DamageEventBus>().ValueRW.Register(stream, handle);
            return handle;
        }
    }

    /// <summary>
    /// 유닛이 선 칸의 타일을 읽어 피해를 스트림에 쌓는다.
    /// 판단은 <see cref="TerrainDamage"/> 가 하고 이 잡은 좌표 → 인덱스 변환만 한다.
    /// </summary>
    [BurstCompile]
    internal partial struct TerrainDamageJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        [ReadOnly] public NativeArray<TileData> Tiles;

        public int Width;
        public int Height;
        public float2 Origin;
        public TerrainTickSettings Settings;
        public float DeltaTime;

        public NativeStream.Writer Damage;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Damage.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Damage.EndForEachIndex();
        }

        private void Execute(Entity entity, in LocalTransform transform)
        {
            // 맵 밖에 있는 유닛(스폰 링이 맵보다 클 때)은 지형의 영향을 받지 않는다.
            int2 cell = (int2)math.floor((transform.Position.xy - Origin) / TerrainGrid.TileSize);
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                return;
            }

            float perSecond = TerrainDamage.PerSecondFor(Tiles[cell.y * Width + cell.x], Settings);
            if (perSecond <= 0f)
            {
                return;
            }

            Damage.Write(new DamageEvent
            {
                Target = entity,
                Amount = perSecond * DeltaTime,
            });
        }
    }
}
