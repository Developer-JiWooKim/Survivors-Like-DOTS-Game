using Assets.MyAssets.Scripts.Runtime.Player;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// 플레이어 머리 위를 따라다니는 HP 바 (uGUI, Screen Space - Overlay 캔버스).
    ///
    /// 위치는 월드 좌표를 매 프레임 화면 좌표로 바꿔서 옮긴다.
    /// World Space 캔버스를 쓰지 않은 이유: 카메라 size 가 바뀌어도 바의 픽셀 크기가 일정하게 유지된다.
    ///
    /// 실행 순서를 CameraFollow 뒤로 미루는 이유: 카메라가 이번 프레임 위치로 옮겨진 뒤에 화면 좌표를 계산해야
    /// 바가 플레이어보다 한 프레임 늦게 따라오며 흔들리지 않는다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PlayerHealthBarView : MonoBehaviour
    {
        [Tooltip("위치를 옮길 바 전체 (배경 포함)")]
        [SerializeField] private RectTransform _bar;

        [Tooltip("채워지는 부분. 바의 자식으로 두고 Stretch(앵커 0~1, 오프셋 0)로 맞춘다")]
        [SerializeField] private RectTransform _fill;

        [Tooltip("월드 좌표를 화면 좌표로 바꿀 카메라. 비우면 Camera.main")]
        [SerializeField] private Camera _camera;

        [Tooltip("플레이어 중심에서 바까지의 월드 오프셋")]
        [SerializeField] private Vector2 _worldOffset = new Vector2(0f, 0.8f);

        private readonly SingletonAccess<PlayerStatusView> _status = new SingletonAccess<PlayerStatusView>();

        private World _world;
        private EntityQuery _playerQuery;

        private void LateUpdate()
        {
            if (!_status.TryRead(out PlayerStatusView view) || !view.HasPlayer)
            {
                SetVisible(false);
                return;
            }

            if (!TryGetPlayerPosition(out Vector3 worldPosition))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            UiBar.SetRatio(_fill, view.MaxHealth > 0f ? view.Health / view.MaxHealth : 0f);

            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam != null && _bar != null)
            {
                _bar.position = cam.WorldToScreenPoint(worldPosition + (Vector3)_worldOffset);
            }
        }

        /// <summary>
        /// 위치는 PlayerStatusView(1 프레임 늦음)가 아니라 이번 프레임 LocalTransform 을 쓴다.
        /// 카메라가 이번 프레임 위치를 따라가므로, 바만 늦으면 이동 중에 바가 뒤처져 보인다.
        /// CameraFollow 가 이미 같은 프레임에 LocalTransform 동기화를 했으므로 추가 대기는 없다.
        /// </summary>
        private bool TryGetPlayerPosition(out Vector3 position)
        {
            position = default;

            World world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            if (_world != world)
            {
                _world = world;
                _playerQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<PlayerMovement>(),
                    ComponentType.ReadOnly<LocalTransform>());
            }

            if (_playerQuery.IsEmptyIgnoreFilter)
            {
                return false;
            }

            world.EntityManager.CompleteDependencyBeforeRO<LocalTransform>();
            LocalTransform transform = _playerQuery.GetSingleton<LocalTransform>();
            position = transform.Position;
            return true;
        }

        private void SetVisible(bool visible)
        {
            if (_bar != null && _bar.gameObject.activeSelf != visible)
            {
                _bar.gameObject.SetActive(visible);
            }
        }
    }
}
