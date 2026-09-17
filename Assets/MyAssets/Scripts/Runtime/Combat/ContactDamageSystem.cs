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
    /// 플레이어와 겹친 적마다 <c>초당 피해 × dt</c> 를 플레이어에게 입힌다.
    ///
    /// 대상이 플레이어 하나뿐이라 적 N 마리 × 1 = O(N) 이다. 투사체 충돌과 달리 공간 해시가 없어도 확장된다.
    /// 1만 마리에서도 거리 비교 1만 번이라 병렬 잡이면 충분하다.
    ///
    /// 피해는 적마다 이벤트 하나씩 스트림에 쓴다. 합산해서 하나로 보내지 않는 이유:
    /// 나중에 적별 피격 연출(누가 때렸나)이나 속성 피해가 붙을 때 정보가 남아 있어야 한다.
    /// 적 수백 마리가 한꺼번에 붙어도 이벤트 수백 개라 적용 비용은 무시할 수준이다.
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
            state.RequireForUpdate<PlayerMovement>();
            state.RequireForUpdate<DamageEventBus>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<PlayerMovement>();

            // 플레이어 위치를 여기서 GetComponent 로 읽으면, 방금 예약된 적 이동 잡(LocalTransform 쓰기)이
            // 끝날 때까지 메인 스레드가 기다린다 (ISSUE-006 과 같은 종류). 룩업으로 잡 안에서 읽는다.
            var stream = new NativeStream(_enemyQuery.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            JobHandle handle = new ContactDamageJob
            {
                Player = player,
                PlayerRadius = SystemAPI.GetComponent<HitRadius>(player).Value,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Writer = stream.AsWriter(),
            }.ScheduleParallel(_enemyQuery, state.Dependency);

            SystemAPI.GetSingletonRW<DamageEventBus>().ValueRW.Register(stream, handle);
            state.Dependency = handle;
        }
    }

    [BurstCompile]
    internal partial struct ContactDamageJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public Entity Player;
        public float PlayerRadius;
        public float DeltaTime;

        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public NativeStream.Writer Writer;

        // 청크마다 한 번만 룩업한다. 잡 구조체는 워커마다 복사되므로 스레드 간에 공유되지 않는다.
        private float2 _playerPosition;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            _playerPosition = TransformLookup[Player].Position.xy;
            Writer.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Writer.EndForEachIndex();
        }

        private void Execute(in LocalTransform transform, in HitRadius radius, in ContactDamage contact)
        {
            float reach = radius.Value + PlayerRadius;
            if (math.distancesq(transform.Position.xy, _playerPosition) > reach * reach)
            {
                return;
            }

            Writer.Write(new DamageEvent
            {
                Target = Player,
                Amount = contact.PerSecond * DeltaTime,
            });
        }
    }
}
