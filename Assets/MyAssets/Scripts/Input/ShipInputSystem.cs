using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivorsLikeDOTS.Input
{
    /// <summary>
    /// 프로젝트 전역 Input Actions 의 <c>Ship</c> 맵을 읽어 <see cref="ShipInput"/> 싱글턴에 써 넣는다.
    /// 마우스 커서 잠금도 여기서 관리한다 — 조준 입력을 받을지 말지가 커서 상태에 달려 있어서다.
    ///
    /// 왜 ISystem 이 아니라 SystemBase 인가:
    /// InputActionAsset / InputAction 은 매니지드 클래스라 Burst 로 컴파일되는 ISystem 에서 다룰 수 없다.
    /// 이 시스템 하나만 매니지드고, 비행·조준 시스템은 전부 Burst 다.
    ///
    /// 왜 PlayerInput 컴포넌트(MonoBehaviour)를 쓰지 않나:
    /// GameObject 에 붙어 이벤트로 값을 주는 방식이라, 결국 ECS 로 옮기는 다리가 또 필요하다.
    /// 액션을 직접 폴링하면 다리가 이 시스템 하나로 끝난다.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class ShipInputSystem : SystemBase
    {
        private const string ShipMapName = "Ship";

        private InputAction _thrust;
        private InputAction _roll;
        private InputAction _aim;
        private InputAction _fire;
        private InputAction _boost;
        private InputAction _releaseCursor;

        protected override void OnCreate()
        {
            // 다른 시스템이 첫 프레임부터 안전하게 조회하도록 미리 만들어 둔다.
            EntityManager.CreateSingleton<ShipInput>();
        }

        protected override void OnStartRunning()
        {
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError(
                    "[ShipInputSystem] 프로젝트 전역 Input Actions 가 설정되지 않았습니다. " +
                    "Project Settings > Input System Package 를 확인하세요.");
                return;
            }

            InputActionMap map = actions.FindActionMap(ShipMapName);
            if (map == null)
            {
                Debug.LogError($"[ShipInputSystem] 액션 맵을 찾지 못했습니다: {ShipMapName}");
                return;
            }

            _thrust = map.FindAction("Thrust", throwIfNotFound: true);
            _roll = map.FindAction("Roll", throwIfNotFound: true);
            _aim = map.FindAction("Aim", throwIfNotFound: true);
            _fire = map.FindAction("Fire", throwIfNotFound: true);
            _boost = map.FindAction("Boost", throwIfNotFound: true);
            _releaseCursor = map.FindAction("ReleaseCursor", throwIfNotFound: true);
            map.Enable();

            LockCursor(true);
        }

        protected override void OnStopRunning()
        {
            LockCursor(false);
        }

        protected override void OnUpdate()
        {
            if (_thrust == null)
            {
                return;
            }

            // Esc 로 커서를 풀고, 풀린 상태에서 클릭하면 다시 잠근다.
            // 에디터에서 인스펙터를 만지려면 커서가 필요하고, 빌드에서도 창 전환 때 필요하다.
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (_releaseCursor.WasPressedThisFrame())
            {
                LockCursor(false);
                locked = false;
            }
            else if (!locked && _fire.WasPressedThisFrame())
            {
                LockCursor(true);
                // 다시 잠그려고 누른 클릭이 발사로 새지 않도록 이번 프레임은 입력을 버린다.
                SystemAPI.SetSingleton(default(ShipInput));
                return;
            }

            Vector3 rawThrust = _thrust.ReadValue<Vector3>();
            var thrust = new float3(rawThrust.x, rawThrust.y, rawThrust.z);

            // 대각선 입력(W+D+Space)이 축 입력보다 강해지지 않게 한다. 길이가 1 이하인 아날로그 입력은 그대로.
            if (math.lengthsq(thrust) > 1f)
            {
                thrust = math.normalize(thrust);
            }

            // 커서가 풀려 있을 때 마우스를 움직여도 조준이 돌면 안 된다.
            // 마우스 delta 는 이미 "이번 프레임 이동량" 이라 deltaTime 을 곱하지 않는다.
            Vector2 rawAim = locked ? _aim.ReadValue<Vector2>() : Vector2.zero;

            SystemAPI.SetSingleton(new ShipInput
            {
                Thrust = thrust,
                Roll = _roll.ReadValue<float>(),
                AimDelta = new float2(rawAim.x, rawAim.y),
                Fire = locked && _fire.IsPressed(),
                Boost = _boost.IsPressed(),
            });
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
