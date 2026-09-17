using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Spatial
{
    /// <summary>
    /// <see cref="EnemySpatialHash"/> 조회 함수 모음. Burst 호환 static 이라 잡·메인 스레드·테스트에서 같은 코드를 쓴다.
    ///
    /// 시스템 안의 private 메서드에서 이리로 옮긴 이유:
    /// "틀리면 디버깅이 지옥인" 공간 해시 조회는 테스트 대상이다 (CLAUDE.md 5장).
    /// 시스템 밖으로 꺼내야 테스트에서 전수 비교 결과와 직접 대조할 수 있다.
    /// </summary>
    public static class SpatialQueries
    {
        /// <summary>한 번에 모을 수 있는 최대 개수. 결과를 담는 고정 크기 목록의 용량에 맞춘다.</summary>
        public const int MaxNearestCount = 16;

        /// <summary>
        /// <paramref name="origin"/> 에서 <paramref name="range"/> 안에 있는 적을 최대 <paramref name="count"/> 마리,
        /// **가까운 순**으로 <paramref name="positions"/> 에 담는다.
        ///
        /// 원점 셀부터 고리 모양으로 넓혀가며 보고, count 번째로 가까운 적보다 가까울 수 없는 고리에 도달하면 멈춘다.
        /// </summary>
        public static void FindNearest(
            in NativeParallelMultiHashMap<int, AgentRef> map, float2 origin, float range, int count,
            ref FixedList512Bytes<float2> positions)
        {
            positions.Clear();
            count = math.clamp(count, 0, MaxNearestCount);
            if (count == 0)
            {
                return;
            }

            // 거리 제곱을 위치와 나란히 들고 정렬 삽입한다. count 가 작아(≤16) 삽입 정렬이 가장 싸다.
            var distances = new FixedList128Bytes<float>();
            float rangeSquared = range * range;

            int2 center = EnemySpatialHash.CellOf(origin);

            // 적은 **중심 좌표**의 셀에 들어 있다. 거리 range 안의 중심은 원점 셀에서 ceil(range / 셀) 칸 안에 있다.
            int maxRing = (int)math.ceil(range / EnemySpatialHash.CellSize);

            for (int ring = 0; ring <= maxRing; ring++)
            {
                // 고리 ring 의 셀 = 중심에서 체비쇼프 거리가 정확히 ring 인 칸들 (테두리만)
                for (int y = -ring; y <= ring; y++)
                {
                    bool edgeRow = y == -ring || y == ring;
                    int step = edgeRow ? 1 : ring * 2; // 가운데 행은 양 끝 두 칸만 본다

                    for (int x = -ring; x <= ring; x += step)
                    {
                        int2 cell = center + new int2(x, y);
                        if (!map.TryGetFirstValue(EnemySpatialHash.KeyOf(cell), out AgentRef agent, out NativeParallelMultiHashMapIterator<int> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            // 키 충돌로 섞여 든 다른 셀의 적은 건너뛴다 — 안 그러면 같은 적이 목록에 두 번 들어간다.
                            if (!EnemySpatialHash.IsInCell(agent, cell))
                            {
                                continue;
                            }

                            float distanceSquared = math.distancesq(origin, agent.Position);
                            if (distanceSquared <= rangeSquared)
                            {
                                InsertSorted(ref positions, ref distances, agent.Position, distanceSquared, count);
                            }
                        }
                        while (map.TryGetNextValue(out agent, ref iterator));
                    }
                }

                // 다음 고리(ring + 1)의 셀은 원점에서 최소 ring × 셀 크기만큼 떨어져 있다
                // (원점은 중심 셀 안 어딘가에 있으므로). 이미 count 마리를 모았고 가장 먼 것이 그보다 가까우면 멈춘다.
                float nextRingMinDistance = ring * EnemySpatialHash.CellSize;
                if (positions.Length == count && distances[count - 1] <= nextRingMinDistance * nextRingMinDistance)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 반경 <paramref name="radius"/> 인 원과 겹치면서 <paramref name="exclude"/> 에 없는 적 하나를 찾는다.
        /// 어느 적이 나올지는 순서에 의존하므로 "아무거나 하나" 다.
        /// </summary>
        /// <param name="exclude">제외할 적. <c>FixedList.Contains</c> 가 <c>ref this</c> 확장이라 ref 로 받는다.</param>
        public static bool TryFindOverlap(
            in NativeParallelMultiHashMap<int, AgentRef> map, float2 position, float radius,
            ref FixedList128Bytes<Entity> exclude, out AgentRef found)
        {
            int range = EnemySpatialHash.CellRangeFor(radius);
            int2 center = EnemySpatialHash.CellOf(position);

            for (int y = -range; y <= range; y++)
            {
                for (int x = -range; x <= range; x++)
                {
                    int key = EnemySpatialHash.KeyOf(center + new int2(x, y));
                    if (!map.TryGetFirstValue(key, out AgentRef agent, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        continue;
                    }

                    do
                    {
                        float reach = radius + agent.Radius;
                        if (math.distancesq(position, agent.Position) <= reach * reach
                            && !exclude.Contains(agent.Entity))
                        {
                            found = agent;
                            return true;
                        }
                    }
                    while (map.TryGetNextValue(out agent, ref iterator));
                }
            }

            found = default;
            return false;
        }

        private static void InsertSorted(
            ref FixedList512Bytes<float2> positions, ref FixedList128Bytes<float> distances,
            float2 position, float distanceSquared, int capacity)
        {
            if (positions.Length == capacity && distanceSquared >= distances[capacity - 1])
            {
                return;
            }

            if (positions.Length < capacity)
            {
                positions.Add(position);
                distances.Add(distanceSquared);
            }
            else
            {
                positions[capacity - 1] = position;
                distances[capacity - 1] = distanceSquared;
            }

            // 마지막 원소를 제자리까지 앞으로 민다.
            for (int i = positions.Length - 1; i > 0 && distances[i] < distances[i - 1]; i--)
            {
                (positions[i], positions[i - 1]) = (positions[i - 1], positions[i]);
                (distances[i], distances[i - 1]) = (distances[i - 1], distances[i]);
            }
        }
    }
}
