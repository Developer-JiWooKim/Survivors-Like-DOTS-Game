using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.InputSystem;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step03
{
    public partial class StudyPlayerMoveSystem : SystemBase
    {
        protected override void OnCreate() { RequireForUpdate<PlayerTag>(); }
        protected override void OnUpdate()
        {
            float2 dir = float2.zero;

            Keyboard keyboard = Keyboard.current;
            if (keyboard is null)
            {
                return;
            }

            if (keyboard.wKey.isPressed)
            {
                dir += new float2(0f, 1f);
            }
            if (keyboard.sKey.isPressed)
            {
                dir += new float2(0f, -1f);
            }
            if (keyboard.aKey.isPressed)
            {
                dir += new float2(-1f, 0f);
            }
            if (keyboard.dKey.isPressed)
            {
                dir += new float2(1f, 0f);
            }

            float deltaTime = SystemAPI.Time.DeltaTime;
            dir = math.normalizesafe(dir);

            foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>().WithAll<PlayerTag>())
            {
                transform.ValueRW.Position.x += dir.x * speed.ValueRO.Speed * deltaTime;
                transform.ValueRW.Position.y += dir.y * speed.ValueRO.Speed * deltaTime;
            }
        }
    }
}
