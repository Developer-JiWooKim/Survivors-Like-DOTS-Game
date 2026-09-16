using Assets.MyAssets.Scripts.Runtime.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.CameraRig
{
    /// <summary>
    /// 카메라가 플레이어 엔티티를 따라간다.
    ///
    /// 왜 MonoBehaviour 인가:
    /// 카메라는 Unity 의 매니지드 오브젝트라 어차피 GameObject 로 존재해야 한다.
    /// 그리고 이건 **ECS 를 읽기만 하는 단방향** 이라 동기화 버그가 생길 여지가 없다.
    /// 플레이어를 엔티티로 둔 이유(양방향 동기화 회피)와 충돌하지 않는다.
    ///
    /// LateUpdate 인 이유:
    /// 시뮬레이션이 플레이어를 움직인 뒤에 카메라가 따라가야 한 프레임 밀리지 않는다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [Tooltip("따라가는 속도. 0 이면 즉시 붙는다.")]
        [SerializeField] private float _smoothing = 12f;

        [Tooltip("카메라의 Z 위치. 2D 직교 카메라는 대상보다 뒤에 있어야 한다.")]
        [SerializeField] private float _depth = -10f;

        private EntityQuery _playerQuery;
        private World _world;

        private void OnEnable()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            if (_world == null || !_world.IsCreated)
            {
                return;
            }

            _playerQuery = _world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerMovement>(),
                ComponentType.ReadOnly<LocalTransform>());
        }

        private void LateUpdate()
        {
            if (_world == null || !_world.IsCreated)
            {
                return;
            }

            // SubScene 로딩 전에는 플레이어가 없다. 조용히 넘어간다.
            if (_playerQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            LocalTransform playerTransform = _playerQuery.GetSingleton<LocalTransform>();
            var target = new Vector3(playerTransform.Position.x, playerTransform.Position.y, _depth);

            if (_smoothing <= 0f)
            {
                transform.position = target;
                return;
            }

            // 프레임률에 독립적인 지수 감쇠. Lerp 를 그냥 쓰면 FPS 에 따라 추적 속도가 달라진다.
            float t = 1f - Mathf.Exp(-_smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, target, t);
        }
    }
}
