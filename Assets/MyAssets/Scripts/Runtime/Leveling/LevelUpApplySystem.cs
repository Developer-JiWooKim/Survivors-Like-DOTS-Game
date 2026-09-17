using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Run;
using Assets.MyAssets.Scripts.Runtime.Weapon;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Leveling
{
    /// <summary>
    /// UI 가 고른 강화를 플레이어에 적용하고 런을 재개한다.
    ///
    /// 왜 GameplaySystemGroup **밖**인가:
    /// 선택을 기다리는 동안 런은 Paused 라 게임플레이 그룹이 통째로 멈춰 있다.
    /// 재개시키는 시스템이 그 안에 있으면 영원히 돌지 않는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(GameplaySystemGroup))]
    public partial struct LevelUpApplySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LevelUpState>();
            state.RequireForUpdate<RunState>();
            state.RequireForUpdate<PlayerExperience>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<LevelUpState> levelUp = SystemAPI.GetSingletonRW<LevelUpState>();
            if (!levelUp.ValueRO.IsOpen || levelUp.ValueRO.SelectedIndex == LevelUpState.NoSelection)
            {
                return;
            }

            UpgradeType chosen = levelUp.ValueRO.GetOption(levelUp.ValueRO.SelectedIndex);
            Entity player = SystemAPI.GetSingletonEntity<PlayerExperience>();
            Apply(ref state, player, chosen);

            levelUp.ValueRW.IsOpen = false;
            levelUp.ValueRW.SelectedIndex = LevelUpState.NoSelection;

            SystemAPI.GetSingletonRW<RunState>().ValueRW.Phase = RunPhase.Playing;
        }

        // SystemAPI 는 static 메서드에서 쓸 수 없다 (소스 생성기가 시스템 인스턴스의 타입 핸들을 참조함, EA0006).
        private void Apply(ref SystemState state, Entity player, UpgradeType upgrade)
        {
            switch (upgrade)
            {
                case UpgradeType.ShardDamage:
                {
                    RefRW<ShardWeapon> weapon = SystemAPI.GetComponentRW<ShardWeapon>(player);
                    weapon.ValueRW.Damage *= UpgradeTable.ShardDamageMultiplier;
                    break;
                }
                case UpgradeType.ShardFireRate:
                {
                    RefRW<ShardWeapon> weapon = SystemAPI.GetComponentRW<ShardWeapon>(player);
                    weapon.ValueRW.Interval *= UpgradeTable.ShardIntervalMultiplier;
                    break;
                }
                case UpgradeType.ShardProjectileSpeed:
                {
                    RefRW<ShardWeapon> weapon = SystemAPI.GetComponentRW<ShardWeapon>(player);
                    weapon.ValueRW.ProjectileSpeed *= UpgradeTable.ShardSpeedMultiplier;
                    break;
                }
                case UpgradeType.MoveSpeed:
                {
                    RefRW<PlayerMovement> movement = SystemAPI.GetComponentRW<PlayerMovement>(player);
                    movement.ValueRW.Speed *= UpgradeTable.MoveSpeedMultiplier;
                    break;
                }
                case UpgradeType.MaxHealth:
                {
                    RefRW<Health> health = SystemAPI.GetComponentRW<Health>(player);
                    health.ValueRW.Max += UpgradeTable.MaxHealthBonus;
                    health.ValueRW.Current = math.min(health.ValueRO.Current + UpgradeTable.MaxHealthBonus, health.ValueRO.Max);
                    break;
                }
            }
        }
    }
}
