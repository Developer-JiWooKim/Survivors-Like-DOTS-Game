using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 시작 시 타일 그리드를 한 번 만들고 맵을 생성한다. 그리드의 소유자이기도 해서 해제도 여기서 한다.
    ///
    /// 왜 시스템이 소유하나 (SubScene 베이킹 데이터가 아니라):
    /// 512×512 = 1 MB 를 베이킹 결과로 들고 다니면 씬 파일이 그만큼 커지고, 맵 규칙을 바꿀 때마다
    /// 재베이킹이 필요하다. 생성 규칙 자체는 결정적이므로 런타임에 만드는 편이 싸고 시드만 바꾸면 된다.
    ///
    /// InitializationSystemGroup 인 이유:
    /// 게임플레이 시스템(<c>GameplaySystemGroup</c>) 은 RunState 게이트에 걸려 건너뛸 수 있다.
    /// 그리드는 게이트와 무관하게 반드시 한 번 만들어져야 한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TerrainGridSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TerrainSettings>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TerrainGrid>())
            {
                return;
            }

            TerrainGrid grid = SystemAPI.GetSingleton<TerrainGrid>();

            // 컨테이너가 컴포넌트 안에 있어 ECS 가 의존성을 추적하지 못한다. 해제 전에 직접 끝낸다.
            grid.WriteHandle.Complete();
            grid.ReadersHandle.Complete();

            if (grid.Tiles.IsCreated)
            {
                grid.Tiles.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 맵은 한 번만 만든다.
            state.Enabled = false;

            TerrainSettings settings = SystemAPI.GetSingleton<TerrainSettings>();

            int count = settings.Width * settings.Height;
            var tiles = new NativeArray<TileData>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            // 맵 중앙이 월드 원점(= 플레이어 시작 지점)에 오도록. 기획서에 원점 규정은 없지만
            // 스폰·리사이클이 전부 플레이어 기준 상대 좌표라 중앙 정렬이 가장 덜 놀랍다.
            var origin = new float2(-settings.Width, -settings.Height) * (TerrainGrid.TileSize * 0.5f);

            var generateJob = new GenerateMapJob
            {
                Tiles = tiles,
                Settings = settings,
            };

            // 262,144 칸을 메인 스레드에서 훑으면 로딩이 눈에 띄게 멈춘다. 칸끼리 의존이 없어 병렬이 자연스럽다.
            JobHandle handle = generateJob.Schedule(count, 1024, state.Dependency);

            var gridEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(gridEntity, new TerrainGrid
            {
                Tiles = tiles,
                Width = settings.Width,
                Height = settings.Height,
                Origin = origin,
                WriteHandle = handle,
                ReadersHandle = default,
            });

            state.Dependency = handle;
        }

        /// <summary>
        /// 타일 하나를 노이즈로 결정한다. 우선순위는 바위 &gt; 물 &gt; 풀 &gt; 흙 —
        /// 겹칠 때 "통과 불가" 가 이기게 해서 물 위에 바위가 떠 있는 모양이 나오지 않게 한다.
        ///
        /// 왜 채널마다 주파수·오프셋을 따로 주나:
        /// 같은 노이즈를 임계값만 달리해 쓰면 물 덩어리를 바위가 정확히 감싸는 동심원 패턴이 나온다.
        /// 오프셋을 벌려 서로 무관한 분포로 만든다.
        /// </summary>
        [BurstCompile]
        private struct GenerateMapJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<TileData> Tiles;
            public TerrainSettings Settings;

            public void Execute(int index)
            {
                var cell = new int2(index % Settings.Width, index / Settings.Width);

                // 중앙 기준 좌표. 시작 지점 반경 안을 비우는 판정과 노이즈 좌표를 같은 기준으로 둔다.
                float2 centered = (float2)cell - new float2(Settings.Width, Settings.Height) * 0.5f;
                bool nearSpawn = math.lengthsq(centered) < Settings.SpawnClearRadius * Settings.SpawnClearRadius;

                // 시드를 float 오프셋으로 흩뿌린다. 채널마다 다른 상수를 더해 분포를 독립시킨다.
                float seedOffset = Settings.Seed * 0.001f;

                float rock = noise.snoise(centered * Settings.RockFrequency + new float2(seedOffset + 131.7f, seedOffset - 57.3f));
                float water = noise.snoise(centered * Settings.WaterFrequency + new float2(seedOffset - 913.1f, seedOffset + 421.9f));
                float grass = noise.snoise(centered * Settings.GrassFrequency + new float2(seedOffset + 77.5f, seedOffset + 1109.3f));

                TileData tile;
                if (!nearSpawn && rock > Settings.RockThreshold)
                {
                    tile = TileData.Of(TileType.Rock);
                }
                else if (!nearSpawn && water > Settings.WaterThreshold)
                {
                    tile = TileData.Of(TileType.Water);
                }
                else if (grass > Settings.GrassThreshold)
                {
                    // 풀만 연료를 갖는다. 흙·물·바위는 타지 않으므로 0 (기획서 4.3).
                    tile = TileData.Of(TileType.Grass, Settings.GrassFuel);
                }
                else
                {
                    tile = TileData.Of(TileType.Dirt);
                }

                Tiles[index] = tile;
            }
        }
    }
}
