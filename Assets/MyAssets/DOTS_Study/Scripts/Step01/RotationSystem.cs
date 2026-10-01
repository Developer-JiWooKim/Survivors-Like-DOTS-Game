using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step01
{
    [BurstCompile]
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // RotationSpeed를 가진 엔티티가 있을때만 OnUpdate를 하게 하기 위해,
            // 그렇지 않으면 RotationSpeed를 가진 엔티티가 없어도 OnUpdate가 실행되어 SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>() 하기 때문, 
            // 대신 해당하는 엔티티가 없으므로 반복은 0
            state.RequireForUpdate<RotationSpeed>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, speed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                // LocalTransform을 읽어와서 speed.ValueRO.RadiansPerSecond * deltaTime 값을 
                // LocalTransform Z값에 맞게 변환하는 과정을 적용한 후 엔티티 LocalTransform의 값에 대입
                transform.ValueRW = transform.ValueRO.RotateZ(speed.ValueRO.RadiansPerSecond * deltaTime);

            }
        }
    }
}