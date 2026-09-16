using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// SubScene 에 배치하는 플레이어. 베이킹 시 <see cref="PlayerMovement"/> 를 가진 엔티티가 된다.
    /// </summary>
    public sealed class PlayerAuthoring : MonoBehaviour
    {
        [Tooltip("초당 이동 거리 (월드 유닛). 기획서 기본값 5.0")]
        [SerializeField] private float _speed = 5f;

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
            }
        }
    }
}
