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
    /// 쿨다운이 끝나면 사거리 안의 가까운 적들을 향해 풀에서 투사체를 꺼내 발사한다 (기획서 8.2 의 9·10번을 합친 것).
    ///
    /// 한 번 발사 = 가까운 적 T 마리 × 타겟당 P 발 (부채꼴). <see cref="ShardWeapon"/> 참조.
    ///
    /// 타겟 탐색은 <see cref="EnemySpatialHash"/> 에서 플레이어 셀부터 **고리 모양으로 넓혀가며** 가까운 T 마리를 모은다.
    /// T 번째로 가까운 적보다 가까울 수 없는 고리에 도달하면 멈춘다. 사거리(탄속 × 수명) 밖은 보지 않는다.
    ///
    /// 한 프레임에 여러 번 발사할 수 있다: 발사 간격이 프레임 시간보다 짧아지면(연사 강화)
    /// 한 번만 쏘는 구조로는 연사 속도가 FPS 에 묶인다. 대신 한 프레임 최대 횟수를 둬서
    /// 긴 프레임(로딩 등) 뒤에 수십 번 몰아 쏘는 일을 막는다.
    ///
    /// 왜 잡이 아니라 메인 스레드인가:
    /// 발사는 초당 수 회 ~ 수십 회, 회당 수~수십 발이다. 풀에서 꺼내는 작업(EntityManager)이 메인 스레드 전용이기도 하다.
    /// 강화가 쌓여 이 시스템이 프로파일러에 보이면 배치 방식을 잡으로 옮긴다.
    ///
    /// 왜 적 이동(EnemyChaseSystem)보다 **먼저** 도나:
    /// 발사할 때 LocalTransform 을 메인 스레드에서 쓴다. 적 이동 잡(LocalTransform 쓰기)이 예약된 뒤라면
    /// 그 잡이 끝날 때까지 기다려야 한다. 이동 전에 돌면 기다릴 것은 해시 재구축뿐이다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    [UpdateAfter(typeof(BuildEnemySpatialHashSystem))]
    [UpdateBefore(typeof(EnemyChaseSystem))]
    public partial struct ShardFireSystem : ISystem
    {
        /// <summary>한 프레임에 발사할 수 있는 최대 횟수.</summary>
        private const int MaxVolleysPerFrame = 4;

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
            ShardWeapon weapon = SystemAPI.GetComponent<ShardWeapon>(player);

            weapon.Cooldown -= SystemAPI.Time.DeltaTime;
            if (weapon.Cooldown > 0f)
            {
                SystemAPI.SetComponent(player, weapon);
                return;
            }

            float2 origin = SystemAPI.GetComponent<LocalTransform>(player).Position.xy;

            // 메인 스레드에서 해시를 읽으므로 이번 프레임 재구축 잡을 먼저 끝낸다.
            EnemySpatialHash hash = SystemAPI.GetSingleton<EnemySpatialHash>();
            hash.BuildHandle.Complete();

            float range = weapon.ProjectileSpeed * weapon.ProjectileLifetime;
            // 결과 목록 용량(SpatialQueries.MaxNearestCount = 16)이 상한. 레벨업 최대 12 보다 크다.
            int targetCount = math.min(weapon.TargetCount, SpatialQueries.MaxNearestCount);

            // 한 프레임에 여러 번 쏘더라도 대상은 같으므로 한 번만 찾는다.
            var targets = new FixedList512Bytes<float2>();
            SpatialQueries.FindNearest(hash.Map, origin, range, targetCount, ref targets);

            NativeArray<Entity> free = _freeProjectileQuery.ToEntityArray(Allocator.Temp);
            int freeCursor = 0;

            int volleys = 0;
            while (weapon.Cooldown <= 0f && volleys < MaxVolleysPerFrame)
            {
                // 쏠 대상이 없으면 쿨다운을 0 에 묶어 두고 다음 프레임에 다시 본다.
                // 음수로 계속 쌓이게 두면, 대상이 생기는 순간 밀린 발사가 한꺼번에 나가 버린다.
                if (targets.Length == 0)
                {
                    weapon.Cooldown = 0f;
                    break;
                }

                FireVolley(ref state, origin, targets, targetCount, weapon, free, ref freeCursor);

                // += 로 누적하는 이유: 프레임 경계에서 넘친 시간만큼 다음 발사를 당겨 평균 연사 속도를 정확히 유지한다.
                weapon.Cooldown += weapon.Interval;
                volleys++;
            }

            // 상한에 걸려 밀린 발사는 버린다. 남겨두면 다음 프레임에도 계속 상한까지 몰아 쏜다.
            weapon.Cooldown = math.max(weapon.Cooldown, 0f);

            free.Dispose();
            SystemAPI.SetComponent(player, weapon);
        }

        private void FireVolley(
            ref SystemState state, float2 origin, in FixedList512Bytes<float2> targets, int targetCount,
            in ShardWeapon weapon, NativeArray<Entity> free, ref int freeCursor)
        {
            int perTarget = math.max(weapon.ProjectilesPerTarget, 1);
            float spread = math.radians(weapon.SpreadDegrees);

            for (int t = 0; t < targetCount; t++)
            {
                // 사거리 안 적이 타겟 수보다 적으면 가까운 적부터 다시 노린다.
                float2 target = targets[t % targets.Length];

                float2 delta = target - origin;
                float2 aim = math.lengthsq(delta) > 1e-6f
                    ? math.normalize(delta)
                    : new float2(1f, 0f); // 적이 플레이어와 겹쳐 있으면 방향이 없다. 아무 쪽으로나 쏜다.

                for (int p = 0; p < perTarget; p++)
                {
                    if (freeCursor >= free.Length)
                    {
                        return; // 풀이 바닥났다. 남은 발은 건너뛴다.
                    }

                    // P 발을 [-spread/2, +spread/2] 에 고르게 배치. 1 발이면 정조준.
                    float offset = perTarget == 1 ? 0f : -spread * 0.5f + spread * p / (perTarget - 1);
                    float2 direction = Rotate(aim, offset);

                    Launch(ref state, free[freeCursor], origin, direction, weapon);
                    freeCursor++;
                }
            }
        }

        private static float2 Rotate(float2 v, float radians)
        {
            float c = math.cos(radians);
            float s = math.sin(radians);
            return new float2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        private static void Launch(ref SystemState state, Entity projectile, float2 origin, float2 direction, in ShardWeapon weapon)
        {
            EntityManager entityManager = state.EntityManager;

            // 프리팹의 스케일·Z 를 보존하고 XY 만 옮긴다.
            LocalTransform transform = entityManager.GetComponentData<LocalTransform>(projectile);
            transform.Position = new float3(origin, transform.Position.z);
            entityManager.SetComponentData(projectile, transform);

            entityManager.SetComponentData(projectile, new Projectile
            {
                Velocity = direction * weapon.ProjectileSpeed,
                Damage = weapon.Damage,
                RemainingLifetime = weapon.ProjectileLifetime,
                PierceRemaining = weapon.Pierce,
                ExplosionRadius = weapon.ExplosionRadius,
                ExplosionDamage = weapon.Damage * weapon.ExplosionDamageRatio,
                HitHistory = default, // 풀에서 재사용되므로 지난 생애의 명중 기록을 반드시 비운다.
            });

            PoolUtility.SetAlive(entityManager, projectile, true);
        }
    }
}
