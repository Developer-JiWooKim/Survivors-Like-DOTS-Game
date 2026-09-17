using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Experience;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 체력이 바닥난 적을 풀로 돌려보내고 XP 젬 드랍(+ 낮은 확률로 자석)을 요청한다 (기획서 8.2 의 17번). 파괴하지 않고 Active 만 끈다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(DamageApplySystem))]
    public partial struct EnemyDeathSystem : ISystem
    {
        private EntityQuery _query;
        private uint _frame;

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

            // 자석 설정이 없는 씬(벤치마크 등)에서도 돌도록 확률 0 으로 둔다.
            float magnetChance = SystemAPI.TryGetSingleton(out MagnetDropSettings magnet) ? magnet.DropChance : 0f;

            JobHandle handle = new EnemyDeathJob
            {
                Drops = drops.AsWriter(),
                MagnetChance = magnetChance,
                Seed = math.hash(new uint2(0x9E3779B9u, ++_frame)),
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
        public float MagnetChance;
        public uint Seed;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Drops.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Drops.EndForEachIndex();
        }

        private void Execute(
            Entity entity,
            in Health health,
            in LocalTransform transform,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
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
                Kind = XpDropKind.Gem,
                Value = XpGem.SmallValue,
            });

            if (MagnetChance <= 0f)
            {
                return;
            }

            // 워커 간에 Random 을 공유하면 레이스. 엔티티마다 결정적으로 만든다 (프레임 시드 × 엔티티 번호).
            var random = Random.CreateFromIndex(math.hash(new uint2(Seed, (uint)entity.Index)));
            if (random.NextFloat() < MagnetChance)
            {
                Drops.Write(new XpDrop
                {
                    Position = transform.Position.xy,
                    Kind = XpDropKind.Magnet,
                });
            }
        }
    }
}
