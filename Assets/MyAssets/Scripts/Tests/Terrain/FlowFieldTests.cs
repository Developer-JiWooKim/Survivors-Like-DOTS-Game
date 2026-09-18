using Assets.MyAssets.Scripts.Runtime.Terrain;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Tests.Terrain
{
    /// <summary>
    /// 거리 필드 + 기울기 경로 테스트 (ISSUE-013, ISSUE-014).
    ///
    /// 왜 테스트하나 (CLAUDE.md 5 장):
    /// 필드가 틀리면 **적 전체가 동시에 이상하게 움직인다.** 한 마리의 버그가 아니라
    /// 1 만 마리가 같은 잘못된 방향으로 가므로 증상은 크지만 원인은 배열 한 칸이다.
    /// 그리고 ISSUE-014 가 보여줬듯 **"틀리지는 않았지만 이상한" 경로**는 테스트가 없으면
    /// 플레이해 보기 전까지 드러나지 않는다 — 그래서 "직선으로 오는가" 까지 테스트로 박아 둔다.
    /// </summary>
    public sealed class FlowFieldTests
    {
        private NativeArray<TileData> _tiles;
        private NativeArray<float> _distance;
        private int _width;
        private int _height;

        private void MakeGrid(int width, int height, TileType fill = TileType.Dirt)
        {
            _width = width;
            _height = height;
            int count = width * height;

            _tiles = new NativeArray<TileData>(count, Allocator.Temp);
            for (int i = 0; i < count; i++)
            {
                _tiles[i] = TileData.Of(fill);
            }

            _distance = new NativeArray<float>(count, Allocator.Temp);
        }

        private void Set(int x, int y, TileType type)
        {
            _tiles[y * _width + x] = TileData.Of(type);
        }

        private int Build(int sx, int sy)
        {
            return FlowFieldBuilder.Build(_tiles, _width, _height, new int2(sx, sy), ref _distance);
        }

        private float DistAt(int x, int y)
        {
            return _distance[y * _width + x];
        }

        private float2 DirAt(int x, int y)
        {
            return FlowField.DirectionAt(_distance, _width, _height, new int2(x, y));
        }

        /// <summary>
        /// 적이 실제로 하는 것과 같은 방식으로 걸어가 목적지에 닿는지. 닿지 못하면 -1.
        /// **충돌 처리(<see cref="TerrainMovement.SlideAlongWalls"/>)를 함께 쓴다** —
        /// 필드만 따라가면 벽을 통과해 버려서 "실제로 도달 가능한가" 를 검증하지 못한다.
        /// </summary>
        private float WalkToSource(float2 from, float2 target, int maxSteps = 4000)
        {
            var at = from;
            const float Step = 0.25f;
            var origin = float2.zero;

            for (int i = 0; i < maxSteps; i++)
            {
                if (math.distance(at, target) < 1f)
                {
                    return i * Step;
                }

                var cell = (int2)math.floor(at);
                float2 direction = FlowField.DirectionAt(_distance, _width, _height, cell);
                if (math.lengthsq(direction) < 1e-6f)
                {
                    // 필드가 방향을 못 주는 칸 — 목표 칸 자신이면 도착으로 본다.
                    return math.distance(at, target) < 1.5f ? i * Step : -1f;
                }

                float2 next = TerrainMovement.SlideAlongWalls(
                    _tiles, _width, _height, origin, at, at + direction * Step);

                // 한 걸음도 못 갔다면 끼인 것이다 (ISSUE-013 의 증상).
                if (math.distancesq(next, at) < 1e-8f)
                {
                    return -1f;
                }

                at = next;
            }

            return -1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_tiles.IsCreated) _tiles.Dispose();
            if (_distance.IsCreated) _distance.Dispose();
        }

        // ---------------------------------------------------------------- 기본

        [Test]
        public void SourceDistanceIsZero()
        {
            MakeGrid(5, 5);
            Build(2, 2);

            Assert.AreEqual(0f, DistAt(2, 2), 1e-5f);
        }

        [Test]
        public void OpenFieldReachesEveryCell()
        {
            MakeGrid(9, 9);
            Build(4, 4);

            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    Assert.Less(DistAt(x, y), FlowField.Unreachable, $"({x},{y}) 에 도달하지 못했다");
                }
            }
        }

        [Test]
        public void RockCellsStayUnreachable()
        {
            MakeGrid(5, 5);
            Set(2, 1, TileType.Rock);
            Build(2, 3);

            Assert.AreEqual(FlowField.Unreachable, DistAt(2, 1), "바위 칸에 거리가 생겼다");
        }

        [Test]
        public void SealedAreaIsUnreachable()
        {
            MakeGrid(5, 5);
            Set(0, 1, TileType.Rock);
            Set(1, 1, TileType.Rock);
            Set(1, 0, TileType.Rock);

            Build(4, 4);

            Assert.AreEqual(FlowField.Unreachable, DistAt(0, 0), "막힌 구역에 거리가 생겼다");
            Assert.AreEqual(float2.zero, DirAt(0, 0), "도달 불가 칸이 방향을 준다");
        }

        // ---------------------------------------------------------------- 거리 척도 (ISSUE-014 의 원인)

        [Test]
        public void StraightNeighborIsExactlyOne()
        {
            MakeGrid(9, 9);
            Build(4, 4);

            Assert.AreEqual(1f, DistAt(5, 4), 1e-4f, "직교 이웃의 거리가 1 이 아니다");
        }

        [Test]
        public void DistanceApproximatesEuclidean()
        {
            // 빈 지형에서 거리 필드는 플레이어를 꼭짓점으로 하는 **매끈한 원뿔**이어야 한다.
            // 체비쇼프(대각선 비용 1)나 팔각(√2) 척도는 여기서 크게 어긋나고, 그게 계단 경로의 원인이었다.
            MakeGrid(21, 21);
            var source = new int2(10, 10);
            Build(source.x, source.y);

            for (int y = 0; y < 21; y++)
            {
                for (int x = 0; x < 21; x++)
                {
                    float euclidean = math.distance(new float2(x, y), new float2(source.x, source.y));
                    float field = DistAt(x, y);

                    // 1 차 아이코널 해는 유클리드 거리를 조금 과대평가한다 (대각선에서 최대 ~8%).
                    Assert.GreaterOrEqual(field, euclidean - 1e-3f, $"({x},{y}) 거리가 유클리드보다 짧다");
                    Assert.LessOrEqual(field, euclidean * 1.09f + 1e-3f, $"({x},{y}) 거리가 너무 크다 — 척도가 왜곡됐다");
                }
            }
        }

        // ---------------------------------------------------------------- 경로 모양 (ISSUE-014 의 본체)

        [Test]
        public void OpenTerrainDirectionPointsStraightAtSource()
        {
            // **ISSUE-014 가 고쳐졌는지 보는 테스트.**
            // 빈 지형에서는 어느 칸에서든 기울기가 플레이어를 정확히 가리켜야 한다.
            // 방향 인덱스(8 방향) 방식에서는 최대 22.5° 까지 어긋났다.
            MakeGrid(31, 31);
            var source = new int2(15, 15);
            Build(source.x, source.y);

            float worstDegrees = 0f;

            for (int y = 1; y < 30; y++)
            {
                for (int x = 1; x < 30; x++)
                {
                    if (x == source.x && y == source.y)
                    {
                        continue;
                    }

                    float2 expected = math.normalize(new float2(source.x - x, source.y - y));
                    float2 actual = DirAt(x, y);

                    float degrees = math.degrees(math.acos(math.clamp(math.dot(expected, actual), -1f, 1f)));
                    worstDegrees = math.max(worstDegrees, degrees);
                }
            }

            Assert.Less(worstDegrees, 8f, $"빈 지형인데 방향이 최대 {worstDegrees:F1}° 어긋난다 — 직선으로 오지 않는다");
        }

        [Test]
        public void OpenTerrainPathIsStraight()
        {
            // 위 테스트의 결과 확인. 실제로 걸어가 본 거리가 직선 거리와 거의 같아야 한다.
            MakeGrid(41, 41);
            var source = new int2(20, 20);
            Build(source.x, source.y);

            var from = new float2(2.5f, 6.5f);   // 축에도 대각선에도 정렬되지 않은 각도
            var target = new float2(source.x + 0.5f, source.y + 0.5f);

            float walked = WalkToSource(from, target);
            float straight = math.distance(from, target);

            Assert.Greater(walked, 0f, "플레이어에 닿지 못했다");
            Assert.Less(walked, straight * 1.15f, $"직선 {straight:F1} 인데 {walked:F1} 을 걸었다 — 경로가 돌아간다");
        }

        // ---------------------------------------------------------------- 우회 (ISSUE-013)

        [Test]
        public void PathGoesAroundWall()
        {
            // 가로 벽이 가운데를 막고 오른쪽 끝에만 틈이 있다.
            MakeGrid(9, 9);
            for (int x = 0; x < 8; x++)
            {
                Set(x, 4, TileType.Rock);
            }

            var source = new int2(1, 7);
            Build(source.x, source.y);

            // 벽 아래에서도 도달 가능해야 하고, 직선보다는 확실히 멀어야 한다(틈으로 돌아가므로).
            float distance = DistAt(1, 1);
            Assert.Less(distance, FlowField.Unreachable, "벽 뒤에서 도달하지 못한다");
            Assert.Greater(distance, 6f, "벽을 통과하는 경로가 나왔다");
        }

        [Test]
        public void EscapesConcaveTrap()
        {
            // U 자 함정. 한 칸 앞만 보는 탐욕적 방식이 갇히는 모양 — 필드를 고른 이유가 여기에 있다.
            MakeGrid(11, 11);

            for (int x = 3; x <= 7; x++)
            {
                Set(x, 5, TileType.Rock);
            }

            Set(3, 6, TileType.Rock);
            Set(7, 6, TileType.Rock);
            Set(3, 7, TileType.Rock);
            Set(7, 7, TileType.Rock);

            var source = new int2(5, 1);
            Build(source.x, source.y);

            var from = new float2(5.5f, 6.5f);
            Assert.Greater(WalkToSource(from, new float2(5.5f, 1.5f)), 0f, "오목한 함정에서 빠져나오지 못한다");
        }

        // ---------------------------------------------------------------- 경로 규칙 == 충돌 규칙

        [Test]
        public void PathDoesNotCutDiagonalCorner()
        {
            // 대각선으로 맞닿은 두 바위 사이를 경로가 지나가면, 이동 쪽(SlideAlongWalls)이 막으므로
            // 적이 "가라는 방향으로 갈 수 없는" 상태가 된다.
            MakeGrid(5, 5);
            Set(2, 1, TileType.Rock);
            Set(1, 2, TileType.Rock);

            Build(2, 2);

            // (1,1) 은 대각선으로만 (2,2) 에 닿을 수 있는데 양옆이 바위다 → 그 대각선은 막혀야 한다.
            // 모서리를 통과했다면 거리가 √2(≈1.41) 근처로 나온다. 실제로는 크게 돌아야 한다.
            Assert.Greater(DistAt(1, 1), 2f, "경로가 모서리를 대각선으로 통과한다");
        }

        [Test]
        public void PathAndCollisionAgreeOnEveryCell()
        {
            // **이 덩어리에서 가장 위험한 자리.**
            // 무작위 바위 배치에서 **모든 칸**의 기울기 방향이 실제로 이동 가능한지 충돌 규칙으로 확인한다.
            // 두 규칙이 어긋나면 적이 그 칸에서 경로를 벗어나거나 끼인다.
            const int Size = 24;
            MakeGrid(Size, Size);

            // 결정적인 의사난수 배치 — 실패를 재현할 수 있어야 한다.
            for (int i = 0; i < Size * Size; i++)
            {
                if (math.hash(new int2(i, 7)) % 100 < 20)
                {
                    _tiles[i] = TileData.Of(TileType.Rock);
                }
            }

            var source = new int2(Size / 2, Size / 2);
            _tiles[source.y * Size + source.x] = TileData.Of(TileType.Dirt);
            Build(source.x, source.y);

            var origin = float2.zero;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (TerrainMovement.IsBlocking(_tiles[y * Size + x].TypeValue))
                    {
                        continue;
                    }

                    float2 direction = DirAt(x, y);
                    if (math.lengthsq(direction) < 1e-6f)
                    {
                        continue;
                    }

                    var from = new float2(x + 0.5f, y + 0.5f);
                    float2 to = from + direction * 0.45f;

                    float2 slid = TerrainMovement.SlideAlongWalls(_tiles, Size, Size, origin, from, to);

                    // 검증하는 것은 "벡터가 그대로 통과하는가" 가 아니라 **"적이 움직일 수 있는가"** 다.
                    // 벽에 붙은 칸에서는 한 축이 깎일 수 있고, 그건 정상적인 미끄러짐이다.
                    // 문제가 되는 것은 **두 축이 모두 깎여 제자리에 멈추는 것** — ISSUE-013 의 증상이다.
                    Assert.Greater(
                        math.distance(slid, from), 1e-4f,
                        $"({x},{y}) 에서 필드가 가리키는 방향으로 한 걸음도 못 간다 — 적이 여기서 끼인다");

                    // 그리고 그 한 걸음은 플레이어에 **가까워지는** 방향이어야 한다.
                    var slidCell = (int2)math.floor(slid);
                    if (slidCell.x >= 0 && slidCell.x < Size && slidCell.y >= 0 && slidCell.y < Size)
                    {
                        float after = _distance[slidCell.y * Size + slidCell.x];
                        float before = _distance[y * Size + x];

                        Assert.LessOrEqual(
                            after, before + 1f,
                            $"({x},{y}) 에서 미끄러진 결과가 플레이어에게서 멀어진다");
                    }
                }
            }
        }

        // ---------------------------------------------------------------- 수렴 · 재사용

        [Test]
        public void ConvergesWellWithinSweepLimit()
        {
            // 스윕이 상한에서 잘리면 거리가 부정확해진다. 노이즈 덩어리 지형에서 몇 번이면 끝나는지 확인.
            const int Size = 48;
            MakeGrid(Size, Size);

            for (int i = 0; i < Size * Size; i++)
            {
                if (math.hash(new int2(i, 31)) % 100 < 18)
                {
                    _tiles[i] = TileData.Of(TileType.Rock);
                }
            }

            var source = new int2(Size / 2, Size / 2);
            _tiles[source.y * Size + source.x] = TileData.Of(TileType.Dirt);

            int sweeps = Build(source.x, source.y);

            Assert.Less(sweeps, 16, $"스윕 {sweeps} 회 — 상한에 걸렸다. 다익스트라로 바꿔야 한다");
        }

        [Test]
        public void RebuildIsNotPollutedByPreviousBuild()
        {
            // 거리 배열을 재사용하므로, 지우지 않으면 **두 번째 필드부터** 옛 값이 남는다.
            MakeGrid(9, 9);

            Build(0, 0);
            Build(8, 8);

            Assert.AreEqual(0f, DistAt(8, 8), 1e-5f, "새 출발점의 거리가 0 이 아니다");
            Assert.Greater(DistAt(0, 0), 7f, "옛 출발점 자리에 옛 거리가 남아 있다");
        }

        [Test]
        public void SourceInsideRockStillProducesField()
        {
            // 플레이어가 바위 안에 들어가는 상황에서 필드를 포기하면 **적 전체가 멈춘다.**
            MakeGrid(5, 5);
            Set(2, 2, TileType.Rock);

            Build(2, 2);

            Assert.Less(DistAt(1, 1), FlowField.Unreachable, "플레이어가 바위 안일 때 필드가 통째로 비었다");
        }

        [Test]
        public void DirectionIsUnitLength()
        {
            MakeGrid(9, 9);
            Build(4, 4);

            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    float2 direction = DirAt(x, y);
                    float length = math.length(direction);

                    if (length > 1e-6f)
                    {
                        Assert.AreEqual(1f, length, 1e-4f, $"({x},{y}) 방향이 단위 벡터가 아니다 — 그쪽 적이 빨라진다");
                    }
                }
            }
        }
    }
}
