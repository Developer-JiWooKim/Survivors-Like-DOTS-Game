using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// 플레이어 체력이 바닥나면 런을 <see cref="RunPhase.GameOver"/> 로 바꾼다.
    /// 다음 프레임부터 <see cref="GameplaySystemGroup"/> 전체가 멈춘다. 게임오버 UI 는 M3 작업.
    ///
    /// 왜 그룹의 **맨 앞**에서 **지난 프레임** 체력을 보나:
    /// 피해 적용 직후에 메인 스레드에서 Health 를 읽으면 이번 프레임의 적 이동·충돌·적용 잡 체인이
    /// 전부 끝날 때까지 매 프레임 기다리게 된다. 프레임 시작 시점에는 그 잡들이 대부분 끝나 있어
    /// 대기가 거의 없다. 대가는 사망 판정 1 프레임 지연 — 체감 불가.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup), OrderFirst = true)]
    public partial struct PlayerDeathSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerMovement>();
            state.RequireForUpdate<RunState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (RefRO<Health> health in SystemAPI.Query<RefRO<Health>>().WithAll<PlayerMovement>())
            {
                if (health.ValueRO.Current > 0f)
                {
                    continue;
                }

                SystemAPI.GetSingletonRW<RunState>().ValueRW.Phase = RunPhase.GameOver;
                Debug.Log($"[Run] 플레이어 사망 — 게임오버. 경과 {(float)SystemAPI.Time.ElapsedTime}s");
                return;
            }
        }
    }
}
