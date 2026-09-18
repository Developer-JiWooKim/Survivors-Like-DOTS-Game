using Unity.Collections;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 연결된 물 덩어리를 찾는 flood fill (기획서 4.3 — "물 타일 하나가 감전되면 연결된 물 덩어리 전체가 같은 틱에").
    ///
    /// **메인 스레드 BFS** (2026-09-18 결정):
    /// 확산(연소)처럼 틱마다 한 칸씩 번지게 하면 병렬 잡으로 공짜지만, 기획서의 "같은 틱" 을 어긴다.
    /// 감전 지속이 0.5 초 = 5 틱이라 큰 웅덩이는 끝까지 번지기 전에 앞쪽이 꺼져 버리고,
    /// 전격의 "한 방에 터진다" 는 타격감이 사라진다.
    ///
    /// **비용이 덩어리 크기에 비례하는 문제**는 방문 칸 수 상한으로 막는다.
    /// 상한을 넘는 호수는 일부만 감전되는데, 이건 결함이 아니라 **밸런스 안전장치**다 —
    /// 맵 절반이 물인 구역에서 전격 하나로 화면을 청소하는 것을 막는다 (기획서 4.4 의 취지).
    ///
    /// 대각선은 잇지 않는다. 연소(4 방향)와 같은 연결성이어야 플레이어가 "이 물길은 이어져 있다" 를
    /// 한 가지 규칙으로 읽는다.
    /// </summary>
    public static class FloodFill
    {
        /// <summary>
        /// <paramref name="start"/> 에서 시작해 연결된 물 타일을 모은다.
        /// </summary>
        /// <param name="result">방문한 칸의 인덱스가 담긴다. 호출자가 비우고 넘긴다.</param>
        /// <param name="visited">칸마다 1 byte 의 방문 표시. 호출자가 재사용해 매번 할당하지 않도록 밖에서 받는다.</param>
        /// <param name="queue">BFS 큐. 같은 이유로 밖에서 받는다.</param>
        /// <param name="maxCells">방문 상한. 넘으면 거기서 멈춘다.</param>
        /// <returns>방문한 칸 수.</returns>
        public static int CollectWaterBody(
            in TerrainGrid grid,
            int2 start,
            ref NativeList<int> result,
            ref NativeArray<byte> visited,
            ref NativeList<int> queue,
            int maxCells)
        {
            result.Clear();
            queue.Clear();

            if (!grid.InBounds(start) || maxCells <= 0)
            {
                return 0;
            }

            int startIndex = grid.IndexOf(start);
            if (!TileTypes.IsWaterLike(grid.Tiles[startIndex].TypeValue))
            {
                return 0;
            }

            queue.Add(startIndex);
            visited[startIndex] = 1;

            // 큐를 앞에서부터 읽어 나간다. NativeQueue 대신 리스트 + 읽기 커서를 쓰는 이유는
            // 방문한 칸 목록이 곧 결과라 따로 모을 필요가 없어서다.
            for (int head = 0; head < queue.Length && result.Length < maxCells; head++)
            {
                int index = queue[head];
                result.Add(index);

                int x = index % grid.Width;
                int y = index / grid.Width;

                TryVisit(grid, x - 1, y, ref visited, ref queue);
                TryVisit(grid, x + 1, y, ref visited, ref queue);
                TryVisit(grid, x, y - 1, ref visited, ref queue);
                TryVisit(grid, x, y + 1, ref visited, ref queue);
            }

            // 다음 호출을 위해 방문 표시를 되돌린다. 배열 전체를 지우면 262,144 byte 를 매번 훑게 되므로
            // **건드린 칸만** 지운다 — 호수 하나가 작을 때 비용이 그 크기에 머문다.
            for (int i = 0; i < queue.Length; i++)
            {
                visited[queue[i]] = 0;
            }

            return result.Length;
        }

        private static void TryVisit(
            in TerrainGrid grid,
            int x,
            int y,
            ref NativeArray<byte> visited,
            ref NativeList<int> queue)
        {
            if (x < 0 || x >= grid.Width || y < 0 || y >= grid.Height)
            {
                return;
            }

            int index = y * grid.Width + x;
            if (visited[index] != 0)
            {
                return;
            }

            if (!TileTypes.IsWaterLike(grid.Tiles[index].TypeValue))
            {
                return;
            }

            visited[index] = 1;
            queue.Add(index);
        }
    }
}
