using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 투사체 프리팹에 붙이는 Authoring.
    /// 탄속·피해·수명은 무기 쪽(<see cref="ShardWeaponAuthoring"/>)이 발사 시점에 채운다.
    /// 같은 투사체 모양을 여러 무기가 다른 수치로 쓸 수 있게 하기 위함이다.
    /// </summary>
    public sealed class ProjectileAuthoring : MonoBehaviour
    {
        [Tooltip("충돌 판정 반경 (월드 유닛)")]
        [SerializeField] private float _hitRadius = 0.15f;

        private sealed class ProjectileBaker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new Projectile());
                AddComponent(entity, new HitRadius { Value = authoring._hitRadius });

                // 여기서는 켜진 채로 두고, 풀 생성 시스템이 인스턴스를 만든 직후 전부 끈다.
                // 렌더링 쪽 MaterialMeshInfo 는 Entities Graphics 의 Baker 가 붙이는 것이라
                // 이 Baker 에서 끌 수 없다. Active 와 렌더링을 끄는 곳을 한 군데로 모으려는 것.
                AddComponent<Active>(entity);
            }
        }
    }
}
