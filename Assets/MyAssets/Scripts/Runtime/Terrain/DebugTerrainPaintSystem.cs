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
    /// | 키 | 동작 | 나중에 대신할 것 |
    /// |---|---|---|
    /// | F | 플레이어가 선 칸에 불을 붙인다 | 화염구 |
    /// | G | 플레이어 주변에 기름을 뿌린다 (반경 3) | 기름통 |
    /// | E | 플레이어가 선 물 덩어리를 감전시킨다 | 전격 사슬 |
    /// | R | 플레이어 주변의 물을 얼린다 (반경 3) | 서리 파동 |
    /// | T | 플레이어 주변을 **넓게** 점화 (반경 30) | — (측정 전용) |
    ///
    /// T 는 게임플레이용이 아니라 **성능 측정용**이다. 연소 확산의 최악 조건(화면 전체가 불바다)을
    /// 한 번에 만들어야 지형이 프레임에 얼마나 얹히는지 잴 수 있다. F 로는 확산을 기다려야 해서
    /// 측정 시점마다 불의 규모가 달라진다 — **같은 조건에서 재라**는 CLAUDE.md 4 장을 지키기 위한 장치다.
    ///
    /// E·R 은 <see cref="TerrainEffects"/> 큐를 거친다 — 실제 무기가 쓸 경로를 그대로 쓴다.
    /// F·G 는 그리드를 직접 고친다 (칠하기는 큐를 거칠 이유가 없다).
    ///
    /// 왜 Input Actions 에셋이 아니라 Keyboard 직접 읽기인가:
    /// 액션 에셋을 고치는 건 사용자 몫인데(CLAUDE.md 3.4), 무기가 들어오면 지워질 디버그 키 때문에
    /// 그 절차를 요구할 이유가 없다. 이 시스템은 에디터·개발 빌드에서만 컴파일된다.
    ///
    /// SystemBase 인 이유: <c>Keyboard.current</c> 가 매니지드 API 라 Burst ISystem 에서 못 읽는다
    /// (<c>PlayerInputSystem</c> 과 같은 사정).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateBefore(typeof(TerrainEffectSystem))]
    public partial class DebugTerrainPaintSystem : SystemBase
    {
        private const int OilBrushRadius = 3;

        /// <summary>T 키의 점화 반경. 카메라 시야(~30×17 u)를 덮고도 남는 크기.</summary>
        private const float BlazeRadius = 30f;

        protected override void OnCreate()
        {
            RequireForUpdate<TerrainGrid>();
            RequireForUpdate<TerrainEffects>();
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
            bool shock = keyboard.eKey.wasPressedThisFrame;
            bool freeze = keyboard.rKey.wasPressedThisFrame;
            bool blaze = keyboard.tKey.wasPressedThisFrame;
            if (!ignite && !pourOil && !shock && !freeze && !blaze)
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

            // 감전·동결은 무기가 쓸 경로(효과 큐)를 그대로 쓴다. TerrainEffectSystem 이 같은 프레임에 처리한다.
            if (shock || freeze || blaze)
            {
                RefRW<TerrainEffects> effects = SystemAPI.GetSingletonRW<TerrainEffects>();

                if (shock)
                {
                    effects.ValueRW.Request(center, TerrainEffectKind.Shock);
                }

                if (freeze)
                {
                    effects.ValueRW.Request(center, TerrainEffectKind.Freeze, OilBrushRadius);
                }

                if (blaze)
                {
                    effects.ValueRW.Request(center, TerrainEffectKind.Ignite, BlazeRadius);
                }
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
