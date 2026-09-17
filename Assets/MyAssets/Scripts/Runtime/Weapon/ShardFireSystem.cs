using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Assets.MyAssets.Scripts.Runtime.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 쿨다운이 끝나면 사거리 안의 최근접 적을 향해 풀에서 투사체 하나를 꺼내 발사한다 (기획서 8.2 의 9·10번을 합친 것).
    ///
    /// 최근접 탐색은 <see cref="EnemySpatialHash"/> 에서 플레이어 셀부터 **고리 모양으로 넓혀가며** 찾는다.
    /// 이미 찾은 적보다 가까울 수 없는 고리에 도달하면 멈추므로, 적이 가까이 있으면 몇 칸만 보고 끝난다.
    /// 사거리(탄속 × 수명) 밖은 보지 않는다 — 닿지도 않을 적에게 쏘지 않고, 탐색 비용의 상한도 된다.
    ///
    /// 왜 잡이 아니라 메인 스레드인가:
    /// 발사는 초당 몇 번뿐이다. 잡으로 옮기면 스케줄 비용이 더 크다.
    ///
    /// 왜 적 이동(EnemyChaseSystem)보다 **먼저** 도나:
    /// 발사할 때 LocalTransform 을 메인 스레드에서 읽고 쓴다. 적 이동 잡(LocalTransform 쓰기)이 예약된 뒤라면
    /// 그 잡이 끝날 때까지 기다려야 한다. 이동 전에 돌면 기다릴 것은 해시 재구축뿐이고, 그건 어차피 조회에 필요하다.
    /// 탐색 대상 위치도 해시 스냅샷(이동 전)이라 순서가 맞다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    [UpdateAfter(typeof(BuildEnemySpatialHashSystem))]
    [UpdateBefore(typeof(EnemyChaseSystem))]
    public partial struct ShardFireSystem : ISystem
    {
        private EntityQuery _freeProjectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Active 가 꺼진 투사체 = 풀에서 쉬고 있는 투사체
            _freeProjectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile>()
                .WithDisabled<Active>()
                .Build();

            state.RequireForUpdate<ShardWeapon>();
            state.RequireForUpdate<EnemySpatialHash>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<ShardWeapon>();
            RefRW<ShardWeapon> weapon = SystemAPI.GetComponentRW<ShardWeapon>(player);

            weapon.ValueRW.Cooldown -= SystemAPI.Time.DeltaTime;
            if (weapon.ValueRO.Cooldown > 0f)
            {
                return;
            }

            ShardWeapon stats = weapon.ValueRO;
            float3 origin = SystemAPI.GetComponent<LocalTransform>(player).Position;

            // 메인 스레드에서 해시를 읽으므로 이번 프레임 재구축 잡을 먼저 끝낸다.
            EnemySpatialHash hash = SystemAPI.GetSingleton<EnemySpatialHash>();
            hash.BuildHandle.Complete();

            float range = stats.ProjectileSpeed * stats.ProjectileLifetime;

            // 쏠 대상이나 남은 투사체가 없으면 쿨다운을 0 에 묶어 두고 다음 프레임에 다시 본다.
            // 음수로 계속 쌓이게 두면, 대상이 생기는 순간 밀린 발사가 한꺼번에 나가 버린다.
            if (!TryFindNearestEnemy(hash.Map, origin.xy, range, out float2 target) || _freeProjectileQuery.IsEmpty)
            {
                SystemAPI.GetComponentRW<ShardWeapon>(player).ValueRW.Cooldown = 0f;
                return;
            }

            Fire(ref state, origin, target, stats);

            // += 로 누적하는 이유: 프레임 경계에서 넘친 시간만큼 다음 발사를 당겨 평균 연사 속도를 정확히 유지한다.
            // Fire 가 EntityManager 를 거쳤으므로 RefRW 를 다시 받는다.
            SystemAPI.GetComponentRW<ShardWeapon>(player).ValueRW.Cooldown += stats.Interval;
        }

        private static bool TryFindNearestEnemy(
            in NativeParallelMultiHashMap<int, AgentRef> enemies, float2 origin, float range, out float2 nearest)
        {
            nearest = default;
            bool found = false;

            // 사거리 밖의 적은 처음부터 후보가 아니다.
            float bestDistanceSquared = range * range;

            int2 center = EnemySpatialHash.CellOf(origin);
            int maxRing = (int)math.ceil(range / EnemySpatialHash.CellSize);

            for (int ring = 0; ring <= maxRing; ring++)
            {
                // 고리 ring 의 셀 = 중심에서 체비쇼프 거리가 정확히 ring 인 칸들 (테두리만)
                for (int y = -ring; y <= ring; y++)
                {
                    bool edgeRow = y == -ring || y == ring;
                    int step = edgeRow ? 1 : ring * 2; // 가운데 행은 양 끝 두 칸만 본다

                    for (int x = -ring; x <= ring; x += step)
                    {
                        int key = EnemySpatialHash.KeyOf(center + new int2(x, y));
                        if (!enemies.TryGetFirstValue(key, out AgentRef enemy, out NativeParallelMultiHashMapIterator<int> iterator))
                        {
                            continue;
                        }

                        do
                        {
                            float distanceSquared = math.distancesq(origin, enemy.Position);
                            if (distanceSquared < bestDistanceSquared)
                            {
                                bestDistanceSquared = distanceSquared;
                                nearest = enemy.Position;
                                found = true;
                            }
                        }
                        while (enemies.TryGetNextValue(out enemy, ref iterator));
                    }
                }

                // 다음 고리(ring + 1)의 셀은 원점에서 최소 ring × 셀 크기만큼 떨어져 있다
                // (원점은 중심 셀 안 어딘가에 있으므로). 이미 그보다 가까운 적을 찾았으면 더 볼 필요가 없다.
                float nextRingMinDistance = ring * EnemySpatialHash.CellSize;
                if (found && bestDistanceSquared <= nextRingMinDistance * nextRingMinDistance)
                {
                    break;
                }
            }

            return found;
        }

        private void Fire(ref SystemState state, float3 origin, float2 target, in ShardWeapon weapon)
        {
            NativeArray<Entity> free = _freeProjectileQuery.ToEntityArray(Allocator.Temp);
            Entity projectile = free[0];
            free.Dispose();

            float2 delta = target - origin.xy;
            float2 direction = math.lengthsq(delta) > 1e-6f
                ? math.normalize(delta)
                : new float2(1f, 0f); // 적이 플레이어와 겹쳐 있으면 방향이 없다. 아무 쪽으로나 쏜다.

            EntityManager entityManager = state.EntityManager;

            // 프리팹의 스케일·Z 를 보존하고 XY 만 옮긴다.
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(projectile);
            transform.Position = new float3(origin.xy, transform.Position.z);
            entityManager.SetComponentData(projectile, transform);

            entityManager.SetComponentData(projectile, new Projectile
            {
                Velocity = direction * weapon.ProjectileSpeed,
                Damage = weapon.Damage,
                RemainingLifetime = weapon.ProjectileLifetime,
            });

            PoolUtility.SetAlive(entityManager, projectile, true);
        }
    }
}
