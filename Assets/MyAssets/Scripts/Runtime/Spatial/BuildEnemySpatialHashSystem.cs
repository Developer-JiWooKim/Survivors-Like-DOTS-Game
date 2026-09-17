using Assets.MyAssets.Scripts.Runtime.Combat;
using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Spatial
{
    /// <summary>
    /// 살아있는 적을 셀 단위로 해시에 넣는다 (기획서 8.2 의 6번). 매 프레임 비우고 병렬로 다시 채운다.
    ///
    /// 왜 매 프레임 전체 재구축인가 (움직인 적만 갱신하지 않고):
    /// 적은 거의 전부 매 프레임 움직인다. 부분 갱신은 "어느 셀에서 빠졌나" 를 추적하는 비용이 더 크다.
    ///
    /// 재스폰 직후, 이동 직전의 위치를 담는다. 이동 잡은 이 스냅샷(= 이번 프레임 시작 위치)을 보고 이웃을 피한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(EnemyRespawnSystem))]
    [UpdateBefore(typeof(EnemyChaseSystem))]
    public partial struct BuildEnemySpatialHashSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<EnemyMovement, Active, LocalTransform, HitRadius>()
                .Build();

            state.EntityManager.AddComponentData(state.SystemHandle, new EnemySpatialHash
            {
                Map = new NativeParallelMultiHashMap<int, AgentRef>(1024, Allocator.Persistent),
            });

            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            EnemySpatialHash hash = state.EntityManager.GetComponentData<EnemySpatialHash>(state.SystemHandle);
            hash.BuildHandle.Complete();
            hash.ReadersHandle.Complete();
            hash.Map.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<EnemySpatialHash> hash = SystemAPI.GetComponentRW<EnemySpatialHash>(state.SystemHandle);

            // 지난 프레임에 해시를 읽던 잡이 남아 있으면 비우기 전에 끝낸다.
            // 프레임 경계에서는 보통 이미 끝나 있어 대기가 거의 없다.
            hash.ValueRO.ReadersHandle.Complete();
            hash.ValueRO.BuildHandle.Complete();

            NativeParallelMultiHashMap<int, AgentRef> map = hash.ValueRO.Map;
            map.Clear();

            // 병렬 쓰기는 용량을 늘릴 수 없어서 미리 확보해야 한다.
            // WithoutFiltering = Active 무시한 풀 전체 수. 상한으로 쓰기 충분하고, enabled 비트 동기화가 필요 없다.
            int upperBound = _query.CalculateEntityCountWithoutFiltering();
            if (map.Capacity < upperBound)
            {
                map.Capacity = upperBound;
            }

            state.Dependency = new BuildEnemySpatialHashJob
            {
                Writer = map.AsParallelWriter(),
            }.ScheduleParallel(_query, state.Dependency);

            hash.ValueRW.BuildHandle = state.Dependency;
            hash.ValueRW.ReadersHandle = default;
        }
    }

    [BurstCompile]
    internal partial struct BuildEnemySpatialHashJob : IJobEntity
    {
        public NativeParallelMultiHashMap<int, AgentRef>.ParallelWriter Writer;

        private void Execute(Entity entity, in LocalTransform transform, in HitRadius radius)
        {
            float2 position = transform.Position.xy;
            Writer.Add(EnemySpatialHash.KeyOf(EnemySpatialHash.CellOf(position)), new AgentRef
            {
                Entity = entity,
                Position = position,
                Radius = radius.Value,
            });
        }
    }
}
