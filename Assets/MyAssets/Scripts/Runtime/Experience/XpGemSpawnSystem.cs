using Assets.MyAssets.Scripts.Runtime.Enemy;
using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Pooling;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// <see cref="XpDropBus"/> 에 쌓인 드랍 요청마다 풀에서 젬 하나를 꺼내 그 자리에 놓는다.
    ///
    /// 풀이 바닥나면 새 젬을 만들지 않고, 플레이어에게서 **가장 먼** 젬에 경험치를 합친다 (사용자 결정).
    /// 경험치는 사라지지 않고, 멀리 쌓인 젬이 커지는 형태가 된다.
    /// "가장 먼 젬" 은 풀이 바닥난 프레임에만 한 번 계산하고, 그 프레임의 넘친 요청은 전부 그 젬에 몰아준다.
    ///
    /// 배정은 한 잡이 순서대로 한다. 병렬로 하면 여러 워커가 같은 빈 젬을 고를 수 있다 (XpDropBus 주석 참조).
    /// 한 프레임 사망 수는 많아야 수십~수백이라 단일 스레드로 충분하다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(EnemyDeathSystem))]
    public partial struct XpGemSpawnSystem : ISystem
    {
        private EntityQuery _freeQuery;
        private EntityQuery _activeQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _freeQuery = SystemAPI.QueryBuilder()
                .WithAll<XpGem>()
                .WithDisabled<Active>()
                .Build();

            _activeQuery = SystemAPI.QueryBuilder()
                .WithAll<XpGem, Active, LocalTransform>()
                .Build();

            // 버스는 이 시스템이 소유한다.
            state.EntityManager.AddComponentData(state.SystemHandle, new XpDropBus
            {
                Streams = new NativeList<NativeStream>(4, Allocator.Persistent),
                Dependency = default,
            });

            state.RequireForUpdate<PlayerPosition>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            XpDropBus bus = state.EntityManager.GetComponentData<XpDropBus>(state.SystemHandle);
            bus.Dependency.Complete();
            for (int i = 0; i < bus.Streams.Length; i++)
            {
                bus.Streams[i].Dispose();
            }
            bus.Streams.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            RefRW<XpDropBus> bus = SystemAPI.GetComponentRW<XpDropBus>(state.SystemHandle);
            if (bus.ValueRO.Streams.Length == 0)
            {
                return;
            }

            // PlayerPosition 은 메인 스레드에서만 쓰므로 읽어도 기다릴 잡이 없다.
            float2 playerPosition = SystemAPI.GetSingleton<PlayerPosition>().Value;

            NativeList<Entity> free = _freeQuery.ToEntityListAsync(
                Allocator.TempJob, state.Dependency, out JobHandle freeHandle);
            NativeList<Entity> activeEntities = _activeQuery.ToEntityListAsync(
                Allocator.TempJob, state.Dependency, out JobHandle activeHandle);
            NativeList<LocalTransform> activeTransforms = _activeQuery.ToComponentDataListAsync<LocalTransform>(
                Allocator.TempJob, state.Dependency, out JobHandle transformsHandle);

            // 여러 스트림을 직렬 잡으로 처리하는 동안 공유하는 상태
            var cursor = new NativeReference<int>(0, Allocator.TempJob);
            var mergeTarget = new NativeReference<int>(SpawnXpGemJob.MergeTargetUnknown, Allocator.TempJob);

            JobHandle chain = JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(freeHandle, activeHandle, transformsHandle),
                bus.ValueRO.Dependency);

            ComponentLookup<LocalTransform> transformLookup = SystemAPI.GetComponentLookup<LocalTransform>();
            ComponentLookup<XpGem> gemLookup = SystemAPI.GetComponentLookup<XpGem>();
            ComponentLookup<Active> activeLookup = SystemAPI.GetComponentLookup<Active>();
            ComponentLookup<MaterialMeshInfo> visibleLookup = SystemAPI.GetComponentLookup<MaterialMeshInfo>();

            // 스트림마다 잡 하나씩 직렬로 잇는다 (DamageApplySystem 과 같은 이유 — 컨테이너 안의 컨테이너 금지).
            for (int i = 0; i < bus.ValueRO.Streams.Length; i++)
            {
                NativeStream stream = bus.ValueRO.Streams[i];
                chain = new SpawnXpGemJob
                {
                    Drops = stream.AsReader(),
                    FreeGems = free.AsDeferredJobArray(),
                    ActiveGems = activeEntities.AsDeferredJobArray(),
                    ActiveTransforms = activeTransforms.AsDeferredJobArray(),
                    PlayerPosition = playerPosition,
                    Cursor = cursor,
                    MergeTarget = mergeTarget,
                    TransformLookup = transformLookup,
                    GemLookup = gemLookup,
                    ActiveLookup = activeLookup,
                    VisibleLookup = visibleLookup,
                }.Schedule(chain);

                chain = stream.Dispose(chain);
            }

            bus.ValueRW.Streams.Clear();
            bus.ValueRW.Dependency = default;

            JobHandle disposed = JobHandle.CombineDependencies(
                free.Dispose(chain),
                activeEntities.Dispose(chain),
                activeTransforms.Dispose(chain));
            disposed = JobHandle.CombineDependencies(disposed, cursor.Dispose(chain), mergeTarget.Dispose(chain));

            state.Dependency = disposed;
        }
    }

    [BurstCompile]
    internal struct SpawnXpGemJob : IJob
    {
        public const int MergeTargetUnknown = -2;
        private const int NoMergeTarget = -1;

        public NativeStream.Reader Drops;

        [ReadOnly] public NativeArray<Entity> FreeGems;
        [ReadOnly] public NativeArray<Entity> ActiveGems;
        [ReadOnly] public NativeArray<LocalTransform> ActiveTransforms;
        public float2 PlayerPosition;

        public NativeReference<int> Cursor;
        public NativeReference<int> MergeTarget;

        public ComponentLookup<LocalTransform> TransformLookup;
        public ComponentLookup<XpGem> GemLookup;
        public ComponentLookup<Active> ActiveLookup;
        public ComponentLookup<MaterialMeshInfo> VisibleLookup;

        public void Execute()
        {
            for (int i = 0; i < Drops.ForEachCount; i++)
            {
                int count = Drops.BeginForEachIndex(i);
                for (int d = 0; d < count; d++)
                {
                    XpDrop drop = Drops.Read<XpDrop>();

                    if (Cursor.Value < FreeGems.Length)
                    {
                        Place(FreeGems[Cursor.Value], drop);
                        Cursor.Value++;
                    }
                    else
                    {
                        MergeIntoFarthest(drop.Value);
                    }
                }
                Drops.EndForEachIndex();
            }
        }

        private void Place(Entity gem, in XpDrop drop)
        {
            // 프리팹의 스케일·Z 를 보존하고 XY 만 옮긴다.
            LocalTransform transform = TransformLookup[gem];
            transform.Position.xy = drop.Position;
            TransformLookup[gem] = transform;

            GemLookup[gem] = new XpGem
            {
                Value = drop.Value,
                Attracted = false,
                Speed = 0f,
            };

            ActiveLookup.SetComponentEnabled(gem, true);
            VisibleLookup.SetComponentEnabled(gem, true);
        }

        private void MergeIntoFarthest(int value)
        {
            if (MergeTarget.Value == MergeTargetUnknown)
            {
                MergeTarget.Value = FindFarthest();
            }

            // 풀이 0 이라 활성 젬조차 없는 경우. 경험치를 받을 곳이 없어 버린다.
            if (MergeTarget.Value == NoMergeTarget)
            {
                return;
            }

            Entity target = ActiveGems[MergeTarget.Value];
            XpGem gem = GemLookup[target];
            gem.Value += value;
            GemLookup[target] = gem;
        }

        private int FindFarthest()
        {
            int best = NoMergeTarget;
            float bestDistanceSquared = -1f;

            for (int i = 0; i < ActiveTransforms.Length; i++)
            {
                float distanceSquared = math.distancesq(ActiveTransforms[i].Position.xy, PlayerPosition);
                if (distanceSquared > bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    best = i;
                }
            }

            return best;
        }
    }
}
