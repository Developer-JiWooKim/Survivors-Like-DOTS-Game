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
        [SerializeField] private float _interval = 0.15f;

        [Tooltip("탄속 (월드 유닛/초)")]
        [SerializeField] private float _projectileSpeed = 12f;

        [Tooltip("발당 피해. 추격형 체력 10 이면 2 발에 죽는다")]
        [SerializeField] private float _damage = 5f;

        [Tooltip("투사체 수명 (초). 탄속 × 수명 = 사거리")]
        [SerializeField] private float _projectileLifetime = 1.5f;

        [Header("발사 형태")]
        [Tooltip("한 번에 노리는 적 수")]
        [SerializeField] private int _targetCount = 3;

        [Tooltip("타겟 하나당 발 수")]
        [SerializeField] private int _projectilesPerTarget = 1;

        [Tooltip("타겟당 발들이 퍼지는 전체 각도 (도)")]
        [SerializeField] private float _spreadDegrees = 15f;

        [Header("관통 · 폭발 (레벨업으로 강화)")]
        [Tooltip("추가로 뚫고 지나갈 적 수. 0 = 관통 없음")]
        [SerializeField] private int _pierce;

        [Tooltip("명중 시 폭발 반경. 0 = 폭발 없음")]
        [SerializeField] private float _explosionRadius;

        [Tooltip("폭발 피해 = 발당 피해 × 이 비율")]
        [SerializeField] private float _explosionDamageRatio = 0.5f;

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
                    TargetCount = Mathf.Max(1, authoring._targetCount),
                    ProjectilesPerTarget = Mathf.Max(1, authoring._projectilesPerTarget),
                    SpreadDegrees = authoring._spreadDegrees,
                    Pierce = Mathf.Max(0, authoring._pierce),
                    ExplosionRadius = Mathf.Max(0f, authoring._explosionRadius),
                    ExplosionDamageRatio = authoring._explosionDamageRatio,
                });
            }
        }
    }
}
