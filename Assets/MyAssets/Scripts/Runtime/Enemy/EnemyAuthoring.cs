using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적 프리팹에 붙이는 Authoring. 베이킹 시 <see cref="EnemyMovement"/> 를 가진 엔티티가 된다.
    /// </summary>
    public sealed class EnemyAuthoring : MonoBehaviour
    {
        [Tooltip("초당 이동 거리 (월드 유닛). 기획서 추격형 기본값 2.5")]
        [SerializeField] private float _speed = 2.5f;

        private sealed class EnemyBaker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new EnemyMovement
                {
                    Speed = authoring._speed,
                });
            }
        }
    }
}
