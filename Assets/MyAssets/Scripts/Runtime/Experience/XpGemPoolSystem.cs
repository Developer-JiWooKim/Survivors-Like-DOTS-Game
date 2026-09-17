using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 시작 시 젬·자석 풀을 한 번에 만들고 전부 비활성으로 둔다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct XpGemPoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<XpGemPool>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            XpGemPool gems = SystemAPI.GetSingleton<XpGemPool>();
            CreatePool(ref state, gems.Prefab, gems.Capacity);

            if (SystemAPI.TryGetSingleton(out MagnetDropSettings magnets))
            {
                CreatePool(ref state, magnets.Prefab, magnets.PoolSize);
            }
        }

        private static void CreatePool(ref SystemState state, Entity prefab, int count)
        {
            if (count <= 0 || prefab == Entity.Null)
            {
                return;
            }

            // 인스턴스는 프리팹의 enabled 상태를 물려받는다. 프리팹을 먼저 끄고 복제하면 하나씩 끌 필요가 없다
            // (EnemySpawnSystem 과 같은 방식).
            EntityManager entityManager = state.EntityManager;
            PoolUtility.SetAlive(entityManager, prefab, false);

            NativeArray<Entity> instances = entityManager.Instantiate(prefab, count, Allocator.Temp);
            instances.Dispose();
        }
    }
}
