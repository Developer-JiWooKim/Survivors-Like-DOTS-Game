using Assets.MyAssets.Scripts.Runtime.Combat;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// SubScene 에 배치하는 플레이어. 베이킹 시 이동·체력·판정 반경을 가진 엔티티가 된다.
    /// </summary>
    public sealed class PlayerAuthoring : MonoBehaviour
    {
        [Tooltip("초당 이동 거리 (월드 유닛). 기획서 기본값 5.0")]
        [SerializeField] private float _speed = 5f;

        [Tooltip("최대 체력. 기획서에 없는 M1 임시값")]
        [SerializeField] private float _maxHealth = 100f;

        [Tooltip("피격 판정 반경 (월드 유닛). 기획서에 없는 M1 임시값")]
        [SerializeField] private float _hitRadius = 0.4f;

        private sealed class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                // Dynamic: 위치가 매 프레임 바뀌므로 LocalTransform 이 필요하다.
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerMovement
                {
                    Speed = authoring._speed,
                });

                // 적과 같은 Health 를 쓴다. 그래야 DamageApplySystem 이 대상 종류를 몰라도 된다.
                AddComponent(entity, new Health
                {
                    Current = authoring._maxHealth,
                    Max = authoring._maxHealth,
                });

                AddComponent(entity, new HitRadius { Value = authoring._hitRadius });
            }
        }
    }
}
