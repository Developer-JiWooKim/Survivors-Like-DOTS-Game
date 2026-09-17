using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적을 시작 시 한 번만 생성한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemySpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // SubScene 로딩이 끝나 EnemySpawner 가 생길 때까지 기다린다.
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 두 번 돌지 않게 먼저 꺼둔다.
            state.Enabled = false;

            EnemySpawner spawner = SystemAPI.GetSingleton<EnemySpawner>();
            if (spawner.Count <= 0 || spawner.Prefab == Entity.Null)
            {
                return;
            }

            NativeArray<Entity> instances = state.EntityManager.Instantiate(
                spawner.Prefab, spawner.Count, Allocator.Temp);

            PlaceInRing(ref state, instances, spawner);

            instances.Dispose();
        }

        /// <summary>
        /// 원점을 중심으로 한 도넛 모양 영역에 적을 흩뿌린다.
        /// </summary>
        private static void PlaceInRing(ref SystemState state, NativeArray<Entity> instances, in EnemySpawner spawner)
        {
            var random = Random.CreateFromIndex(spawner.RandomSeed);
            EntityManager entityManager = state.EntityManager;

            for (int i = 0; i < instances.Length; i++)
            {
                float2 point = RingSampler.Sample(ref random, spawner.MinRadius, spawner.MaxRadius);
                float3 position = new float3(point, 0f);

                // 프리팹에서 베이킹된 스케일·회전을 보존해야 하므로 위치만 갈아끼운다.
                // LocalTransform.FromPosition 을 쓰면 스케일이 1 로 초기화된다.
                LocalTransform transform = entityManager.GetComponentData<LocalTransform>(instances[i]);
                transform.Position = position;
                entityManager.SetComponentData(instances[i], transform);
            }
        }
    }
}
