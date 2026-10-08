using Assets.MyAssets.DOTS_Study.Scripts.Step03;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step05
{
    [BurstCompile]
    public partial struct LifeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Alive>();
            state.RequireForUpdate<Life>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

        }
    }
}
