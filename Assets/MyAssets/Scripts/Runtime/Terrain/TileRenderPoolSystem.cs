using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 시작 시 타일 쿼드 풀을 만들고 각 쿼드에 창 안의 자리(<see cref="TileViewIndex"/>)를 배정한다.
    /// 자리는 이후 바뀌지 않는다 — <see cref="TileRenderPool"/> 주석 참조.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TerrainGridSystem))]
    public partial struct TileRenderPoolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileRenderPool>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            TileRenderPool pool = SystemAPI.GetSingleton<TileRenderPool>();
            int count = pool.ViewWidth * pool.ViewHeight;
            if (count <= 0 || pool.Prefab == Entity.Null)
            {
                return;
            }

            EntityManager entityManager = state.EntityManager;

            // 프리팹을 먼저 꺼두면 인스턴스가 꺼진 채로 나온다 (XpGemPoolSystem 과 같은 방식).
            // 켜진 채 나오면 렌더 시스템이 자리를 잡아주기 전 한 프레임 동안 전부 원점에 겹쳐 보인다.
            //
            // PoolUtility 를 쓰지 않는 이유:
            // 그 헬퍼는 Active 와 MaterialMeshInfo 를 **함께** 토글한다. 적·젬은 "보이는 것" 과
            // "게임플레이상 살아있는 것" 이 같아야 하지만, 타일 쿼드는 순수한 표시 장치다.
            // 지형의 진실은 TerrainGrid 배열에 있고 쿼드는 그걸 비출 뿐이라, 화면 밖 타일이
            // "죽었다" 는 개념 자체가 없다. Active 를 붙이면 없는 상태를 하나 만드는 셈이다.
            entityManager.SetComponentEnabled<MaterialMeshInfo>(pool.Prefab, false);

            NativeArray<Entity> instances = entityManager.Instantiate(pool.Prefab, count, Allocator.Temp);

            for (int i = 0; i < instances.Length; i++)
            {
                entityManager.SetComponentData(instances[i], new TileViewIndex { Value = i });
            }

            instances.Dispose();
        }
    }
}
