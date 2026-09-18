using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 지형이 이동에 주는 영향 테스트 (기획서 4.2 표).
    ///
    /// 왜 테스트하나 (CLAUDE.md 5 장):
    /// 벽 미끄러짐은 **틀리면 적이 벽을 통과하거나 영구히 끼는** 종류의 로직이다.
    /// 둘 다 1 만 마리가 뛰는 화면에서는 "가끔 이상한 놈이 있다" 로만 보여 원인을 못 찾는다.
    /// 특히 **모서리 통과**(x·y 를 따로 판정할 때 대각선으로 빠져나가는 것)는 눈으로 절대 못 잡는다.
    /// </summary>
    public sealed class TerrainMovementTests
    {
        private NativeArray<TileData> _tiles;
        private int _width;
        private int _height;
        private readonly float2 _origin = float2.zero;

        private void MakeGrid(int width, int height, TileType fill = TileType.Dirt)
        {
            _width = width;
            _height = height;
            _tiles = new NativeArray<TileData>(width * height, Allocator.Temp);

            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = TileData.Of(fill);
            }
        }

        private void Set(int x, int y, TileType type)
        {
            _tiles[y * _width + x] = TileData.Of(type);
        }

        private float2 Slide(float2 from, float2 to)
        {
            return TerrainMovement.SlideAlongWalls(_tiles, _width, _height, _origin, from, to);
        }

        [TearDown]
        public void TearDown()
        {
            if (_tiles.IsCreated)
            {
                _tiles.Dispose();
            }
        }

        // ---------------------------------------------------------------- 속도 배수 (기획서 4.2)

        [Test]
        public void PlainTilesDoNotChangeSpeed()
        {
            foreach (TileType type in new[] { TileType.Dirt, TileType.Grass, TileType.Water, TileType.Rock })
            {
                Assert.AreEqual(1f, TerrainMovement.SpeedMultiplierFor(type, isPlayer: true), 1e-5f);
                Assert.AreEqual(1f, TerrainMovement.SpeedMultiplierFor(type, isPlayer: false), 1e-5f);
            }
        }

        [Test]
        public void OilSlowsEnemiesOnly()
        {
            // 기획서 4.2 — 기름: 플레이어 효과 없음, 적 -20%.
            Assert.AreEqual(1f, TerrainMovement.SpeedMultiplierFor(TileType.Oil, isPlayer: true), 1e-5f);
            Assert.AreEqual(0.8f, TerrainMovement.SpeedMultiplierFor(TileType.Oil, isPlayer: false), 1e-5f);

            // 물 위 기름도 밟는 느낌은 기름이다.
            Assert.AreEqual(0.8f, TerrainMovement.SpeedMultiplierFor(TileType.OilOnWater, isPlayer: false), 1e-5f);
        }

        [Test]
        public void IceSpeedsUpPlayerOnly()
        {
            // 기획서 4.2 — 빙판: 플레이어 +30%. 적의 미끄러짐(관성)은 아직 미구현이라 1.0.
            Assert.AreEqual(1.3f, TerrainMovement.SpeedMultiplierFor(TileType.Ice, isPlayer: true), 1e-5f);
            Assert.AreEqual(1f, TerrainMovement.SpeedMultiplierFor(TileType.Ice, isPlayer: false), 1e-5f);
        }

        [Test]
        public void CrackSlowsEnemies()
        {
            // 기획서 4.2 — 균열: 적 -40%. 플레이어 +20% 는 굴착공 전용이라 아직 적용하지 않는다.
            Assert.AreEqual(0.6f, TerrainMovement.SpeedMultiplierFor(TileType.Crack, isPlayer: false), 1e-5f);
            Assert.AreEqual(1f, TerrainMovement.SpeedMultiplierFor(TileType.Crack, isPlayer: true), 1e-5f);
        }

        [Test]
        public void SpeedMultiplierOutsideMapIsOne()
        {
            MakeGrid(4, 4, TileType.Oil);

            float outside = TerrainMovement.SpeedMultiplierAt(
                _tiles, _width, _height, _origin, new float2(-5f, 2f), isPlayer: false);

            Assert.AreEqual(1f, outside, 1e-5f);
        }

        // ---------------------------------------------------------------- 통과 판정

        [Test]
        public void OnlyRockBlocks()
        {
            Assert.IsTrue(TerrainMovement.IsBlocking(TileType.Rock));

            foreach (TileType type in new[]
                     {
                         TileType.Dirt, TileType.Grass, TileType.Water,
                         TileType.Oil, TileType.OilOnWater, TileType.Ice, TileType.Crack,
                     })
            {
                Assert.IsFalse(TerrainMovement.IsBlocking(type), $"{type} 이 통과 불가로 처리됐다");
            }
        }

        [Test]
        public void OutsideMapDoesNotBlock()
        {
            // 맵 밖은 경계 클램프가 담당한다. 여기서 막으면 두 장치가 겹쳐 경계에서 떨린다.
            MakeGrid(4, 4);
            Assert.IsFalse(TerrainMovement.IsBlockedAt(_tiles, _width, _height, _origin, new float2(-1f, 1f)));
        }

        // ---------------------------------------------------------------- 벽 미끄러짐

        [Test]
        public void MovesFreelyWhenNothingBlocks()
        {
            MakeGrid(5, 5);

            var to = new float2(3.5f, 3.5f);
            Assert.AreEqual(to, Slide(new float2(1.5f, 1.5f), to));
        }

        [Test]
        public void SlidesAlongWallInsteadOfStopping()
        {
            // 오른쪽이 바위. 대각선으로 가려 하면 x 는 막히고 y 만 진행해야 한다.
            MakeGrid(5, 5);
            Set(2, 1, TileType.Rock);

            float2 result = Slide(new float2(1.5f, 1.5f), new float2(2.5f, 2.5f));

            Assert.AreEqual(1.5f, result.x, 1e-5f, "막힌 축이 진행됐다");
            Assert.AreEqual(2.5f, result.y, 1e-5f, "막히지 않은 축이 멈췄다 — 벽에 붙으면 영영 못 돈다");
        }

        [Test]
        public void DoesNotSlipThroughDiagonalCorner()
        {
            // 대각선 목적지만 비어 있고 x·y 경유 칸이 모두 바위인 배치.
            // 축을 따로 판정할 때 **두 칸 사이 모서리로 빠져나가는** 고전적인 버그를 막는다.
            MakeGrid(5, 5);
            Set(2, 1, TileType.Rock);   // x 방향 경유
            Set(1, 2, TileType.Rock);   // y 방향 경유

            var from = new float2(1.5f, 1.5f);
            float2 result = Slide(from, new float2(2.5f, 2.5f));

            Assert.AreEqual(from, result, "모서리를 통과해 대각선으로 빠져나갔다");
        }

        [Test]
        public void BlockedOnBothAxesStaysPut()
        {
            MakeGrid(5, 5);
            Set(2, 2, TileType.Rock);

            // 바위 칸으로 직행.
            var from = new float2(1.5f, 2.5f);
            float2 result = Slide(from, new float2(2.5f, 2.5f));

            Assert.AreEqual(from.x, result.x, 1e-5f);
        }

        [Test]
        public void UnitAlreadyInsideRockIsNotTrapped()
        {
            // 스폰이 바위 위에 떨어지거나 지형이 나중에 바위로 바뀌면 영구히 갇힐 수 있다.
            // 이미 벽 안이면 막지 않고 내보낸다.
            MakeGrid(5, 5, TileType.Rock);

            var from = new float2(2.5f, 2.5f);
            var to = new float2(3.5f, 3.5f);

            Assert.AreEqual(to, Slide(from, to), "바위 안에 갇힌 유닛이 빠져나오지 못한다");
        }

        [Test]
        public void MovingOutsideMapIsNotBlockedByWallLogic()
        {
            // 맵 밖으로 나가는 이동은 여기서 막지 않는다 (경계 클램프의 몫).
            MakeGrid(4, 4);

            var to = new float2(-0.5f, 1.5f);
            Assert.AreEqual(to, Slide(new float2(0.5f, 1.5f), to));
        }
    }
}
