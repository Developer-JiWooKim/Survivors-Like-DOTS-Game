using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 플레이어까지의 **거리 필드** 싱글턴 (ISSUE-013 → ISSUE-014, 2026-09-18).
    ///
    /// 왜 A* 가 아닌가:
    /// A* 는 **출발지 1 → 목적지 1** 을 푼다. 적 10,000 마리에 쓰면 1 만 번 돌려야 하는데
    /// **그 1 만 개의 경로가 전부 같은 곳(플레이어)으로 수렴**한다. 방향을 뒤집어 목적지에서 한 번 퍼뜨리면
    /// 비용이 **맵 크기에 비례하고 적 수와 무관**해진다. 적에 붙는 컴포넌트도 0 이다 (기획서 8.4).
    ///
    /// 왜 방향 인덱스가 아니라 **거리**를 저장하나 (ISSUE-014):
    /// 처음엔 칸마다 방향(0~7)을 저장했는데 **경로가 계단 모양**으로 나왔다. 이유가 둘이었다.
    /// 1. BFS 는 대각선 비용도 1 로 보므로 **같은 비용의 경로가 무수히 많고**, 그중 아무거나 고정된다.
    /// 2. 방향이 8 개로 양자화되어 20° 같은 각도를 0°와 45°로 번갈아 밟는다.
    ///
    /// 거리를 저장하고 적이 **이웃 네 칸의 거리로 기울기를 구하면** 두 문제가 함께 사라진다.
    /// 빈 지형에서 거리 필드는 플레이어를 꼭짓점으로 하는 원뿔이라 기울기가 **정확히 플레이어를 가리킨다** —
    /// 완전한 직선이 나온다. 바위 근처에서는 등고선이 휘어 자연스럽게 돌아간다.
    /// 방향이 연속값이라 양자화도 없다.
    ///
    /// 잡 핸들 두 개는 <c>TerrainGrid</c>·<c>EnemySpatialHash</c> 와 같은 사정이다
    /// (컨테이너가 컴포넌트 안에 있어 ECS 의 자동 의존성 추적이 닿지 않는다).
    /// </summary>
    public struct FlowField : IComponentData
    {
        /// <summary>도달할 수 없는 칸 (바위, 또는 벽으로 막힌 구역).</summary>
        public const float Unreachable = float.MaxValue;

        /// <summary>대각선 이동 비용. 1 로 두면 체비쇼프 거리가 되어 경로가 계단이 된다 (ISSUE-014).</summary>
        public const float DiagonalCost = 1.41421356f;

        /// <summary>칸마다 플레이어까지의 거리 (타일 단위). <see cref="Unreachable"/> = 도달 불가.</summary>
        public NativeArray<float> Distance;

        public int Width;
        public int Height;
        public float2 Origin;

        /// <summary>이 필드를 만들 때 플레이어가 있던 칸. 칸이 바뀌면 다시 만든다.</summary>
        public int2 SourceCell;

        /// <summary>한 번이라도 만들어졌는가. 첫 프레임에는 필드가 없어 직진 추격으로 돈다.</summary>
        public bool HasField;

        public JobHandle WriteHandle;
        public JobHandle ReadersHandle;

        public void RegisterReader(JobHandle readerHandle)
        {
            ReadersHandle = JobHandle.CombineDependencies(ReadersHandle, readerHandle);
        }

        /// <summary>이웃을 도는 8 방향 정수 오프셋 (0~3 = 직교, 4~7 = 대각선).</summary>
        public static int2 OffsetOf(int index)
        {
            switch (index)
            {
                case 0: return new int2(1, 0);
                case 1: return new int2(-1, 0);
                case 2: return new int2(0, 1);
                case 3: return new int2(0, -1);
                case 4: return new int2(1, 1);
                case 5: return new int2(-1, 1);
                case 6: return new int2(1, -1);
                default: return new int2(-1, -1);
            }
        }

        /// <summary>해당 방향의 이동 비용. 직교 1, 대각선 √2.</summary>
        public static float CostOf(int index)
        {
            return index < 4 ? 1f : DiagonalCost;
        }

        /// <summary>
        /// 이 위치에서 플레이어 쪽으로 가는 방향 (단위 벡터). 구할 수 없으면 <c>float2.zero</c>.
        ///
        /// 이웃 네 칸의 거리 차로 기울기를 구하고 **반대 방향**(거리가 줄어드는 쪽)을 돌려준다.
        /// 막히거나 도달 불가인 이웃은 자기 칸 값으로 대신한다 — 그쪽으로는 기울기가 0 이 되어
        /// "벽 쪽으로 밀리지 않는" 효과가 자연히 생긴다.
        /// </summary>
        public static float2 DirectionAt(
            in NativeArray<float> distance,
            int width,
            int height,
            int2 cell)
        {
            if (cell.x < 0 || cell.x >= width || cell.y < 0 || cell.y >= height)
            {
                return float2.zero;
            }

            float center = distance[cell.y * width + cell.x];
            if (center >= Unreachable)
            {
                return float2.zero;
            }

            float left = Sample(distance, width, height, cell.x - 1, cell.y, center);
            float right = Sample(distance, width, height, cell.x + 1, cell.y, center);
            float down = Sample(distance, width, height, cell.x, cell.y - 1, center);
            float up = Sample(distance, width, height, cell.x, cell.y + 1, center);

            // 중앙 차분. 거리가 커지는 쪽이 +이므로 이동은 그 반대다.
            var gradient = new float2(right - left, up - down);

            float lengthSquared = math.lengthsq(gradient);
            if (lengthSquared >= 1e-6f)
            {
                return -gradient / math.sqrt(lengthSquared);
            }

            // 기울기가 0 인 **안장점**. 좌우 우회로가 똑같이 좋을 때(대칭인 U 자 바위 등) 생긴다 —
            // 중앙 차분이 양쪽에서 상쇄되어 "어디로 갈지 모르는" 상태가 된다.
            // 거리 필드에는 출발점 말고 지역 최소가 없으므로 **더 작은 이웃이 반드시 존재한다.**
            // 이산 최급강하로 되돌아가 한쪽을 고른다.
            return SteepestDescent(distance, width, height, cell, center);
        }

        /// <summary>
        /// 이웃 8 칸 중 거리가 가장 작은 쪽. 대각선은 **양옆이 모두 뚫려 있을 때만** 후보로 본다 —
        /// 이동 쪽(<c>TerrainMovement.SlideAlongWalls</c>)이 통과시키는 방향과 일치시키기 위해서다.
        /// </summary>
        private static float2 SteepestDescent(
            in NativeArray<float> distance,
            int width,
            int height,
            int2 cell,
            float center)
        {
            float best = center;
            var bestOffset = int2.zero;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    int nx = cell.x + dx;
                    int ny = cell.y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        continue;
                    }

                    if (dx != 0 && dy != 0)
                    {
                        bool sideX = IsOpen(distance, width, height, cell.x + dx, cell.y);
                        bool sideY = IsOpen(distance, width, height, cell.x, cell.y + dy);
                        if (!sideX || !sideY)
                        {
                            continue;
                        }
                    }

                    float value = distance[ny * width + nx];
                    if (value < best)
                    {
                        best = value;
                        bestOffset = new int2(dx, dy);
                    }
                }
            }

            if (bestOffset.Equals(int2.zero))
            {
                return float2.zero;
            }

            return math.normalize(new float2(bestOffset.x, bestOffset.y));
        }

        private static bool IsOpen(in NativeArray<float> distance, int width, int height, int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            return distance[y * width + x] < Unreachable;
        }

        private static float Sample(
            in NativeArray<float> distance,
            int width,
            int height,
            int x,
            int y,
            float fallback)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return fallback;
            }

            float value = distance[y * width + x];
            return value >= Unreachable ? fallback : value;
        }
    }
}
