using Unity.Entities;
using Unity.Rendering;

namespace Assets.MyAssets.Scripts.Runtime.Pooling
{
    /// <summary>
    /// 풀 엔티티를 켜고 끄는 메인 스레드용 헬퍼.
    /// <see cref="Active"/> 와 <c>MaterialMeshInfo</c> 를 항상 **함께** 토글해야 해서 한 곳에 모았다.
    /// 하나만 바꾸면 "안 보이는데 맞는 적" 이나 "보이는데 통과되는 적" 이 생긴다.
    /// 잡 안에서는 <c>EnabledRefRW</c> 두 개로 같은 일을 한다.
    /// </summary>
    public static class PoolUtility
    {
        public static void SetAlive(EntityManager entityManager, Entity entity, bool alive)
        {
            entityManager.SetComponentEnabled<Active>(entity, alive);

            // 렌더러가 없는 풀 엔티티도 허용한다 (예: 판정 전용 엔티티).
            if (entityManager.HasComponent<MaterialMeshInfo>(entity))
            {
                entityManager.SetComponentEnabled<MaterialMeshInfo>(entity, alive);
            }
        }
    }
}
