using SurvivorsLikeDOTS.Flight;
using SurvivorsLikeDOTS.Input;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


namespace SurvivorsLikeDOTS.Player
{

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ShipFlightSystem))]
    public partial struct PlayerAimSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ShipInput>();
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            ShipInput input = SystemAPI.GetSingleton<ShipInput>();
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (aim, control, aimSetting, transform) in
                    SystemAPI.Query<RefRW<PlayerAim>, RefRW<ShipControl>, RefRO<PlayerAimSettings>, RefRW<LocalTransform>>().WithAll<PlayerTag>())
            {
                quaternion aimRot = aim.ValueRO.Rotation;

                // 마우스 -> 조준 회전(화면 기준이라 뒤에 곱함)
                float yaw = input.AimDelta.x * aimSetting.ValueRO.RadiansPerPixel;
                float pitch = input.AimDelta.y * aimSetting.ValueRO.RadiansPerPixel;
                aimRot = math.mul(aimRot, quaternion.RotateY(yaw));
                aimRot = math.mul(aimRot, quaternion.RotateX(-pitch));

                float3 aimFwd = math.mul(aim.ValueRO.Rotation, math.forward()); // 조준 방향
                float3 curUp = math.mul(aim.ValueRO.Rotation, math.up()); // 조준의 현재 위
                float3 shipUp = math.mul(transform.ValueRO.Rotation, math.up());
                float3 targetUp = math.normalizesafe(shipUp - math.dot(shipUp, aimFwd) * aimFwd, curUp);


                aim.ValueRW.Rotation = aimRot;

                // control.ValueRW.DesiredForward = aim.ValueRO.Rotation;
            }
        }
    }
}
