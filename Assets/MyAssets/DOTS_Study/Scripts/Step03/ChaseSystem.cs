using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step03
{
    [BurstCompile]
    [UpdateAfter(typeof(StudyPlayerMoveSystem))]
    public partial struct ChaseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChaserTag>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<PlayerTag>();
            float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(player).Position;
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>().WithAll<ChaserTag>().WithNone<PlayerTag>())
            {
                float3 dir = targetPosition - transform.ValueRO.Position;
                dir.z = 0f;
                dir = math.normalizesafe(dir);

                transform.ValueRW.Position.x += dir.x * speed.ValueRO.Speed * deltaTime;
                transform.ValueRW.Position.y += dir.y * speed.ValueRO.Speed * deltaTime;
            }
        }
    }
}
