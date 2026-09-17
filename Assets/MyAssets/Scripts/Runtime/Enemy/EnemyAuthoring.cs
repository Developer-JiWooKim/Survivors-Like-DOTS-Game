using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적 프리팹에 붙이는 Authoring. 베이킹 시 이동·체력·판정 반경·활성 태그를 가진 엔티티가 된다.
    /// </summary>
    public sealed class EnemyAuthoring : MonoBehaviour
    {
        [Tooltip("초당 이동 거리 (월드 유닛). 기획서 추격형 기본값 2.5")]
        [SerializeField] private float _speed = 2.5f;

        [Tooltip("최대 체력. 기획서 추격형 기본값 10")]
        [SerializeField] private float _maxHealth = 10f;

        [Tooltip("충돌 판정 반경 (월드 유닛). 기획서 8.3 기본값 0.25")]
        [SerializeField] private float _hitRadius = 0.25f;

        [Tooltip("플레이어에 닿았을 때 1 회 피해. 이후 플레이어는 잠시 무적이 된다. 기획서에 없는 M1 임시값")]
        [SerializeField] private float _contactDamage = 10f;

        private sealed class EnemyBaker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new EnemyMovement
                {
                    Speed = authoring._speed,
                });

                AddComponent(entity, new Health
                {
                    Current = authoring._maxHealth,
                    Max = authoring._maxHealth,
                });

                AddComponent(entity, new HitRadius { Value = authoring._hitRadius });
                AddComponent(entity, new ContactDamage { Damage = authoring._contactDamage });

                // 켜진 채로 베이킹한다. 인스턴스는 이 상태를 물려받으므로 최초 스폰분은 바로 살아있다.
                AddComponent<Active>(entity);
            }
        }
    }
}
