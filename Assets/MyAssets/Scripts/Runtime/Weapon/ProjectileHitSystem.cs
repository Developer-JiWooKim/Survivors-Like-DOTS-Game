using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Enemy;
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

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 투사체와 적의 원-원 겹침을 검사해 <see cref="DamageEvent"/> 를 스트림에 쌓는다 (기획서 8.2 의 12번).
    ///
    /// M1 은 **전수 비교** (투사체 수 × 적 수). 투사체 수십 × 적 1,000 이면 프레임당 수만 번 비교라
    /// Burst 에서 무시할 수준이다. M2 에서 공간 해시가 들어오면 <see cref="ProjectileHitJob"/> 의
    /// 적 순회 루프만 해시 조회로 바꾼다. 스트림 → 적용 시스템 구조는 그대로 간다.
    /// 교체 전후를 같은 조건으로 측정해 비교하는 것이 M2 기록 대상이다.
    ///
    /// 관통은 없다 — 처음 겹친 적 하나만 맞히고 풀로 돌아간다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ProjectileMoveSystem))]
    public partial struct ProjectileHitSystem : ISystem
    {
        private EntityQuery _enemyQuery;
        private EntityQuery _projectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active, LocalTransform, HitRadius>()
                .Build();

            // 쿼리를 명시하는 이유는 ProjectileMoveSystem 과 같다.
            _projectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile, LocalTransform, HitRadius>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_projectileQuery);
            state.RequireForUpdate(_enemyQuery);
            state.RequireForUpdate<DamageEventBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // 적 데이터를 병렬 잡에서 읽을 수 있게 평평한 배열로 모은다.
            // Async 버전이라 적 이동 잡을 메인 스레드에서 기다리지 않는다.
            NativeList<Entity> enemyEntities = _enemyQuery.ToEntityListAsync(
                Allocator.TempJob, state.Dependency, out JobHandle entitiesHandle);
            NativeList<LocalTransform> enemyTransforms = _enemyQuery.ToComponentDataListAsync<LocalTransform>(
                Allocator.TempJob, state.Dependency, out JobHandle transformsHandle);
            NativeList<HitRadius> enemyRadii = _enemyQuery.ToComponentDataListAsync<HitRadius>(
                Allocator.TempJob, state.Dependency, out JobHandle radiiHandle);

            JobHandle gathered = JobHandle.CombineDependencies(entitiesHandle, transformsHandle, radiiHandle);

            // 청크 하나당 foreach 인덱스 하나. 필터 무시 개수여야 unfilteredChunkIndex 범위와 맞는다.
            var stream = new NativeStream(_projectileQuery.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            JobHandle hit = new ProjectileHitJob
            {
                EnemyEntities = enemyEntities.AsDeferredJobArray(),
                EnemyTransforms = enemyTransforms.AsDeferredJobArray(),
                EnemyRadii = enemyRadii.AsDeferredJobArray(),
                Writer = stream.AsWriter(),
            }.ScheduleParallel(_projectileQuery, gathered);

            // 스트림 해제는 적용 시스템(DamageApplySystem)의 몫이다. 여기서는 등록만 한다.
            SystemAPI.GetSingletonRW<DamageEventBus>().ValueRW.Register(stream, hit);

            state.Dependency = JobHandle.CombineDependencies(
                enemyEntities.Dispose(hit),
                enemyTransforms.Dispose(hit),
                enemyRadii.Dispose(hit));
        }
    }

    [BurstCompile]
    internal partial struct ProjectileHitJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        [ReadOnly] public NativeArray<Entity> EnemyEntities;
        [ReadOnly] public NativeArray<LocalTransform> EnemyTransforms;
        [ReadOnly] public NativeArray<HitRadius> EnemyRadii;

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
            float2 position = transform.Position.xy;

            for (int i = 0; i < EnemyTransforms.Length; i++)
            {
                float reach = radius.Value + EnemyRadii[i].Value;
                if (math.distancesq(position, EnemyTransforms[i].Position.xy) > reach * reach)
                {
                    continue;
                }

                Writer.Write(new DamageEvent
                {
                    Target = EnemyEntities[i],
                    Amount = projectile.Damage,
                });

                active.ValueRW = false;
                visible.ValueRW = false;
                return;
            }
        }
    }
}
