using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Experience;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 체력이 바닥난 적을 풀로 돌려보내고 XP 젬 드랍을 요청한다 (기획서 8.2 의 17번). 파괴하지 않고 Active 만 끈다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DamageApplySystem))]
    public partial struct EnemyDeathSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Active 는 켜진 것만, MaterialMeshInfo 는 상태 무관 (ProjectileMoveSystem 주석 참조)
            _query = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Health, LocalTransform>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_query);
            state.RequireForUpdate<XpDropBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var drops = new NativeStream(_query.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            JobHandle handle = new EnemyDeathJob
            {
                Drops = drops.AsWriter(),
            }.ScheduleParallel(_query, state.Dependency);

            // 스트림 해제는 XpGemSpawnSystem 의 몫이다.
            SystemAPI.GetSingletonRW<XpDropBus>().ValueRW.Register(drops, handle);
            state.Dependency = handle;
        }
    }

    [BurstCompile]
    internal partial struct EnemyDeathJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public NativeStream.Writer Drops;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Drops.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Drops.EndForEachIndex();
        }

        private void Execute(in Health health, in LocalTransform transform, EnabledRefRW<Active> active, EnabledRefRW<MaterialMeshInfo> visible)
        {
            if (health.Current > 0f)
            {
                return;
            }

            active.ValueRW = false;
            visible.ValueRW = false;

            // M1 은 적이 한 종류라 항상 소형 젬 하나. 적 타입별 드랍은 M4 에서 Blob 스탯으로 옮긴다.
            Drops.Write(new XpDrop
            {
                Position = transform.Position.xy,
                Value = XpGem.SmallValue,
            });
        }
    }
}
