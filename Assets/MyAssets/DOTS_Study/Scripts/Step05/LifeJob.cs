using Assets.MyAssets.DOTS_Study.Scripts.Step03;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step05
{
    [BurstCompile]
    [WithAll(typeof(Life))]
    public partial struct LifeJob : IJobEntity
    {
        public float DeltaTime;
        public float3 TargetPosition;

        private void Execute(ref LocalTransform transform, in MoveSpeed speed)
        {
            float3 dir = TargetPosition - transform.Position;
            dir.z = 0f;
            dir = math.normalizesafe(dir);

            transform.Position.x += dir.x * speed.Speed * DeltaTime;
            transform.Position.y += dir.y * speed.Speed * DeltaTime;
        }
    }
}
