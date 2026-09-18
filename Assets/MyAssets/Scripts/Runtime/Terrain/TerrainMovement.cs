using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 지형이 이동에 주는 영향 (기획서 4.2 표). 순수 함수만 둔다 — <see cref="BurnRules"/> 와 같은 이유.
    ///
    /// 그리드를 통째로 받지 않고 **타일 하나와 인덱스 계산만** 다루는 이유:
    /// 적 이동은 이미 공간 해시를 읽는 잡 안에 있다. 거기에 그리드까지 넘기되
    /// **판단 로직은 여기에 모아** 플레이어와 적이 같은 규칙을 쓰도록 강제한다.
    /// </summary>
    public static class TerrainMovement
    {
        /// <summary>
        /// 이 타일 위에서의 이동 속도 배수 (기획서 4.2).
        ///
        /// 플레이어와 적이 다른 값을 받는다 — 기획서 표가 "플레이어 효과" 와 "적 효과" 를 따로 정했다.
        /// 기름은 적만 느려지고, 빙판은 플레이어만 빨라진다. **지형을 플레이어에게 유리한 도구로
        /// 만들되 공짜는 아니게** 하는 설계다 (연소 피해는 플레이어에게도 들어간다 — 기획서 4.4).
        ///
        /// 〔아직 넣지 않은 것〕
        /// - 빙판 위 적의 **미끄러짐(관성)**: Velocity 컴포넌트가 필요해 미뤘다 (2026-09-18 결정).
        ///   서리 파동(냉기 무기)이 들어올 때 함께 넣는다. 지금은 배수 1.0 이다.
        /// - 균열 위 플레이어 +20% 는 **굴착공 전용**이라(기획서 4.2) 캐릭터 시스템(M5) 전까지 적용하지 않는다.
        /// </summary>
        public static float SpeedMultiplierFor(TileType type, bool isPlayer)
        {
            switch (type)
            {
                case TileType.Oil:
                case TileType.OilOnWater:
                    // 기획서 4.2 — 적 이동속도 -20%, 플레이어 효과 없음.
                    return isPlayer ? 1f : 0.8f;

                case TileType.Ice:
                    // 기획서 4.2 — 플레이어 +30%. 적의 미끄러짐은 위 주석 참조.
                    return isPlayer ? 1.3f : 1f;

                case TileType.Crack:
                    // 기획서 4.2 — 적 -40%. 플레이어 +20% 는 굴착공 전용이라 아직 적용하지 않는다.
                    return isPlayer ? 1f : 0.6f;

                default:
                    return 1f;
            }
        }

        /// <summary>통과할 수 없는 타일인가. 기획서 4.2 — 바위는 플레이어·적 모두 통과 불가.</summary>
        public static bool IsBlocking(TileType type)
        {
            return type == TileType.Rock;
        }

        /// <summary>
        /// 바위를 피해 실제로 갈 수 있는 위치를 구한다 (2026-09-18 결정 — **벽을 따라 미끄러짐**).
        ///
        /// x 와 y 를 **따로** 시험한다. 한 축이 막히면 다른 축만 진행하므로 적이 바위를 끼고 돌아
        /// 플레이어에게 도달한다. 단순히 "막히면 정지" 로 하면 **경로탐색이 없는 직진 추격이라
        /// 바위 뒤의 적이 영영 오지 못하고**, 플레이어가 바위 옆에 서는 것만으로 난이도가 무너진다.
        ///
        /// 축을 나눠 시험하는 순서(x 먼저)에 따라 모서리에서 결과가 미세하게 달라지지만,
        /// 어느 쪽이든 벽을 타고 도는 결과는 같아 체감되지 않는다.
        /// </summary>
        /// <param name="from">이동 전 위치. 이미 벽 안에 있으면 그대로 돌려준다(끼임 방지).</param>
        /// <param name="to">막힘을 무시했을 때의 목표 위치.</param>
        public static float2 SlideAlongWalls(
            in NativeArray<TileData> tiles,
            int width,
            int height,
            float2 origin,
            float2 from,
            float2 to)
        {
            // 이미 벽 안이면 어디로도 못 가게 막을 이유가 없다. 스폰이 바위 위에 떨어졌을 때
            // 영구히 갇히는 것을 막는다.
            if (IsBlockedAt(tiles, width, height, origin, from))
            {
                return to;
            }

            var result = from;

            // x 축만 이동해 본다.
            var tryX = new float2(to.x, from.y);
            if (!IsBlockedAt(tiles, width, height, origin, tryX))
            {
                result.x = to.x;
            }

            // 이어서 y 축. x 가 반영된 위치에서 판정해야 모서리를 통과하지 않는다.
            var tryY = new float2(result.x, to.y);
            if (!IsBlockedAt(tiles, width, height, origin, tryY))
            {
                result.y = to.y;
            }

            return result;
        }

        /// <summary>맵 밖은 막지 않는다 — 경계는 별도로 <c>TerrainGrid.ClampToBounds</c> 가 담당한다.</summary>
        public static bool IsBlockedAt(
            in NativeArray<TileData> tiles,
            int width,
            int height,
            float2 origin,
            float2 position)
        {
            int2 cell = (int2)math.floor((position - origin) / TerrainGrid.TileSize);
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            {
                return false;
            }

            return IsBlocking(tiles[cell.y * width + cell.x].TypeValue);
        }

        /// <summary>유닛이 선 칸의 속도 배수. 맵 밖은 1.0.</summary>
        public static float SpeedMultiplierAt(
            in NativeArray<TileData> tiles,
            int width,
            int height,
            float2 origin,
            float2 position,
            bool isPlayer)
        {
            int2 cell = (int2)math.floor((position - origin) / TerrainGrid.TileSize);
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            {
                return 1f;
            }

            return SpeedMultiplierFor(tiles[cell.y * width + cell.x].TypeValue, isPlayer);
        }
    }
}
