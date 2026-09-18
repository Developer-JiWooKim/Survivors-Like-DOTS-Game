using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 시간이 지나면 풀리는 상태(감전·동결)의 전진 규칙 테스트 (기획서 4.2·4.3).
    ///
    /// 왜 테스트하나: 타이머가 한 틱만 어긋나도 "감전이 0.5 초" 같은 기획 수치가 전부 틀어지는데,
    /// 0.5 초짜리 상태를 눈으로 세는 건 불가능하다.
    /// </summary>
    public sealed class TileStateRulesTests
    {
        [Test]
        public void Shock_SetsFlagAndTimer()
        {
            TileData shocked = TileStateRules.Shock(TileData.Of(TileType.Water), 5);

            Assert.IsTrue(shocked.HasState(TileStateFlags.Shocked));
            Assert.AreEqual(5, shocked.Timer);
        }

        [Test]
        public void Shock_LastsExactlyDurationTicks()
        {
            // 기획서 4.3: 지속 0.5 초 = 10Hz 에서 5 틱.
            const byte Duration = 5;
            TileData tile = TileStateRules.Shock(TileData.Of(TileType.Water), Duration);

            int ticksShocked = 0;
            for (int i = 0; i < 50 && tile.HasState(TileStateFlags.Shocked); i++)
            {
                ticksShocked++;
                tile = TileStateRules.StepTimer(tile);
            }

            Assert.AreEqual(Duration, ticksShocked);
            Assert.AreEqual(TileType.Water, tile.TypeValue, "감전이 풀렸다고 물이 사라지면 안 된다");
        }

        [Test]
        public void Shock_RefreshExtendsButNeverShortens()
        {
            // 전격 사슬이 같은 웅덩이를 여러 번 때릴 때, 남은 시간이 짧아지면
            // "더 때렸는데 더 빨리 풀린다" 는 이상한 일이 벌어진다.
            TileData tile = TileStateRules.Shock(TileData.Of(TileType.Water), 5);
            tile = TileStateRules.Shock(tile, 2);

            Assert.AreEqual(5, tile.Timer, "짧은 지속으로 덮어써서 남은 시간이 깎였다");

            tile = TileStateRules.Shock(tile, 9);
            Assert.AreEqual(9, tile.Timer, "긴 지속으로는 연장되어야 한다");
        }

        [Test]
        public void Freeze_TurnsWaterIntoIce()
        {
            TileData ice = TileStateRules.Freeze(TileData.Of(TileType.Water), 150);

            Assert.AreEqual(TileType.Ice, ice.TypeValue);
            Assert.AreEqual(150, ice.Timer);
        }

        [Test]
        public void Freeze_ThawsBackToWaterAfterDuration()
        {
            // 기획서 4.2: 빙판은 15 초 후 물로 해동. 흙이 되면 물이 영구히 사라진다 (ISSUE-012 와 같은 함정).
            const byte Duration = 12;
            TileData tile = TileStateRules.Freeze(TileData.Of(TileType.Water), Duration);

            for (int i = 0; i < Duration; i++)
            {
                Assert.AreEqual(TileType.Ice, tile.TypeValue, $"{i} 틱째에 이미 녹았다");
                tile = TileStateRules.StepTimer(tile);
            }

            Assert.AreEqual(TileType.Water, tile.TypeValue, "빙판이 물로 돌아가지 않았다");
            Assert.AreEqual(0, tile.Timer);
        }

        [Test]
        public void Freeze_ClearsShock()
        {
            // Timer 가 하나뿐이라 감전과 동결이 같은 타일에 공존할 수 없다.
            // 얼면 물이 아니게 되므로 감전이 풀리는 게 자연스럽다.
            TileData tile = TileStateRules.Shock(TileData.Of(TileType.Water), 5);
            tile = TileStateRules.Freeze(tile, 150);

            Assert.IsFalse(tile.HasState(TileStateFlags.Shocked));
            Assert.AreEqual(150, tile.Timer, "동결 타이머가 감전 타이머에 덮였다");
        }

        [Test]
        public void StepTimer_DoesNothingToPlainTiles()
        {
            // 타이머를 안 쓰는 타일이 영향을 받으면, 연소 중인 풀의 Timer 가 멋대로 깎인다.
            foreach (TileType type in new[] { TileType.Dirt, TileType.Grass, TileType.Water, TileType.Rock, TileType.Oil })
            {
                TileData tile = TileData.Of(type, 100);
                tile.Timer = 7;

                TileData next = TileStateRules.StepTimer(tile);
                Assert.AreEqual(7, next.Timer, $"{type} 의 Timer 가 깎였다");
                Assert.AreEqual(type, next.TypeValue);
            }
        }

        [Test]
        public void StepTimer_BurningTileKeepsUsingFuelNotTimer()
        {
            // 연소의 시계는 Fuel 이다 (기획서 4.3). 불타는 풀 위에서 감전이 따로 흘러도 서로 간섭하면 안 된다.
            TileData tile = TileData.Of(TileType.Grass, 50);
            tile.State |= (byte)TileStateFlags.Burning;
            tile = TileStateRules.Shock(tile, 3);

            tile = TileStateRules.StepTimer(tile);

            Assert.AreEqual(50, tile.Fuel, "타이머 처리가 연료를 건드렸다");
            Assert.AreEqual(2, tile.Timer);
            Assert.IsTrue(tile.HasState(TileStateFlags.Burning));
        }
    }
}
