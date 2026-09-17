using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 시작 시 투사체를 풀 용량만큼 한 번에 만들고 전부 비활성으로 둔다.
    /// 런타임 중 투사체에 대한 구조적 변경은 이 한 번뿐이다 (기획서 8.4).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ProjectilePoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ProjectilePool>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            ProjectilePool pool = SystemAPI.GetSingleton<ProjectilePool>();
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
