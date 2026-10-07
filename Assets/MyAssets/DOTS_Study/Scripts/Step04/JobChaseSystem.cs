using Assets.MyAssets.DOTS_Study.Scripts.Step03;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step04
{
    [BurstCompile]
    [UpdateAfter(typeof(StudyPlayerMoveSystem))]
    public partial struct JobChaseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<JobChaserTag>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity player = SystemAPI.GetSingletonEntity<PlayerTag>();
            float3 targetPosition = SystemAPI.GetComponent<LocalTransform>(player).Position;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // new ChaseJob { DeltaTime = deltaTime, TargetPosition = targetPosition }.Run();
            // new ChaseJob { DeltaTime = deltaTime, TargetPosition = targetPosition }.Schedule();
            new ChaseJob { DeltaTime = deltaTime, TargetPosition = targetPosition }.ScheduleParallel();
        }
    }
}
