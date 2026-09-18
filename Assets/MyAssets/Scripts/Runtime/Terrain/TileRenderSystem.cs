using Assets.MyAssets.Scripts.Runtime.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 타일 쿼드 풀을 플레이어 주변 창(window)에 맞춰 매 프레임 갱신한다.
    /// 쿼드마다 담당 칸이 <see cref="TileViewIndex"/> 로 고정되어 있어, 창 원점만 다시 구하면
    /// 나머지는 전부 인덱스 산술이다 (<see cref="TileRenderPool"/> 주석 참조).
    ///
    /// 왜 GameplaySystemGroup 이 아니라 SimulationSystemGroup 인가:
    /// 이건 게임플레이가 아니라 **표시**다. 레벨업으로 일시정지했을 때도 지형은 계속 보여야 하고,
    /// 정지 중에 창을 갱신하지 않으면 그 사이 카메라 보간이 남아 가장자리가 빈 채로 멈춘다.
    ///
    /// TransformSystemGroup 보다 먼저 도는 이유:
    /// 여기서 쓴 LocalTransform 이 같은 프레임의 LocalToWorld(렌더 행렬)에 반영되어야 1 프레임 밀리지 않는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct TileRenderSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 쿼리를 직접 만드는 이유는 ProjectileMoveSystem 과 같다 — EnabledRefRW<MaterialMeshInfo> 가
            // "켜진 것만" 이 아니라 "있기만 하면" 을 뜻해야 한다. 꺼져 있는 쿼드를 다시 켜는 게
            // 이 시스템의 일이라, 꺼진 것이 쿼리에서 빠지면 창 밖으로 나갔던 칸이 영영 돌아오지 않는다.
            _query = SystemAPI.QueryBuilder()
                .WithAll<TileViewTag, TileViewIndex>()
                .WithAllRW<LocalTransform, URPMaterialPropertyBaseColor>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_query);
            state.RequireForUpdate<TerrainGrid>();
            state.RequireForUpdate<TileRenderPool>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TileRenderPool pool = SystemAPI.GetSingleton<TileRenderPool>();
            if (pool.ViewWidth <= 0 || pool.ViewHeight <= 0)
            {
                return;
            }

            Entity gridEntity = SystemAPI.GetSingletonEntity<TerrainGrid>();
            RefRW<TerrainGrid> grid = SystemAPI.GetComponentRW<TerrainGrid>(gridEntity);

            // 플레이어는 SubScene 로딩 전에는 없다. 그때는 맵 중앙을 보여준다.
            float2 focus = SystemAPI.TryGetSingleton(out PlayerPosition playerPosition)
                ? playerPosition.Value
                : grid.ValueRO.Origin + new float2(grid.ValueRO.Width, grid.ValueRO.Height) * (TerrainGrid.TileSize * 0.5f);

            // 창을 플레이어가 선 칸 기준으로 잡는다. 칸 단위로만 움직이므로 지형이 부드럽게
            // 흐르지 않고 한 칸씩 끊겨 보일 일이 없다 — 쿼드는 어차피 타일 중심에 고정이고,
            // 움직이는 건 카메라다.
            int2 center = grid.ValueRO.CellOf(focus);
            int2 windowOrigin = center - new int2(pool.ViewWidth / 2, pool.ViewHeight / 2);

            var job = new UpdateTileViewsJob
            {
                Tiles = grid.ValueRO.Tiles,
                Width = grid.ValueRO.Width,
                Height = grid.ValueRO.Height,
                Origin = grid.ValueRO.Origin,
                WindowOrigin = windowOrigin,
                ViewWidth = pool.ViewWidth,
                Depth = pool.Depth,
            };

            // 그리드에 쓰는 잡(연소 틱)이 끝난 뒤에 읽어야 한다. 컨테이너가 컴포넌트 안에 있어
            // ECS 의 자동 추적이 닿지 않으므로 직접 건다.
            JobHandle dependency = JobHandle.CombineDependencies(state.Dependency, grid.ValueRO.WriteHandle);
            JobHandle handle = job.ScheduleParallel(_query, dependency);

            // 다음 틱의 쓰기가 이 읽기 뒤에 일어나도록 등록한다.
            grid.ValueRW.RegisterReader(handle);
            state.Dependency = handle;
        }

        /// <summary>
        /// 쿼드 하나를 자기 자리의 타일에 맞춘다. 맵 밖 자리는 렌더러를 꺼 아무것도 그리지 않는다
        /// (맵 가장자리에 서면 창의 일부가 맵을 벗어난다).
        /// </summary>
        [BurstCompile]
        private partial struct UpdateTileViewsJob : IJobEntity
        {
            [ReadOnly] public NativeArray<TileData> Tiles;

            public int Width;
            public int Height;
            public float2 Origin;
            public int2 WindowOrigin;
            public int ViewWidth;
            public float Depth;

            private void Execute(
                in TileViewIndex viewIndex,
                ref LocalTransform transform,
                ref URPMaterialPropertyBaseColor color,
                EnabledRefRW<MaterialMeshInfo> visible)
            {
                int2 local = new int2(viewIndex.Value % ViewWidth, viewIndex.Value / ViewWidth);
                int2 cell = WindowOrigin + local;

                bool inBounds = cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
                visible.ValueRW = inBounds;
                if (!inBounds)
                {
                    return;
                }

                TileData tile = Tiles[cell.y * Width + cell.x];

                float2 center = Origin + ((float2)cell + 0.5f) * TerrainGrid.TileSize;
                transform.Position = new float3(center.x, center.y, Depth);

                color.Value = TileColors.Of(tile);
            }
        }
    }
}
