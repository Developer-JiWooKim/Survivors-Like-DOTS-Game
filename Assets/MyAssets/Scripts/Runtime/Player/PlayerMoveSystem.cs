using Assets.MyAssets.Scripts.Runtime.Input;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// 입력에 따라 플레이어를 이동시킨다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct PlayerMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 입력 싱글턴과 플레이어가 모두 준비되기 전에는 돌지 않는다.
            // PlayerInputState 는 PlayerInputSystem.OnCreate 에서,
            // PlayerMovement 는 SubScene 로딩 후에 생긴다.
            state.RequireForUpdate<PlayerInputState>();
            state.RequireForUpdate<PlayerMovement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PlayerInputState input = SystemAPI.GetSingleton<PlayerInputState>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            // 플레이어는 1개지만 Query 로 순회한다.
            // 싱글턴으로 강제하면 나중에 로컬 협동 같은 걸 넣을 때 구조를 바꿔야 한다.
            foreach ((RefRW<LocalTransform> transform, RefRW<PlayerPosition> position, RefRO<PlayerMovement> movement)
                     in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerPosition>, RefRO<PlayerMovement>>())
            {
                float2 delta = input.Move * movement.ValueRO.Speed * deltaTime;

                // 2D 게임이라 Z 는 건드리지 않는다.
                // Z 를 0 으로 덮어쓰면 나중에 레이어별 정렬용 오프셋을 못 쓰게 된다.
                transform.ValueRW.Position += new float3(delta.x, delta.y, 0f);

                // 잡들이 기다림 없이 읽을 수 있는 사본 (PlayerPosition 주석 참조)
                position.ValueRW.Value = transform.ValueRO.Position.xy;
            }
        }
    }
}
