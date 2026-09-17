using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 시작 시 젬을 풀 용량만큼 한 번에 만들고 전부 비활성으로 둔다.
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

            XpGemPool pool = SystemAPI.GetSingleton<XpGemPool>();
            if (pool.Capacity <= 0 || pool.Prefab == Entity.Null)
            {
                return;
            }

            EntityManager entityManager = state.EntityManager;
            NativeArray<Entity> instances = entityManager.Instantiate(pool.Prefab, pool.Capacity, Allocator.Temp);

            for (int i = 0; i < instances.Length; i++)
            {
                PoolUtility.SetAlive(entityManager, instances[i], false);
            }

            instances.Dispose();
        }
    }
}
