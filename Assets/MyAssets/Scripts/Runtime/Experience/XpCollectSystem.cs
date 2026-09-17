using Assets.MyAssets.Scripts.Runtime.Player;
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

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 젬을 플레이어 쪽으로 끌어당기고, 닿으면 수집해 경험치를 올린다 (기획서 8.2 의 18번 XpMagnetSystem).
    ///
    /// 대상이 플레이어 하나라 젬 N 개 × 1 = O(N). 젬 1만 개(기획서 7.1)도 공간 해시 없이 병렬 잡으로 감당한다.
    ///
    /// 수집한 경험치는 스트림에 모아 한 잡이 합산한다.
    /// 병렬 잡이 PlayerExperience 에 직접 더하면 여러 워커가 같은 값을 동시에 고치는 레이스가 난다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(XpGemSpawnSystem))]
    public partial struct XpCollectSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Active 는 켜진 것만, MaterialMeshInfo 는 상태 무관 (ProjectileMoveSystem 주석 참조)
            _query = SystemAPI.QueryBuilder()
                .WithAllRW<XpGem, LocalTransform>()
                .WithAllRW<Active>()
                .WithPresentRW<MaterialMeshInfo>()
                .Build();

            state.RequireForUpdate(_query);
            state.RequireForUpdate<XpCollectSettings>();
            state.RequireForUpdate<PlayerExperience>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<PlayerExperience>();

            // 젬 LocalTransform 에 쓰는 잡이라 플레이어 LocalTransform 을 룩업으로 읽을 수 없다 → 사본 사용
            float2 playerPosition = SystemAPI.GetComponent<PlayerPosition>(player).Value;

            var collected = new NativeStream(_query.CalculateChunkCountWithoutFiltering(), Allocator.TempJob);

            JobHandle handle = new XpCollectJob
            {
                Player = playerPosition,
                Settings = SystemAPI.GetSingleton<XpCollectSettings>(),
                DeltaTime = SystemAPI.Time.DeltaTime,
                Collected = collected.AsWriter(),
            }.ScheduleParallel(_query, state.Dependency);

            handle = new AddExperienceJob
            {
                Collected = collected.AsReader(),
                Player = player,
                ExperienceLookup = SystemAPI.GetComponentLookup<PlayerExperience>(),
            }.Schedule(handle);

            state.Dependency = collected.Dispose(handle);
        }
    }

    [BurstCompile]
    internal partial struct XpCollectJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public float2 Player;
        public XpCollectSettings Settings;
        public float DeltaTime;

        public NativeStream.Writer Collected;

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            Collected.BeginForEachIndex(unfilteredChunkIndex);
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
        {
            Collected.EndForEachIndex();
        }

        private void Execute(
            ref LocalTransform transform,
            ref XpGem gem,
            EnabledRefRW<Active> active,
            EnabledRefRW<MaterialMeshInfo> visible)
        {
            float2 toPlayer = Player - transform.Position.xy;
            float distanceSquared = math.lengthsq(toPlayer);

            if (distanceSquared <= Settings.PickupRadius * Settings.PickupRadius)
            {
                Collected.Write(gem.Value);
                active.ValueRW = false;
                visible.ValueRW = false;
                return;
            }

            if (!gem.Attracted)
            {
                if (distanceSquared > Settings.MagnetRadius * Settings.MagnetRadius)
                {
                    return;
                }

                gem.Attracted = true;
                gem.Speed = Settings.AttractStartSpeed;
            }

            gem.Speed += Settings.AttractAcceleration * DeltaTime;

            // 남은 거리보다 멀리 가면 플레이어를 지나쳐 되돌아오며 떨린다. 남은 거리로 자른다.
            float distance = math.sqrt(distanceSquared);
            float step = math.min(gem.Speed * DeltaTime, distance);
            transform.Position.xy += toPlayer / distance * step;
        }
    }

    [BurstCompile]
    internal struct AddExperienceJob : IJob
    {
        public NativeStream.Reader Collected;
        public Entity Player;
        public ComponentLookup<PlayerExperience> ExperienceLookup;

        public void Execute()
        {
            int total = 0;
            for (int i = 0; i < Collected.ForEachCount; i++)
            {
                int count = Collected.BeginForEachIndex(i);
                for (int c = 0; c < count; c++)
                {
                    total += Collected.Read<int>();
                }
                Collected.EndForEachIndex();
            }

            if (total == 0)
            {
                return;
            }

            // 레벨업 판정은 여기서 하지 않는다. LevelUpSystem 이 다음 프레임 시작에 본다 —
            // 일시정지(메인 스레드 상태 변경)가 필요한 일이라 잡 안에서 할 수 없다.
            RefRW<PlayerExperience> experience = ExperienceLookup.GetRefRW(Player);
            experience.ValueRW.Xp += total;
        }
    }
}
