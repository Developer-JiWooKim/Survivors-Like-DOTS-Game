#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 확산을 눈으로 확인하기 위한 **임시 디버그 도구**. 점화원(화염구)과 기름통은 M3 후반에 들어온다.
    ///
    /// | 키 | 동작 |
    /// |---|---|
    /// | F | 플레이어가 선 칸에 불을 붙인다 |
    /// | G | 플레이어 주변에 기름을 뿌린다 (반경 3) |
    ///
    /// 왜 Input Actions 에셋이 아니라 Keyboard 직접 읽기인가:
    /// 액션 에셋을 고치는 건 사용자 몫인데(CLAUDE.md 3.4), 무기가 들어오면 지워질 디버그 키 때문에
    /// 그 절차를 요구할 이유가 없다. 이 시스템은 에디터·개발 빌드에서만 컴파일된다.
    ///
    /// SystemBase 인 이유: <c>Keyboard.current</c> 가 매니지드 API 라 Burst ISystem 에서 못 읽는다
    /// (<c>PlayerInputSystem</c> 과 같은 사정).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(TerrainTickSystem))]
    public partial class DebugTerrainPaintSystem : SystemBase
    {
        private const int OilBrushRadius = 3;

        protected override void OnCreate()
        {
            RequireForUpdate<TerrainGrid>();
            RequireForUpdate<PlayerPosition>();
        }

        protected override void OnUpdate()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool ignite = keyboard.fKey.wasPressedThisFrame;
            bool pourOil = keyboard.gKey.wasPressedThisFrame;
            if (!ignite && !pourOil)
            {
                return;
            }

            Entity gridEntity = SystemAPI.GetSingletonEntity<TerrainGrid>();
            RefRW<TerrainGrid> grid = SystemAPI.GetComponentRW<TerrainGrid>(gridEntity);

            // 그리드를 메인 스레드에서 고치므로 확산 잡과 렌더 잡이 끝난 뒤여야 한다.
            grid.ValueRO.WriteHandle.Complete();
            grid.ValueRO.ReadersHandle.Complete();

            float2 playerPosition = SystemAPI.GetSingleton<PlayerPosition>().Value;
            int2 center = grid.ValueRO.CellOf(playerPosition);

            if (pourOil)
            {
                PourOil(grid.ValueRO, center);
            }

            if (ignite)
            {
                Ignite(grid.ValueRO, center);
            }
        }

        private static void PourOil(TerrainGrid grid, int2 center)
        {
            for (int dy = -OilBrushRadius; dy <= OilBrushRadius; dy++)
            {
                for (int dx = -OilBrushRadius; dx <= OilBrushRadius; dx++)
                {
                    var cell = new int2(center.x + dx, center.y + dy);
                    if (!grid.InBounds(cell) || dx * dx + dy * dy > OilBrushRadius * OilBrushRadius)
                    {
                        continue;
                    }

                    // 바위는 덮지 않는다. 벽에 기름이 발리면 확산 경계가 헷갈린다.
                    int index = grid.IndexOf(cell);
                    TileType existing = grid.Tiles[index].TypeValue;
                    if (existing == TileType.Rock)
                    {
                        continue;
                    }

                    // 물 위에서는 기름이 뜬다 — 다 타도 물이 남는다 (ISSUE-012).
                    // 앞으로 들어올 기름통 무기도 같은 규칙을 써야 한다.
                    TileType poured = TileTypes.IsWaterLike(existing) ? TileType.OilOnWater : TileType.Oil;
                    grid.Tiles[index] = TileData.Of(poured, 200);
                }
            }
        }

        private static void Ignite(TerrainGrid grid, int2 cell)
        {
            if (!grid.InBounds(cell))
            {
                return;
            }

            int index = grid.IndexOf(cell);
            TileData tile = grid.Tiles[index];

            // 연료가 없는 칸(흙·물·바위)은 붙지 않는다. 기획서 4.3 의 연소 조건 그대로.
            if (!BurnRules.IsFlammable(tile))
            {
                return;
            }

            tile.State |= (byte)TileStateFlags.Burning;
            grid.Tiles[index] = tile;
        }
    }
}
#endif
