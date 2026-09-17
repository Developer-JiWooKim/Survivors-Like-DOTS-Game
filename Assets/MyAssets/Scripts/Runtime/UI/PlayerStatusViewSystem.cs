using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Leveling;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// 프레임 시작에 플레이어 체력·경험치를 <see cref="PlayerStatusView"/> 로 복사한다.
    ///
    /// GameplaySystemGroup 밖에 두는 이유: 레벨업 일시정지 중에도 갱신돼야 한다
    /// (최대 체력 강화를 고르면 HP 바가 바로 바뀌어야 함).
    /// 프레임 시작이라 지난 프레임 잡들은 대부분 끝나 있어, 여기서 Health 를 읽는 대기는 거의 없다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LevelUpApplySystem))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    public partial struct PlayerStatusViewSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new PlayerStatusView());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var view = new PlayerStatusView();

            foreach ((RefRO<Health> health, RefRO<PlayerExperience> experience)
                     in SystemAPI.Query<RefRO<Health>, RefRO<PlayerExperience>>())
            {
                view.HasPlayer = true;
                view.Health = health.ValueRO.Current;
                view.MaxHealth = health.ValueRO.Max;
                view.Level = experience.ValueRO.Level;
                view.Xp = experience.ValueRO.Xp;
                view.XpToNext = experience.ValueRO.XpToNext;
                break;
            }

            SystemAPI.SetComponent(state.SystemHandle, view);
        }
    }
}
