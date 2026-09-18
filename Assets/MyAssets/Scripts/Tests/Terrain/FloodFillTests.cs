using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 감전 flood fill 테스트 (기획서 4.3 — "연결된 물 덩어리 전체가 같은 틱에").
    ///
    /// 왜 테스트하나 (CLAUDE.md 5 장):
    /// flood fill 버그는 **떨어진 웅덩이까지 감전되거나**(연결 판정 실수) **한 칸이 빠지거나**(방문 표시 실수)
    /// 하는 형태다. 둘 다 전격 빌드의 체감을 통째로 바꾸는데, 화면에서 한 프레임 번쩍이는 걸로는 셀 수 없다.
    /// 방문 표시를 되돌리는 코드가 특히 위험하다 — 틀리면 **두 번째 감전부터** 조용히 망가진다.
    /// </summary>
    public sealed class FloodFillTests
    {
        private TerrainGrid _grid;
        private NativeArray<byte> _visited;
        private NativeList<int> _body;
        private NativeList<int> _queue;

        private void MakeGrid(int width, int height, TileType fill = TileType.Dirt)
        {
            _grid = new TerrainGrid
            {
                Tiles = new NativeArray<TileData>(width * height, Allocator.Temp),
                Width = width,
                Height = height,
                Origin = new float2(0f, 0f),
            };

            for (int i = 0; i < _grid.Tiles.Length; i++)
            {
                _grid.Tiles[i] = TileData.Of(fill);
            }

            _visited = new NativeArray<byte>(_grid.Tiles.Length, Allocator.Temp);
            _body = new NativeList<int>(64, Allocator.Temp);
            _queue = new NativeList<int>(64, Allocator.Temp);
        }

        private void Set(int x, int y, TileType type)
        {
            _grid.Tiles[y * _grid.Width + x] = TileData.Of(type);
        }

        private int Fill(int x, int y, int maxCells = 10000)
        {
            return FloodFill.CollectWaterBody(
                _grid, new int2(x, y), ref _body, ref _visited, ref _queue, maxCells);
        }

        [TearDown]
        public void TearDown()
        {
            if (_grid.Tiles.IsCreated)
            {
                _grid.Tiles.Dispose();
            }

            if (_visited.IsCreated)
            {
                _visited.Dispose();
            }

            if (_body.IsCreated)
            {
                _body.Dispose();
            }

            if (_queue.IsCreated)
            {
                _queue.Dispose();
            }
        }

        [Test]
        public void ReturnsZeroWhenStartIsNotWater()
        {
            MakeGrid(5, 5);
            Assert.AreEqual(0, Fill(2, 2));
        }

        [Test]
        public void ReturnsZeroOutsideMap()
        {
            MakeGrid(5, 5, TileType.Water);
            Assert.AreEqual(0, Fill(-1, 2));
            Assert.AreEqual(0, Fill(5, 2));
        }

        [Test]
        public void CollectsWholeConnectedBody()
        {
            // ㄷ 자 물길. 한 번에 전부 모아야 한다.
            MakeGrid(5, 5);
            Set(1, 1, TileType.Water);
            Set(1, 2, TileType.Water);
            Set(1, 3, TileType.Water);
            Set(2, 3, TileType.Water);
            Set(3, 3, TileType.Water);

            Assert.AreEqual(5, Fill(1, 1));
        }

        [Test]
        public void DoesNotReachSeparateBody()
        {
            // 떨어진 웅덩이까지 감전되면 전격이 맵 전체를 청소한다.
            MakeGrid(5, 5);
            Set(0, 0, TileType.Water);
            Set(4, 4, TileType.Water);

            Assert.AreEqual(1, Fill(0, 0));
        }

        [Test]
        public void DoesNotConnectDiagonally()
        {
            // 연소(4 방향)와 같은 연결성이어야 플레이어가 한 가지 규칙으로 읽는다.
            MakeGrid(3, 3);
            Set(0, 0, TileType.Water);
            Set(1, 1, TileType.Water);

            Assert.AreEqual(1, Fill(0, 0));
        }

        [Test]
        public void TreatsOilOnWaterAsWater()
        {
            // 기름이 떠 있어도 아래는 물이다 (ISSUE-012). 전격 콤보가 기름 때문에 끊기면 안 된다.
            MakeGrid(4, 1);
            Set(0, 0, TileType.Water);
            Set(1, 0, TileType.OilOnWater);
            Set(2, 0, TileType.Water);

            Assert.AreEqual(3, Fill(0, 0));
        }

        [Test]
        public void IceDoesNotConductAsWater()
        {
            // 얼면 물이 아니다. 여기서 이어지면 "얼려서 끊는" 플레이가 불가능해진다.
            MakeGrid(4, 1);
            Set(0, 0, TileType.Water);
            Set(1, 0, TileType.Ice);
            Set(2, 0, TileType.Water);

            Assert.AreEqual(1, Fill(0, 0));
        }

        [Test]
        public void StopsAtMaxCells()
        {
            // 상한은 성능 장치이자 밸런스 장치다 (거대한 호수 한 방 청소 방지).
            MakeGrid(10, 10, TileType.Water);

            Assert.AreEqual(7, Fill(0, 0, maxCells: 7));
        }

        [Test]
        public void DoesNotWrapAroundMapEdge()
        {
            // 가로로 감싸 돌면 맵 반대편 웅덩이가 같은 덩어리로 묶인다.
            MakeGrid(4, 3);
            Set(0, 1, TileType.Water);   // 왼쪽 끝
            Set(3, 0, TileType.Water);   // 이전 행의 오른쪽 끝

            Assert.AreEqual(1, Fill(0, 1));
        }

        [Test]
        public void SecondCallIsNotAffectedByFirst()
        {
            // 방문 표시를 되돌리지 않으면 **두 번째 감전부터** 조용히 아무것도 안 잡힌다.
            // 이 테스트가 없으면 "처음 한 번은 되는데 그 뒤로 안 된다" 를 플레이에서 찾아야 한다.
            MakeGrid(6, 6, TileType.Water);

            int first = Fill(0, 0);
            int second = Fill(0, 0);

            Assert.AreEqual(36, first);
            Assert.AreEqual(first, second, "두 번째 호출 결과가 첫 호출에 오염됐다");
        }

        [Test]
        public void SecondCallIsCleanEvenAfterHittingMaxCells()
        {
            // 상한에 걸려 중간에 멈춘 경우에도 방문 표시가 전부 되돌아와야 한다.
            // 큐에는 들어갔지만 결과에는 안 들어간 칸들이 남기 쉬운 자리다.
            MakeGrid(8, 8, TileType.Water);

            Fill(0, 0, maxCells: 5);
            Assert.AreEqual(64, Fill(0, 0), "상한에 걸린 뒤 방문 표시가 남아 다음 감전이 줄었다");
        }
    }
}
