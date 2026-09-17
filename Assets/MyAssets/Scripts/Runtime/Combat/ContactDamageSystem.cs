using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 플레이어가 적에 닿으면 1 회 피해를 받고 잠시 무적이 된다 (기획서 3 장 "적 접촉 시 데미지, 0.5초 무적 프레임").
    ///
    /// 두 단계로 나눈다:
    /// 1. <see cref="ContactScanJob"/> (병렬) — 플레이어에 닿은 적들의 피해를 모두 모은다. 적 N × 1 = O(N).
    /// 2. <see cref="ResolveHitJob"/> (단일) — 그중 **가장 큰 피해 하나만** 데미지 버스로 보내고 무적을 건다.
    /// 여러 적이 같은 프레임에 닿아도 피해는 한 번이다. 병렬 잡에서 "하나만" 을 고르면 레이스가 나므로 단일 잡으로 줄인다.
    ///
    /// 무적 중에는 1 단계 잡 자체를 예약하지 않는다.
    ///
    /// 〔변경 이력〕 처음(2026-09-17 (2))에는 "무적 없이 닿아 있는 적마다 초당 피해" 로 구현했다.
    /// 기획서 3 장 표를 확인하지 않은 채 수치를 제안한 실수였고, M1 게이트에서 발견해 기획서대로 바꿨다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(EnemyChaseSystem))]
    [UpdateBefore(typeof(DamageApplySystem))]
    public partial struct ContactDamageSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _enemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active, LocalTransform, HitRadius, ContactDamage>()
                .Build();

            state.RequireForUpdate(_enemyQuery);
            state.RequireForUpdate<HitInvulnerability>();
            state.RequireForUpdate<DamageEventBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<HitInvulnerability>();

            // 무적 시간은 메인 스레드에서 줄인다. 무적을 거는 ResolveHitJob 은 지난 프레임 잡이라 끝나 있다.
            RefRW<HitInvulnerability> invulnerability = SystemAPI.GetComponentRW<HitInvulnerability>(player);
            invulnerability.ValueRW.Remaining -= SystemAPI.Time.DeltaTime;
            if (invulnerability.ValueRO.IsActive)
            {
                return;
            }

            var contacts = new NativeStream(_enemyQuery.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            // 플레이어 위치: 적 LocalTransform 은 읽기만 하므로 룩업으로 읽어도 된다.
            // (메인 스레드에서 GetComponent 하면 방금 예약된 적 이동 잡을 기다린다 — ISSUE-006 과 같은 종류)
            JobHandle scan = new ContactScanJob
            {
                Player = player,
                PlayerRadius = SystemAPI.GetComponent<HitRadius>(player).Value,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                Contacts = contacts.AsWriter(),
            }.ScheduleParallel(_enemyQuery, state.Dependency);

            // 결과 스트림은 foreach 인덱스 하나에 최대 1 건
            var hit = new NativeStream(1, Allocator.TempJob);

            JobHandle resolve = new ResolveHitJob
            {
                Contacts = contacts.AsReader(),
                Hit = hit.AsWriter(),
                Player = player,
                InvulnerabilityLookup = SystemAPI.GetComponentLookup<HitInvulnerability>(),
            }.Schedule(scan);

            // hit 스트림 해제는 DamageApplySystem 의 몫, contacts 는 여기서 정리한다.
            SystemAPI.GetSingletonRW<DamageEventBus>().ValueRW.Register(hit, resolve);
            state.Dependency = contacts.Dispose(resolve);
        }
    }

    [BurstCompile]
    internal partial struct ContactScanJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public Entity Player;
        public float PlayerRadius;

        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public NativeStream.Writer Contacts;

        // 청크마다 한 번만 룩업한다. 잡 구조체는 워커마다 복사되므로 스레드 간에 공유되지 않는다.
        private float2 _playerPosition;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            _playerPosition = TransformLookup[Player].Position.xy;
            Contacts.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Contacts.EndForEachIndex();
        }

        private void Execute(in LocalTransform transform, in HitRadius radius, in ContactDamage contact)
        {
            float reach = radius.Value + PlayerRadius;
            if (math.distancesq(transform.Position.xy, _playerPosition) <= reach * reach)
            {
                Contacts.Write(contact.Damage);
            }
        }
    }

    [BurstCompile]
    internal struct ResolveHitJob : IJob
    {
        public NativeStream.Reader Contacts;
        public NativeStream.Writer Hit;
        public Entity Player;
        public ComponentLookup<HitInvulnerability> InvulnerabilityLookup;

        public void Execute()
        {
            bool touched = false;
            float strongest = 0f;

            for (int i = 0; i < Contacts.ForEachCount; i++)
            {
                int count = Contacts.BeginForEachIndex(i);
                for (int c = 0; c < count; c++)
                {
                    touched = true;
                    strongest = math.max(strongest, Contacts.Read<float>());
                }
                Contacts.EndForEachIndex();
            }

            // 비어 있어도 인덱스는 열고 닫아야 읽는 쪽(DamageApplySystem)이 정상 처리한다.
            Hit.BeginForEachIndex(0);
            if (touched)
            {
                Hit.Write(new DamageEvent
                {
                    Target = Player,
                    Amount = strongest,
                });

                RefRW<HitInvulnerability> invulnerability = InvulnerabilityLookup.GetRefRW(Player);
                invulnerability.ValueRW.Remaining = invulnerability.ValueRO.Duration;
            }
            Hit.EndForEachIndex();
        }
    }
}
