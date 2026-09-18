using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 지형 효과 요청(<see cref="TerrainEffects"/>)을 모아 그리드에 적용한다.
    /// 감전은 flood fill, 동결·점화는 반경 칠하기.
    ///
    /// **메인 스레드에서 돈다.** flood fill 은 한 요청이 수천 칸을 건드리므로 병렬로 나눌 수 없고,
    /// 나눌 이유도 없다 — 요청은 프레임당 많아야 수십 건이다.
    ///
    /// <see cref="TerrainTickSystem"/> 보다 **먼저** 도는 이유:
    /// 이번 프레임에 들어온 점화·감전이 같은 프레임의 틱에 반영되어야 한 틱(0.1 초) 밀리지 않는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(TerrainTickSystem))]
    public partial struct TerrainEffectSystem : ISystem
    {
        private NativeArray<byte> _visited;
        private NativeList<int> _body;
        private NativeList<int> _queue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TerrainGrid>();
            state.RequireForUpdate<TerrainTickSettings>();

            state.EntityManager.AddComponentData(state.SystemHandle, new TerrainEffects
            {
                Queue = new NativeQueue<TerrainEffectRequest>(Allocator.Persistent),
            });

            // flood fill 작업 버퍼는 한 번 만들어 계속 재사용한다. 매 감전마다 할당하면
            // 전격 사슬이 연쇄할 때 프레임마다 수십 번의 할당이 생긴다.
            _body = new NativeList<int>(1024, Allocator.Persistent);
            _queue = new NativeList<int>(1024, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            TerrainEffects effects = state.EntityManager.GetComponentData<TerrainEffects>(state.SystemHandle);
            if (effects.Queue.IsCreated)
            {
                effects.Queue.Dispose();
            }

            if (_visited.IsCreated)
            {
                _visited.Dispose();
            }

            _body.Dispose();
            _queue.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<TerrainEffects> effects = SystemAPI.GetComponentRW<TerrainEffects>(state.SystemHandle);
            if (effects.ValueRO.Queue.Count == 0)
            {
                return;
            }

            TerrainTickSettings settings = SystemAPI.GetSingleton<TerrainTickSettings>();

            Entity gridEntity = SystemAPI.GetSingletonEntity<TerrainGrid>();
            RefRW<TerrainGrid> gridRef = SystemAPI.GetComponentRW<TerrainGrid>(gridEntity);

            // 그리드를 메인 스레드에서 고치므로 확산 잡·렌더 잡이 모두 끝난 뒤여야 한다.
            gridRef.ValueRO.WriteHandle.Complete();
            gridRef.ValueRO.ReadersHandle.Complete();

            TerrainGrid grid = gridRef.ValueRO;

            // 방문 표시는 그리드 크기에 맞춰 한 번만 만든다.
            if (!_visited.IsCreated || _visited.Length != grid.Tiles.Length)
            {
                if (_visited.IsCreated)
                {
                    _visited.Dispose();
                }

                _visited = new NativeArray<byte>(grid.Tiles.Length, Allocator.Persistent);
            }

            while (effects.ValueRW.Queue.TryDequeue(out TerrainEffectRequest request))
            {
                switch (request.Kind)
                {
                    case TerrainEffectKind.Shock:
                        ApplyShock(grid, request.Cell, settings);
                        break;

                    case TerrainEffectKind.Freeze:
                        ApplyFreeze(grid, request, settings);
                        break;

                    case TerrainEffectKind.Ignite:
                        ApplyIgnite(grid, request);
                        break;
                }
            }
        }

        /// <summary>연결된 물 덩어리 전체를 같은 틱에 감전시킨다 (기획서 4.3).</summary>
        private void ApplyShock(TerrainGrid grid, int2 cell, in TerrainTickSettings settings)
        {
            int count = FloodFill.CollectWaterBody(
                grid, cell, ref _body, ref _visited, ref _queue, settings.ShockMaxCells);

            for (int i = 0; i < count; i++)
            {
                int index = _body[i];
                grid.Tiles[index] = TileStateRules.Shock(grid.Tiles[index], settings.ShockDurationTicks);
            }
        }

        /// <summary>반경 안의 물을 빙판으로 (기획서 4.2 — 물 + 냉기).</summary>
        private static void ApplyFreeze(TerrainGrid grid, in TerrainEffectRequest request, in TerrainTickSettings settings)
        {
            // 반경 루프를 종류마다 되풀이하는 이유:
            // 공통 루프 + 콜백으로 묶으려면 델리게이트가 필요한데 **Burst 는 매니지드 델리게이트를 못 쓴다.**
            // 루프 자체는 열 줄이 안 되므로 반복을 감수하고 Burst 를 지킨다.
            int radius = (int)math.ceil(request.Radius / TerrainGrid.TileSize);
            int radiusSq = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var cell = new int2(request.Cell.x + dx, request.Cell.y + dy);
                    if (dx * dx + dy * dy > radiusSq || !grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    if (TileTypes.IsWaterLike(grid.Tiles[index].TypeValue))
                    {
                        grid.Tiles[index] = TileStateRules.Freeze(grid.Tiles[index], settings.IceDurationTicks);
                    }
                }
            }
        }

        /// <summary>반경 안의 가연 타일에 점화 (기획서 5.1 — 화염구).</summary>
        private static void ApplyIgnite(TerrainGrid grid, in TerrainEffectRequest request)
        {
            int radius = (int)math.ceil(request.Radius / TerrainGrid.TileSize);
            int radiusSq = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    var cell = new int2(request.Cell.x + dx, request.Cell.y + dy);
                    if (dx * dx + dy * dy > radiusSq || !grid.InBounds(cell))
                    {
                        continue;
                    }

                    int index = grid.IndexOf(cell);
                    TileData tile = grid.Tiles[index];
                    if (!BurnRules.IsFlammable(tile))
                    {
                        continue;
                    }

                    tile.State |= (byte)TileStateFlags.Burning;
                    grid.Tiles[index] = tile;
                }
            }
        }
    }
}
