using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// SubScene 에 배치하는 투사체 풀 설정.
    /// </summary>
    public sealed class ProjectilePoolAuthoring : MonoBehaviour
    {
        [Tooltip("ProjectileAuthoring 이 붙은 프리팹 에셋 (씬 안의 오브젝트 아님)")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("미리 만들어 둘 투사체 수. 동시에 날아다닐 수 있는 최대치다. 바닥나면 발사를 건너뛴다.")]
        [SerializeField] private int _capacity = 64;

        private sealed class ProjectilePoolBaker : Baker<ProjectilePoolAuthoring>
        {
            public override void Bake(ProjectilePoolAuthoring authoring)
            {
                if (authoring._prefab == null)
                {
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ProjectilePool
                {
                    Prefab = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic),
                    Capacity = authoring._capacity,
                });
            }
        }
    }
}
