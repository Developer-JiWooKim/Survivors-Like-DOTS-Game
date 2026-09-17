using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 시작 시 적 풀을 한 번에 만든다. 전부 **꺼진 채**로 두고, 켜는 일은 <see cref="EnemySpawnDirectorSystem"/> 이 한다.
    /// 런타임 중 적에 대한 구조적 변경은 이 한 번뿐이다 (기획서 6.2, 8.4).
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
            if (spawner.PoolSize <= 0 || spawner.Prefab == Entity.Null)
            {
                return;
            }

            EntityManager entityManager = state.EntityManager;

            // 인스턴스는 프리팹의 enabled 상태를 그대로 물려받는다.
            // 2 만 개를 하나씩 끄는 대신 **프리팹을 먼저 꺼 두고** 복제한다.
            // 프리팹 엔티티는 어차피 모든 쿼리에서 제외되고 그려지지도 않으므로 꺼도 부작용이 없다.
            PoolUtility.SetAlive(entityManager, spawner.Prefab, false);

            NativeArray<Entity> instances = entityManager.Instantiate(spawner.Prefab, spawner.PoolSize, Allocator.Temp);
            instances.Dispose();
        }
    }
}
