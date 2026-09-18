using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 연소 확산 규칙 (기획서 4.3). **순수 함수만** 둔다 — 잡도 엔티티도 여기 없다.
    ///
    /// 왜 잡에서 떼어냈나 (CLAUDE.md 5 장 "틀리면 디버깅이 지옥인 것"):
    /// 확산 버그는 "가끔 한 칸 더 번진다 / 가끔 안 번진다" 형태로 나타난다. 1 만 마리가 뛰는
    /// 화면에서 눈으로 잡을 수 있는 종류가 아니다. 규칙을 순수 함수로 빼두면 EditMode 테스트로
    /// 정확히 검증할 수 있고, 잡은 "그리드를 훑으며 이 함수를 부르는 껍데기" 로 단순해진다.
    ///
    /// 결정성 (기획서 11 장 #2, 2026-09-18 결정):
    /// 난수를 <see cref="Random01"/> 로 **그 자리에서** 만든다. 같은 (타일 인덱스, 틱) 은 항상 같은 값이라
    /// 잡이 어느 순서로 실행되든, 몇 개의 워커 스레드에 나뉘든 결과가 같다.
    /// 타일에 난수 시드를 저장하는 방법은 <see cref="TileData"/> 의 4 byte 예산을 깨므로 택하지 않았다.
    /// </summary>
    public static class BurnRules
    {
        /// <summary>
        /// (타일 인덱스, 틱) → 0 이상 1 미만의 난수.
        ///
        /// 인덱스와 틱을 그냥 더하거나 XOR 하면 (인덱스 t+1, 틱 t) 와 (인덱스 t, 틱 t+1) 이 같은 값이 되어
        /// 확산이 대각선 줄무늬로 보인다. 두 값을 각각 섞은 뒤 다시 해시해 그 상관을 끊는다.
        /// </summary>
        public static float Random01(int tileIndex, uint tick)
        {
            uint hashed = math.hash(new uint2(
                math.hash(new int2(tileIndex, 0)),
                math.hash(new uint2(tick, 0x9E3779B9u))));

            // 상위 24 비트만 쓴다. 하위 비트는 해시 품질이 떨어지는 경우가 있고,
            // 24 비트면 float 가 정확히 표현할 수 있는 범위 안이다.
            return (hashed >> 8) * (1f / 16777216f);
        }

        /// <summary>이 타일이 탈 수 있는가. 기획서 4.3 — 연료가 남은 풀·기름만 탄다 (물·바위·흙은 안 탄다).</summary>
        public static bool IsFlammable(in TileData tile)
        {
            if (tile.Fuel == 0)
            {
                return false;
            }

            TileType type = tile.TypeValue;
            return type == TileType.Grass || TileTypes.IsOil(type);
        }

        /// <summary>
        /// 불타는 이웃 하나가 이 타일에 옮겨붙일 확률. 기획서 4.3 — 기름은 확률 3 배.
        /// </summary>
        public static float IgniteChancePerNeighbor(in TileData tile, in TerrainTickSettings settings)
        {
            float chance = settings.IgniteChance;
            if (TileTypes.IsOil(tile.TypeValue))
            {
                chance *= settings.OilSpreadMultiplier;
            }

            return math.saturate(chance);
        }

        /// <summary>
        /// 불타는 이웃이 <paramref name="burningNeighbors"/> 개일 때 이번 틱에 불이 붙을 확률.
        ///
        /// 이웃마다 따로 굴리지 않고 한 번만 굴리는 이유:
        /// 타일당 난수 하나면 (인덱스, 틱) 해시 하나로 끝나 결정성이 자명해진다.
        /// 이웃별로 굴리려면 "몇 번째 이웃" 까지 해시에 넣어야 하고, 그러면 같은 타일이
        /// 한 틱에 여러 난수를 쓰게 되어 테스트로 재현하기가 번거로워진다.
        /// 독립 시행 n 회와 같은 확률 1 - (1-p)^n 을 쓰면 결과 분포는 동일하다.
        /// </summary>
        public static float IgniteChance(in TileData tile, int burningNeighbors, in TerrainTickSettings settings)
        {
            if (burningNeighbors <= 0)
            {
                return 0f;
            }

            float perNeighbor = IgniteChancePerNeighbor(tile, settings);
            return 1f - math.pow(1f - perNeighbor, burningNeighbors);
        }

        /// <summary>
        /// 타일 한 칸의 다음 틱 상태. 그리드 접근이 없는 순수 함수라 테스트에서 바로 부를 수 있다.
        /// </summary>
        /// <param name="tile">현재 상태.</param>
        /// <param name="burningNeighbors">4 방향 이웃 중 연소 중인 칸 수 (대각선 제외 — 기획서 4.3).</param>
        /// <param name="random">0 이상 1 미만. 호출자가 <see cref="Random01"/> 로 만든다.</param>
        public static TileData Step(in TileData tile, int burningNeighbors, float random, in TerrainTickSettings settings)
        {
            TileData next = tile;

            if (tile.HasState(TileStateFlags.Burning))
            {
                // 기획서 4.3: 매 틱 Fuel--, 0 이 되면 소화되고 흙이 된다.
                if (next.Fuel > 0)
                {
                    next.Fuel--;
                }

                if (next.Fuel == 0)
                {
                    next.State &= unchecked((byte)~(byte)TileStateFlags.Burning);

                    // 기획서 4.3 은 "다 타면 흙" 이지만, 물 위에 뜬 기름은 **아래의 물이 남는다**
                    // (2026-09-18 결정, ISSUE-012). 기름 한 번으로 호수가 영구히 사라지는 것을 막는다.
                    next.Type = tile.TypeValue == TileType.OilOnWater
                        ? (byte)TileType.Water
                        : (byte)TileType.Dirt;
                }

                // 이미 타는 중이면 새로 붙일 것이 없다.
                return next;
            }

            if (!IsFlammable(tile))
            {
                return next;
            }

            if (random < IgniteChance(tile, burningNeighbors, settings))
            {
                next.State |= (byte)TileStateFlags.Burning;
            }

            return next;
        }
    }
}
