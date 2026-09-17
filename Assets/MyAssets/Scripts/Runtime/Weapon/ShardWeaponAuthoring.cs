using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 플레이어 GameObject 에 <c>PlayerAuthoring</c> 과 함께 붙인다.
    /// </summary>
    public sealed class ShardWeaponAuthoring : MonoBehaviour
    {
        [Tooltip("발사 간격 (초)")]
        [SerializeField] private float _interval = 0.5f;

        [Tooltip("탄속 (월드 유닛/초)")]
        [SerializeField] private float _projectileSpeed = 12f;

        [Tooltip("발당 피해. 추격형 체력 10 이면 2 발에 죽는다")]
        [SerializeField] private float _damage = 5f;

        [Tooltip("투사체 수명 (초). 탄속 × 수명 = 사거리")]
        [SerializeField] private float _projectileLifetime = 1.5f;

        private sealed class ShardWeaponBaker : Baker<ShardWeaponAuthoring>
        {
            public override void Bake(ShardWeaponAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ShardWeapon
                {
                    Interval = authoring._interval,
                    Cooldown = 0f,
                    ProjectileSpeed = authoring._projectileSpeed,
                    Damage = authoring._damage,
                    ProjectileLifetime = authoring._projectileLifetime,
                });
            }
        }
    }
}
