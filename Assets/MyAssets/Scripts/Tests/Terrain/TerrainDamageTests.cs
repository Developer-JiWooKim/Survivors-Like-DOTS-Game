using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 지형 피해 규칙 테스트 (기획서 4.3·4.4).
    ///
    /// 왜 테스트하나: 지형 피해는 **적과 플레이어 모두**에게 들어가는 규칙이라
    /// 한쪽에만 잘못 적용되면 밸런스가 통째로 무너지는데, 초당 피해는 눈으로 세기 어렵다.
    /// </summary>
    public sealed class TerrainDamageTests
    {
        private static TerrainTickSettings Settings()
        {
            return new TerrainTickSettings
            {
                TickInterval = 0.1f,
                BurnDamagePerSecond = 12f,
                ShockDamagePerSecond = 40f,
                OilDamageMultiplier = 2f,
            };
        }

        private static TileData With(TileType type, TileStateFlags state)
        {
            TileData tile = TileData.Of(type, 200);
            tile.State |= (byte)state;
            return tile;
        }

        [Test]
        public void QuietTilesDealNoDamage()
        {
            foreach (TileType type in new[] { TileType.Dirt, TileType.Grass, TileType.Water, TileType.Rock, TileType.Ice, TileType.Oil })
            {
                Assert.AreEqual(0f, TerrainDamage.PerSecondFor(TileData.Of(type, 200), Settings()), 1e-5f,
                    $"{type} 이 가만히 있는데 피해를 줬다");
            }
        }

        [Test]
        public void BurningGrassDealsBaseDamage()
        {
            Assert.AreEqual(12f, TerrainDamage.PerSecondFor(With(TileType.Grass, TileStateFlags.Burning), Settings()), 1e-5f);
        }

        [Test]
        public void BurningOilDealsDoubleDamage()
        {
            // 기획서 4.3 — 기름은 피해 2 배.
            Assert.AreEqual(24f, TerrainDamage.PerSecondFor(With(TileType.Oil, TileStateFlags.Burning), Settings()), 1e-5f);

            // 물 위에 떠 있어도 타는 것은 기름이다.
            Assert.AreEqual(24f, TerrainDamage.PerSecondFor(With(TileType.OilOnWater, TileStateFlags.Burning), Settings()), 1e-5f);
        }

        [Test]
        public void ShockedWaterDealsShockDamage()
        {
            Assert.AreEqual(40f, TerrainDamage.PerSecondFor(With(TileType.Water, TileStateFlags.Shocked), Settings()), 1e-5f);
        }

        [Test]
        public void BurningAndShockedStack()
        {
            // 기름에 불이 붙은 채 전격이 들어오는 게 기획서 5.4 가 노리는 콤보다. 더해져야 한다.
            TileData tile = With(TileType.OilOnWater, TileStateFlags.Burning | TileStateFlags.Shocked);

            // 연소 12 × 기름 2 배 = 24, 감전 40 → 64
            Assert.AreEqual(64f, TerrainDamage.PerSecondFor(tile, Settings()), 1e-5f);
        }

        [Test]
        public void OilMultiplierAppliesOnlyToBurn()
        {
            // 기름 배수가 감전에까지 곱해지면 전격 피해가 지형에 따라 2 배가 된다 — 기획서에 없는 규칙이다.
            TileData shockedOil = With(TileType.OilOnWater, TileStateFlags.Shocked);
            Assert.AreEqual(40f, TerrainDamage.PerSecondFor(shockedOil, Settings()), 1e-5f);
        }

        [Test]
        public void FrozenTileDealsNoDamage()
        {
            // 동결은 피해 상태가 아니다 (기획서 4.2 — 빙판은 이동 효과만).
            Assert.AreEqual(0f, TerrainDamage.PerSecondFor(With(TileType.Ice, TileStateFlags.Frozen), Settings()), 1e-5f);
        }
    }
}
