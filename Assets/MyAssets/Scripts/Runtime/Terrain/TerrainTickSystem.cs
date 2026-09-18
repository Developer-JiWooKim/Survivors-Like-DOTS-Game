using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 지형 상태를 10Hz 로 전진시킨다 (기획서 4.3). 지금은 연소 확산만 — 감전·동결은 다음 덩어리.
    ///
    /// 왜 프레임마다가 아니라 10Hz 인가 (기획서 4.3):
    /// 확산은 그리드를 통째로 훑는 작업이라 프레임당 돌리면 맵 크기에 비례해 비용이 붙는다.
    /// 그리고 불이 매 프레임 번지면 눈으로 따라갈 수 없을 만큼 빠르다 — 10Hz 는 성능과 가독성이 같은 방향인 드문 경우다.
    ///
    /// 왜 GameplaySystemGroup 인가:
    /// 확산은 게임플레이다. 레벨업으로 멈춘 동안 불이 계속 번지면 "멈춰 있는데 상황이 나빠지는" 상태가 된다.
    /// 렌더(<see cref="TileRenderSystem"/>)만 그룹 밖에 두어 정지 중에도 화면은 유지된다.
    ///
    /// **한 프레임에 틱을 여러 번 돌리지 않는다.** 프레임이 길어져 밀린 틱을 몰아서 처리하면
    /// 렉이 걸린 순간 불이 순간이동한 것처럼 번진다. 밀린 만큼은 버리고 다음 프레임에 한 번만 돈다
    /// (누적값은 유지해 평균 주기는 지켜진다).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct TerrainTickSystem : ISystem
    {
        private float _accumulated;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TerrainGrid>();
            state.RequireForUpdate<TerrainTickSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TerrainTickSettings settings = SystemAPI.GetSingleton<TerrainTickSettings>();
            if (settings.TickInterval <= 0f)
            {
                return;
            }

            _accumulated += SystemAPI.Time.DeltaTime;
            if (_accumulated < settings.TickInterval)
            {
                return;
            }

            // 밀린 틱은 버린다 (위 주석). 나머지를 남겨 평균 주기를 유지한다.
            _accumulated = math.min(_accumulated - settings.TickInterval, settings.TickInterval);

            Entity gridEntity = SystemAPI.GetSingletonEntity<TerrainGrid>();
            RefRW<TerrainGrid> grid = SystemAPI.GetComponentRW<TerrainGrid>(gridEntity);

            grid.ValueRW.Tick++;

            var job = new BurnSpreadJob
            {
                Read = grid.ValueRO.Tiles,
                Write = grid.ValueRO.Back,
                Width = grid.ValueRO.Width,
                Height = grid.ValueRO.Height,
                Tick = grid.ValueRO.Tick,
                Settings = settings,
            };

            // 지난 프레임에 그리드를 읽던 잡(렌더)이 남아 있으면 덮어쓰기 전에 끝나야 한다.
            // 컨테이너가 컴포넌트 안에 있어 ECS 의 자동 추적이 닿지 않으므로 직접 건다.
            JobHandle dependency = JobHandle.CombineDependencies(
                state.Dependency,
                grid.ValueRO.WriteHandle,
                grid.ValueRO.ReadersHandle);

            JobHandle handle = job.Schedule(grid.ValueRO.Tiles.Length, 1024, dependency);

            grid.ValueRW.Swap();
            grid.ValueRW.WriteHandle = handle;
            grid.ValueRW.ReadersHandle = default;
            state.Dependency = handle;
        }

        /// <summary>
        /// 칸마다 이웃 4 개를 읽어 다음 상태를 쓴다. 판단은 전부 <see cref="BurnRules"/> 가 하고
        /// 이 잡은 **그리드를 훑으며 부르는 껍데기**다 — 규칙을 EditMode 테스트로 검증하기 위한 분리.
        /// </summary>
        [BurstCompile]
        private struct BurnSpreadJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<TileData> Read;

            // 칸마다 자기 인덱스에만 쓴다. 겹치는 쓰기가 없어 안전 검사를 끄고 병렬로 돌린다.
            [NativeDisableParallelForRestriction] public NativeArray<TileData> Write;

            public int Width;
            public int Height;
            public uint Tick;
            public TerrainTickSettings Settings;

            public void Execute(int index)
            {
                TileData tile = Read[index];

                int x = index % Width;
                int y = index / Width;

                // 대각선은 세지 않는다 (기획서 4.3 — 4 방향). 맵 밖은 벽이라 불이 없다.
                int burningNeighbors =
                    CountBurning(x - 1, y) +
                    CountBurning(x + 1, y) +
                    CountBurning(x, y - 1) +
                    CountBurning(x, y + 1);

                float random = BurnRules.Random01(index, Tick);
                Write[index] = BurnRules.Step(tile, burningNeighbors, random, Settings);
            }

            private int CountBurning(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    return 0;
                }

                return Read[y * Width + x].HasState(TileStateFlags.Burning) ? 1 : 0;
            }
        }
    }
}
