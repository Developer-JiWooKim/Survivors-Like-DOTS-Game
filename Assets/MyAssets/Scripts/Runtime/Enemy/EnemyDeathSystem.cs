using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 체력이 바닥난 적을 풀로 돌려보낸다 (기획서 8.2 의 17번). 파괴하지 않고 Active 만 끈다.
    /// XP 젬 드랍은 다음 작업에서 여기에 붙는다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageApplySystem))]
    public partial struct EnemyDeathSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Active 는 켜진 것만, MaterialMeshInfo 는 상태 무관 (ProjectileMoveSystem 주석 참조)
            _query = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Health>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new EnemyDeathJob().ScheduleParallel(_query);
        }
    }

    [BurstCompile]
    internal partial struct EnemyDeathJob : IJobEntity
    {
        private void Execute(in Health health, EnabledRefRW<Active> active, EnabledRefRW<MaterialMeshInfo> visible)
        {
            if (health.Current > 0f)
            {
                return;
            }

            active.ValueRW = false;
            visible.ValueRW = false;
        }
    }
}
