using Assets.MyAssets.Scripts.Runtime.Player;
using Assets.MyAssets.Scripts.Runtime.Run;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 플레이어까지의 거리 필드를 만들어 둔다 (ISSUE-013 → ISSUE-014).
    ///
    /// **플레이어가 다른 칸으로 넘어갔을 때만** 다시 만든다.
    /// 한 칸 안에서 움직이는 동안에는 필드가 그대로 유효하므로 초당 수십 번 만들 이유가 없다.
    /// 1 u = 1 타일이고 플레이어 속도가 5 u/s 라 재계산은 **초당 5 회 남짓**이다.
    ///
    /// 단일 스레드 잡으로 띄운다. 래스터 스윕은 앞선 칸의 결과를 뒤 칸이 바로 쓰는 구조라
    /// 나눠 돌리면 수렴이 느려진다. 대신 **메인 스레드를 막지 않는 것만으로 충분**하다
    /// (M2 에서 확인된 병목이 메인 스레드다).
    ///
    /// <see cref="EnemyChaseSystem"/> 보다 먼저 돌아 이번 프레임 위치를 반영한다.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(PlayerMoveSystem))]
    public partial struct FlowFieldSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TerrainGrid>();
            state.RequireForUpdate<PlayerPosition>();

            state.EntityManager.AddComponentData(state.SystemHandle, new FlowField
            {
                SourceCell = new int2(int.MinValue, int.MinValue),
                HasField = false,
            });
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            FlowField field = state.EntityManager.GetComponentData<FlowField>(state.SystemHandle);
            field.WriteHandle.Complete();
            field.ReadersHandle.Complete();

            if (field.Distance.IsCreated)
            {
                field.Distance.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TerrainGrid grid = SystemAPI.GetSingleton<TerrainGrid>();
            float2 playerPosition = SystemAPI.GetSingleton<PlayerPosition>().Value;
            int2 cell = grid.CellOf(playerPosition);

            RefRW<FlowField> field = SystemAPI.GetComponentRW<FlowField>(state.SystemHandle);

            int count = grid.Tiles.Length;
            bool sizeChanged = !field.ValueRO.Distance.IsCreated || field.ValueRO.Distance.Length != count;

            // 플레이어가 같은 칸 안에서 움직이는 동안에는 필드가 그대로 유효하다.
            if (!sizeChanged && field.ValueRO.HasField && field.ValueRO.SourceCell.Equals(cell))
            {
                return;
            }

            if (sizeChanged)
            {
                field.ValueRO.WriteHandle.Complete();
                field.ValueRO.ReadersHandle.Complete();

                if (field.ValueRO.Distance.IsCreated)
                {
                    field.ValueRW.Distance.Dispose();
                }

                field.ValueRW.Distance = new NativeArray<float>(count, Allocator.Persistent);
            }

            field.ValueRW.Width = grid.Width;
            field.ValueRW.Height = grid.Height;
            field.ValueRW.Origin = grid.Origin;
            field.ValueRW.SourceCell = cell;
            field.ValueRW.HasField = true;

            var job = new BuildFlowFieldJob
            {
                Tiles = grid.Tiles,
                Width = grid.Width,
                Height = grid.Height,
                Source = cell,
                Distance = field.ValueRO.Distance,
            };

            // 그리드에 쓰는 잡(확산 틱)과, 지난 프레임에 필드를 읽던 잡(적 이동)이 모두 끝난 뒤여야 한다.
            JobHandle dependency = JobHandle.CombineDependencies(
                state.Dependency,
                grid.WriteHandle,
                field.ValueRO.ReadersHandle);

            JobHandle handle = job.Schedule(dependency);

            SystemAPI.GetSingletonRW<TerrainGrid>().ValueRW.RegisterReader(handle);
            field.ValueRW.WriteHandle = handle;
            field.ValueRW.ReadersHandle = default;
            state.Dependency = handle;
        }
    }

    /// <summary>
    /// 계산 자체는 <see cref="FlowFieldBuilder"/> 가 하고, 이 잡은 그걸 워커 스레드에서 부르는 껍데기다
    /// (규칙을 EditMode 테스트로 검증하기 위한 분리 — <c>TerrainTickSystem</c> 과 같은 구성).
    /// </summary>
    [BurstCompile]
    internal struct BuildFlowFieldJob : IJob
    {
        [ReadOnly] public NativeArray<TileData> Tiles;

        public int Width;
        public int Height;
        public int2 Source;

        public NativeArray<float> Distance;

        public void Execute()
        {
            FlowFieldBuilder.Build(Tiles, Width, Height, Source, ref Distance);
        }
    }
}
