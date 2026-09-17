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
    /// 관통: 한 프레임에 **아직 맞히지 않은** 적 하나만 맞힌다. 관통 횟수가 남아 있으면 계속 날아가고,
    /// 맞힌 적은 <see cref="Projectile.HitHistory"/> 에 남겨 겹쳐 있는 동안 다시 때리지 않는다.
    /// 한 프레임에 한 마리로 제한한 이유: 탄이 한 프레임에 이동하는 거리(약 0.2 u)가 적 지름(0.5)보다 작아
    /// 붙어 있는 적들도 다음 프레임에 차례로 맞는다. 루프를 단순하게 유지하는 쪽을 택했다.
    ///
    /// 폭발: 명중 지점 반경 안의 **다른** 적들에게 폭발 피해를 준다. 직격 대상은 직격 피해만 받는다.
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
                .WithAll<LocalTransform, HitRadius>()
                .WithAllRW<Projectile>() // 관통 횟수·명중 기록을 갱신한다
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
            ref Projectile projectile,
            in HitRadius radius,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            float2 position = transform.Position.xy;
            if (!SpatialQueries.TryFindOverlap(Enemies, position, radius.Value, ref projectile.HitHistory, out AgentRef target))
            {
                return;
            }

            Writer.Write(new DamageEvent
            {
                Target = target.Entity,
                Amount = projectile.Damage,
            });

            if (projectile.ExplosionRadius > 0f)
            {
                // 폭발 중심은 탄 위치가 아니라 맞은 적의 위치 — 적 중심에서 퍼지는 게 눈으로 보기에 자연스럽다.
                Explode(target.Position, projectile.ExplosionRadius, projectile.ExplosionDamage, target.Entity);
            }

            if (projectile.PierceRemaining <= 0)
            {
                active.ValueRW = false;
                visible.ValueRW = false;
                return;
            }

            projectile.PierceRemaining--;

            // 목록이 가득 차면(관통 상한을 넘게 설정한 경우) 가장 오래된 기록을 버린다.
            // 오래된 적은 이미 멀리 지나쳤을 가능성이 높다.
            if (projectile.HitHistory.Length == projectile.HitHistory.Capacity)
            {
                projectile.HitHistory.RemoveAt(0);
            }
            projectile.HitHistory.Add(target.Entity);
        }

        private void Explode(float2 center, float explosionRadius, float damage, Entity directHit)
        {
            int range = EnemySpatialHash.CellRangeFor(explosionRadius);
            int2 centerCell = EnemySpatialHash.CellOf(center);

            for (int y = -range; y <= range; y++)
            {
                for (int x = -range; x <= range; x++)
                {
                    int2 cell = centerCell + new int2(x, y);
                    if (!Enemies.TryGetFirstValue(EnemySpatialHash.KeyOf(cell), out AgentRef enemy, out NativeParallelMultiHashMapIterator<int> iterator))
                    {
                        continue;
                    }

                    do
                    {
                        // 키 충돌로 섞여 든 다른 셀의 적을 거른다 — 안 그러면 한 적이 폭발 피해를 두 번 받는다.
                        if (enemy.Entity == directHit || !EnemySpatialHash.IsInCell(enemy, cell))
                        {
                            continue;
                        }

                        float reach = explosionRadius + enemy.Radius;
                        if (math.distancesq(center, enemy.Position) <= reach * reach)
                        {
                            Writer.Write(new DamageEvent
                            {
                                Target = enemy.Entity,
                                Amount = damage,
                            });
                        }
                    }
                    while (Enemies.TryGetNextValue(out enemy, ref iterator));
                }
            }
        }
    }
}
