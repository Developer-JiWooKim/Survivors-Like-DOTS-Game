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

            // 맵 경계 (2026-09-18 결정 — 기획서 11 장 미결 #1): 512×512 유한 맵 + 벽.
            // 그리드가 아직 없을 수도 있어(지형 없이 돌리는 벤치마크 씬 등) 있을 때만 가둔다.
            bool hasGrid = SystemAPI.TryGetSingleton(out Terrain.TerrainGrid grid);
            if (hasGrid)
            {
                // 그리드를 메인 스레드에서 읽으므로 확산 틱 잡이 끝나 있어야 한다.
                // 컨테이너가 컴포넌트 안에 있어 ECS 가 이 의존성을 추적하지 못한다.
                // 플레이어는 1 명이라 여기서 기다리는 비용이 문제되지 않는다 (적은 잡 안에서 읽는다).
                grid.WriteHandle.Complete();
            }

            // 플레이어는 1개지만 Query 로 순회한다.
            // 싱글턴으로 강제하면 나중에 로컬 협동 같은 걸 넣을 때 구조를 바꿔야 한다.
            foreach ((RefRW<LocalTransform> transform, RefRW<PlayerPosition> position, RefRO<PlayerMovement> movement)
                     in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PlayerPosition>, RefRO<PlayerMovement>>())
            {
                float2 from = transform.ValueRO.Position.xy;
                float speed = movement.ValueRO.Speed;

                if (hasGrid)
                {
                    // 지형 속도 배수 (기획서 4.2). **밟고 선 칸** 기준이다 — 가려는 칸 기준으로 하면
                    // 빙판 가장자리에서 한 발짝 전에 미리 빨라져 경계가 어긋나 보인다.
                    speed *= Terrain.TerrainMovement.SpeedMultiplierAt(
                        grid.Tiles, grid.Width, grid.Height, grid.Origin, from, isPlayer: true);
                }

                float2 delta = input.Move * speed * deltaTime;
                float2 to = from + delta;

                if (hasGrid)
                {
                    // 바위 통과 불가 (기획서 4.2). 벽을 따라 미끄러진다 — 적과 같은 규칙을 쓴다.
                    to = Terrain.TerrainMovement.SlideAlongWalls(
                        grid.Tiles, grid.Width, grid.Height, grid.Origin, from, to);

                    // 맵 경계 (2026-09-18 결정). 반경은 플레이어 쿼드의 절반 —
                    // 기획서에 플레이어 충돌 반경 규정이 없어 M3 임시값 0.5 를 쓴다.
                    to = grid.ClampToBounds(to, 0.5f);
                }

                // 2D 게임이라 Z 는 건드리지 않는다.
                // Z 를 0 으로 덮어쓰면 나중에 레이어별 정렬용 오프셋을 못 쓰게 된다.
                transform.ValueRW.Position = new float3(to.x, to.y, transform.ValueRO.Position.z);

                // 잡들이 기다림 없이 읽을 수 있는 사본 (PlayerPosition 주석 참조)
                position.ValueRW.Value = transform.ValueRO.Position.xy;
            }
        }
    }
}
