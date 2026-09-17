using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 쿨다운이 끝나면 최근접 적을 향해 풀에서 투사체 하나를 꺼내 발사한다 (기획서 8.2 의 9·10번을 합친 것).
    ///
    /// 왜 잡이 아니라 메인 스레드인가:
    /// 발사는 초당 몇 번뿐이고, 최근접 탐색도 발사하는 프레임에만 적 1,000 마리를 한 번 훑는다.
    /// 잡으로 옮기면 스케줄 비용이 더 크다. 대신 발사 프레임에는 적 이동 잡 완료를 기다리는
    /// 동기화 지점이 생긴다 — 무기가 늘어 이게 프로파일러에 보이면 그때 잡으로 옮긴다.
    ///
    /// 최근접 탐색은 전수 비교 O(N) 이다. 공간 해시(M2)가 들어오면 이 조회만 교체한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(EnemyChaseSystem))]
    public partial struct ShardFireSystem : ISystem
    {
        private EntityQuery _enemyQuery;
        private EntityQuery _freeProjectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active, LocalTransform>()
                .Build();

            // Active 가 꺼진 투사체 = 풀에서 쉬고 있는 투사체
            _freeProjectileQuery = SystemAPI.QueryBuilder()
                .WithAll<Projectile>()
                .WithDisabled<Active>()
                .Build();

            state.RequireForUpdate<ShardWeapon>();
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

            float3 origin = SystemAPI.GetComponent<LocalTransform>(player).Position;

            // 쏠 대상이나 남은 투사체가 없으면 쿨다운을 0 에 묶어 두고 다음 프레임에 다시 본다.
            // 음수로 계속 쌓이게 두면, 대상이 생기는 순간 밀린 발사가 한꺼번에 나가 버린다.
            if (!TryFindNearestEnemy(origin, out float3 target) || _freeProjectileQuery.IsEmpty)
            {
                weapon.ValueRW.Cooldown = 0f;
                return;
            }

            ShardWeapon stats = weapon.ValueRO;
            Fire(ref state, origin, target, stats);

            // += 로 누적하는 이유: 프레임 경계에서 넘친 시간만큼 다음 발사를 당겨 평균 연사 속도를 정확히 유지한다.
            // Fire 가 EntityManager 를 거쳤으므로 RefRW 를 다시 받는다.
            SystemAPI.GetComponentRW<ShardWeapon>(player).ValueRW.Cooldown += stats.Interval;
        }

        private bool TryFindNearestEnemy(float3 origin, out float3 nearest)
        {
            nearest = default;

            NativeArray<LocalTransform> enemies = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            float bestDistanceSquared = float.MaxValue;
            bool found = false;

            for (int i = 0; i < enemies.Length; i++)
            {
                float distanceSquared = math.distancesq(enemies[i].Position.xy, origin.xy);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    nearest = enemies[i].Position;
                    found = true;
                }
            }

            enemies.Dispose();
            return found;
        }

        private void Fire(ref SystemState state, float3 origin, float3 target, in ShardWeapon weapon)
        {
            NativeArray<Entity> free = _freeProjectileQuery.ToEntityArray(Allocator.Temp);
            Entity projectile = free[0];
            free.Dispose();

            float2 delta = target.xy - origin.xy;
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
