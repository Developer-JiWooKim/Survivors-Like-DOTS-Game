using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 플레이어 칸에서의 거리 필드를 만든다 (ISSUE-014).
    /// 잡도 엔티티도 없는 순수 함수 — 경로가 맞는지를 EditMode 테스트로 검증하기 위해서다.
    ///
    /// **아이코널 방정식(|∇D| = 1)을 래스터 스윕으로 푼다** (fast sweeping method).
    ///
    /// 왜 "이웃 중 최소 + 이동 비용" 이 아닌가 — 이게 ISSUE-014 의 핵심이다:
    /// - 대각선 비용을 1 로 두면 체비쇼프 거리가 되어 같은 비용의 경로가 무수히 많아지고 경로가 계단이 된다.
    /// - 대각선을 √2 로 고쳐도 **팔각(octile) 거리**가 될 뿐이다. 이 척도의 기울기는 팔분면마다 **상수**라
    ///   방향이 22.5° 단위로 양자화된다 — 실측에서 최대 20.4° 어긋났다. 계단이 각도만 바뀌어 남는다.
    /// - 아이코널 해는 **진짜 유클리드 거리**에 수렴한다. 빈 지형에서 필드가 매끈한 원뿔이 되어
    ///   기울기가 플레이어를 정확히 가리킨다 → **직선**. 장애물 근처에서는 등고선이 휘어 자연스럽게 돌아간다.
    ///
    /// 갱신 규칙(속도 1):
    /// 직교 이웃에서 x 방향 최소 a, y 방향 최소 b 를 얻어
    /// <c>|a-b| ≥ 1</c> 이면 <c>min(a,b)+1</c>, 아니면 <c>(a+b+√(2-(a-b)²))/2</c>.
    /// 앞은 한 축에서만 파면이 오는 경우, 뒤는 두 축에서 비스듬히 오는 경우다.
    ///
    /// 왜 우선순위 큐(다익스트라·FMM)가 아니라 스윕인가:
    /// 큐도 방문 표시도 없어 **추가 메모리가 0** 이고, 배열을 순서대로 훑어 **캐시 지역성이 좋다**.
    /// 분기와 간접 참조가 거의 없어 Burst 에 얹기도 좋다.
    /// 값이 줄기만 하고 유한하므로 반드시 수렴한다. 필요한 스윕 수는 **경로가 방향을 바꾸는 횟수**에
    /// 비례하며, 노이즈 덩어리 지형에서는 몇 회면 끝난다 (테스트 <c>ConvergesWellWithinSweepLimit</c>).
    /// </summary>
    public static class FlowFieldBuilder
    {
        /// <summary>
        /// 거리 필드를 채운다.
        /// </summary>
        /// <returns>수행한 스윕 횟수. <paramref name="maxSweeps"/> 와 같으면 수렴 전에 잘렸다는 뜻이다.</returns>
        public static int Build(
            in NativeArray<TileData> tiles,
            int width,
            int height,
            int2 source,
            ref NativeArray<float> distance,
            int maxSweeps = 32)
        {
            for (int i = 0; i < distance.Length; i++)
            {
                distance[i] = FlowField.Unreachable;
            }

            if (source.x < 0 || source.x >= width || source.y < 0 || source.y >= height)
            {
                return 0;
            }

            int sourceIndex = source.y * width + source.x;

            // 플레이어가 바위 안에 있어도(경계 처리 실패 등) 출발점을 심는다.
            // 여기서 포기하면 맵 전체가 도달 불가가 되어 **적이 전부 멈춘다** — 증상이 훨씬 나쁘다.
            distance[sourceIndex] = 0f;

            int sweeps = 0;
            bool changed = true;

            // 스윕 방향 4 가지를 돌아가며 쓴다. 한 방향만 쓰면 그 반대로 퍼지는 파면이 한 번에 못 간다.
            while (changed && sweeps < maxSweeps)
            {
                bool xForward = (sweeps & 1) == 0;
                bool yForward = (sweeps & 2) == 0;

                changed = Sweep(tiles, width, height, sourceIndex, ref distance, xForward, yForward);
                sweeps++;
            }

            return sweeps;
        }

        private static bool Sweep(
            in NativeArray<TileData> tiles,
            int width,
            int height,
            int sourceIndex,
            ref NativeArray<float> distance,
            bool xForward,
            bool yForward)
        {
            bool changed = false;

            int yStart = yForward ? 0 : height - 1;
            int yEnd = yForward ? height : -1;
            int yStep = yForward ? 1 : -1;

            int xStart = xForward ? 0 : width - 1;
            int xEnd = xForward ? width : -1;
            int xStep = xForward ? 1 : -1;

            for (int y = yStart; y != yEnd; y += yStep)
            {
                for (int x = xStart; x != xEnd; x += xStep)
                {
                    int index = y * width + x;

                    // 바위는 지나갈 수 없으므로 거리를 갖지 않는다. 출발 칸만은 예외다(위 주석).
                    if (index != sourceIndex && IsBlockedAt(tiles, width, height, x, y))
                    {
                        continue;
                    }

                    // 축 정렬 스텐실 (간격 1)
                    float a = math.min(
                        Read(tiles, distance, width, height, sourceIndex, x - 1, y),
                        Read(tiles, distance, width, height, sourceIndex, x + 1, y));

                    float b = math.min(
                        Read(tiles, distance, width, height, sourceIndex, x, y - 1),
                        Read(tiles, distance, width, height, sourceIndex, x, y + 1));

                    float candidate = Solve(a, b, 1f);

                    // 45° 돌아간 대각선 스텐실 (간격 √2)
                    //
                    // 왜 둘을 함께 쓰나: 축 정렬 스텐실만 쓰면 오차가 **대각선 방향에서 최대**가 되어
                    // (실측 방향 오차 11.5°) 비스듬히 오는 적의 경로가 여전히 휜다.
                    // 대각선 스텐실은 반대로 축 방향에서 오차가 크다. 둘의 최솟값을 취하면
                    // 서로의 약한 구간을 덮어 이방성이 크게 줄어든다.
                    float c = math.min(
                        ReadDiagonal(tiles, distance, width, height, sourceIndex, x, y, -1, -1),
                        ReadDiagonal(tiles, distance, width, height, sourceIndex, x, y, 1, 1));

                    float d = math.min(
                        ReadDiagonal(tiles, distance, width, height, sourceIndex, x, y, -1, 1),
                        ReadDiagonal(tiles, distance, width, height, sourceIndex, x, y, 1, -1));

                    candidate = math.min(candidate, Solve(c, d, FlowField.DiagonalCost));

                    if (candidate < distance[index])
                    {
                        distance[index] = candidate;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// 아이코널 갱신. 서로 직교하는 두 축에서 온 거리 <paramref name="a"/>·<paramref name="b"/> 와
        /// 그 축의 격자 간격 <paramref name="h"/> 로 이 칸의 거리를 푼다.
        /// </summary>
        private static float Solve(float a, float b, float h)
        {
            bool hasA = a < FlowField.Unreachable;
            bool hasB = b < FlowField.Unreachable;

            if (!hasA && !hasB)
            {
                return FlowField.Unreachable;
            }

            if (!hasA)
            {
                return b + h;
            }

            if (!hasB)
            {
                return a + h;
            }

            float difference = math.abs(a - b);
            if (difference >= h)
            {
                // 파면이 사실상 한 축에서만 온다 — 비스듬한 해가 존재하지 않는 경우.
                return math.min(a, b) + h;
            }

            return (a + b + math.sqrt(2f * h * h - difference * difference)) * 0.5f;
        }

        /// <summary>
        /// 대각선 이웃의 거리. **양옆 중 하나라도 막혀 있으면 읽지 않는다.**
        ///
        /// 이동 쪽(<see cref="TerrainMovement.SlideAlongWalls"/>)이 x·y 를 따로 시험해 한쪽만 막혀도
        /// 그 축을 버리기 때문이다. 경로 규칙이 더 느슨하면 적이 "가라는 대로 갈 수 없는" 상태가 된다
        /// (ISSUE-013 에서 배운 것. 테스트 <c>PathAndCollisionAgreeOnEveryCell</c> 가 지킨다).
        /// </summary>
        private static float ReadDiagonal(
            in NativeArray<TileData> tiles,
            in NativeArray<float> distance,
            int width,
            int height,
            int sourceIndex,
            int x,
            int y,
            int dx,
            int dy)
        {
            if (IsBlockedAt(tiles, width, height, x + dx, y) ||
                IsBlockedAt(tiles, width, height, x, y + dy))
            {
                return FlowField.Unreachable;
            }

            return Read(tiles, distance, width, height, sourceIndex, x + dx, y + dy);
        }

        /// <summary>이웃 칸의 거리. 맵 밖이거나 바위면 <see cref="FlowField.Unreachable"/>.</summary>
        private static float Read(
            in NativeArray<TileData> tiles,
            in NativeArray<float> distance,
            int width,
            int height,
            int sourceIndex,
            int x,
            int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return FlowField.Unreachable;
            }

            int index = y * width + x;

            // 출발 칸은 바위여도 읽는다 (플레이어가 바위 안인 경우).
            if (index != sourceIndex && IsBlockedAt(tiles, width, height, x, y))
            {
                return FlowField.Unreachable;
            }

            return distance[index];
        }

        private static bool IsBlockedAt(in NativeArray<TileData> tiles, int width, int height, int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return true;
            }

            return TerrainMovement.IsBlocking(tiles[y * width + x].TypeValue);
        }
    }
}
