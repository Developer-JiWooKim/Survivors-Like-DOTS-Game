using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Benchmark
{
    /// <summary>
    /// 벤치마크 인스턴스를 1회 생성한다.
    ///
    /// 왜 1회만 도는가:
    /// 대량 Instantiate 는 구조적 변경이라 비싸다. 벤치마크의 관심사는 "생성 비용"이 아니라
    /// "생성된 뒤 매 프레임 렌더 비용"이므로, 스폰은 시작할 때 한 번만 하고 시스템을 꺼버린다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BenchmarkSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // SubScene 로딩이 끝나 BenchmarkSpawner 가 존재할 때까지 OnUpdate 를 돌리지 않는다.
            // 이게 없으면 씬이 로드되기 전 첫 프레임에 싱글턴 조회가 실패한다.
            state.RequireForUpdate<BenchmarkSpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 두 번 돌지 않게 먼저 꺼둔다. 아래에서 예외가 나더라도 무한 반복되지 않는다.
            state.Enabled = false;

            BenchmarkSpawner spawner = SystemAPI.GetSingleton<BenchmarkSpawner>();
            if (spawner.Count <= 0 || spawner.Prototype == Entity.Null)
            {
                return;
            }

            NativeArray<Entity> instances = state.EntityManager.Instantiate(
                spawner.Prototype, spawner.Count, Allocator.Temp);

            PlaceInGrid(ref state, instances, spawner.AreaSize);

            instances.Dispose();
        }

        /// <summary>
        /// 인스턴스를 정사각 격자로 배치한다.
        ///
        /// 격자로 놓는 이유: 무작위 배치는 겹침 때문에 실제로 그려지는 픽셀 수가 달라져
        /// 측정값이 실행마다 흔들린다. 격자는 결정적이라 before/after 비교가 가능하다.
        /// </summary>
        private static void PlaceInGrid(ref SystemState state, NativeArray<Entity> instances, float areaSize)
        {
            int count = instances.Length;
            int side = (int)math.ceil(math.sqrt(count));
            float step = side > 1 ? areaSize / (side - 1) : 0f;
            float origin = -areaSize * 0.5f;

            EntityManager entityManager = state.EntityManager;

            for (int i = 0; i < count; i++)
            {
                int gridX = i % side;
                int gridY = i / side;

                float3 position = new float3(
                    origin + (gridX * step),
                    origin + (gridY * step),
                    0f);

                entityManager.SetComponentData(instances[i], LocalTransform.FromPosition(position));
            }
        }
    }
}
