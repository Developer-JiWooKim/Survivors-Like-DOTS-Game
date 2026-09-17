using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Assets.MyAssets.Scripts.Runtime.Spatial;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 투사체와 적의 원-원 겹침을 검사해 <see cref="DamageEvent"/> 를 스트림에 쌓는다 (기획서 8.2 의 12번).
    ///
    /// 적은 <see cref="EnemySpatialHash"/> 에서 투사체 주변 셀만 조회한다.
    /// 이전(전수 비교)에는 매 프레임 적 전체의 Entity·위치·반경을 세 개의 리스트로 복사한 뒤
    /// 투사체마다 전부 훑었다 — 비교는 투사체 수 × 적 수, 복사는 적 수에 비례했다.
    /// 지금은 복사가 없고, 투사체 1 발당 주변 9 칸만 본다.
    ///
    /// 해시의 적 위치는 이번 프레임 **이동 전** 스냅샷이다. 적은 한 프레임에 0.04 u 정도 움직여
    /// 판정 반경(0.25 + 0.15)에 비해 무시할 수준이다.
    ///
    /// 관통은 없다 — 처음 겹친 적 하나만 맞히고 풀로 돌아간다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ProjectileMoveSystem))]
    public partial struct ProjectileHitSystem : ISystem
    {
        private EntityQuery _projectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // 쿼리를 명시하는 이유는 ProjectileMoveSystem 과 같다.
            _projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile, LocalTransform, HitRadius>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_projectileQuery);
            state.RequireForUpdate<EnemySpatialHash>();
            state.RequireForUpdate<DamageEventBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<EnemySpatialHash> hash = SystemAPI.GetSingletonRW<EnemySpatialHash>();

            // 청크 하나당 foreach 인덱스 하나. 필터 무시 개수여야 unfilteredChunkIndex 범위와 맞는다.
            var stream = new NativeStream(_projectileQuery.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            // 해시 재구축이 끝난 뒤에 돌아야 한다. 컨테이너가 컴포넌트 안에 있어 ECS 가 이 의존성을 모른다.
            JobHandle hit = new ProjectileHitJob
            {
                Enemies = hash.ValueRO.Map,
                Writer = stream.AsWriter(),
            }.ScheduleParallel(_projectileQuery,
                JobHandle.CombineDependencies(state.Dependency, hash.ValueRO.BuildHandle));

            hash.ValueRW.RegisterReader(hit);

            // 스트림 해제는 적용 시스템(DamageApplySystem)의 몫이다. 여기서는 등록만 한다.
            SystemAPI.GetSingletonRW<DamageEventBus>().ValueRW.Register(stream, hit);

            state.Dependency = hit;
        }
    }

    [BurstCompile]
    internal partial struct ProjectileHitJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, AgentRef> Enemies;

        public NativeStream.Writer Writer;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Writer.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Writer.EndForEachIndex();
        }

        private void Execute(
            in LocalTransform transform,
            in Projectile projectile,
            in HitRadius radius,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            if (!TryFindOverlap(transform.Position.xy, radius.Value, out Entity target))
            {
                return;
            }

            Writer.Write(new DamageEvent
            {
                Target = target,
                Amount = projectile.Damage,
            });

            active.ValueRW = false;
            visible.ValueRW = false;
        }

        private bool TryFindOverlap(float2 position, float radius, out Entity target)
        {
            int range = EnemySpatialHash.CellRangeFor(radius);
            int2 center = EnemySpatialHash.CellOf(position);

            for (int y = -range; y <= range; y++)
            {
                for (int x = -range; x <= range; x++)
                {
                    int key = EnemySpatialHash.KeyOf(center + new int2(x, y));
                    if (!Enemies.TryGetFirstValue(key, out AgentRef enemy, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        continue;
                    }

                    do
                    {
                        float reach = radius + enemy.Radius;
                        if (math.distancesq(position, enemy.Position) <= reach * reach)
                        {
                            target = enemy.Entity;
                            return true;
                        }
                    }
                    while (Enemies.TryGetNextValue(out enemy, ref iterator));
                }
            }

            target = Entity.Null;
            return false;
        }
    }
}
