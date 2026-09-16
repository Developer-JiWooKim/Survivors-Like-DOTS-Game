using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.Scripts.Runtime.Input
{
    /// <summary>
    /// Input System 값을 읽어 <see cref="PlayerInputState"/> 싱글턴에 써 넣는다.
    ///
    /// 왜 ISystem 이 아니라 SystemBase 인가:
    /// Input System 의 InputActionAsset / InputAction 은 매니지드 클래스다.
    /// Burst 로 컴파일되는 ISystem 에서는 다룰 수 없어 SystemBase 를 쓴다 (CLAUDE.md 5장).
    /// 이 시스템 하나만 매니지드고, 나머지 게임플레이 시스템은 전부 Burst 다.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        /// <summary>프로젝트 전역 Input Actions 의 액션 경로.</summary>
        private const string MoveActionPath = "Player/Move";

        private InputAction _moveAction;

        protected override void OnCreate()
        {
            // 다른 시스템들이 첫 프레임부터 안전하게 조회할 수 있도록 미리 만들어 둔다.
            EntityManager.CreateSingleton<PlayerInputState>();
        }

        protected override void OnStartRunning()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError(
                    "[PlayerInputSystem] 프로젝트 전역 Input Actions 가 설정되지 않았습니다. " +
                    "Project Settings > Input System Package 를 확인하세요.");
                return;
            }

            _moveAction = actions.FindAction(MoveActionPath);
            if (_moveAction == null)
            {
                Debug.LogError($"[PlayerInputSystem] 액션을 찾지 못했습니다: {MoveActionPath}");
                return;
            }

            _moveAction.Enable();
        }

        protected override void OnUpdate()
        {
            float2 move = float2.zero;

            if (_moveAction != null)
            {
                Vector2 raw = _moveAction.ReadValue<Vector2>();
                move = new float2(raw.x, raw.y);

                // 대각선 입력이 축 입력보다 빨라지는 것을 막는다.
                // WASD 는 (1,1) 이 들어와 길이가 √2 가 되므로 정규화가 필요하다.
                // 게임패드 아날로그 스틱은 이미 길이가 1 이하라 그대로 둔다.
                float lengthSquared = math.lengthsq(move);
                if (lengthSquared > 1f)
                {
                    move = math.normalize(move);
                }
            }

            SystemAPI.SetSingleton(new PlayerInputState { Move = move });
        }
    }
}
