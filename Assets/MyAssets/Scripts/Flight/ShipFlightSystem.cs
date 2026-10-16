using SurvivorsLikeDOTS.Player;
using Unity.Burst;
using Unity.Entities;


namespace SurvivorsLikeDOTS.Flight
{

    [BurstCompile]
    public partial struct ShipFlightSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

        }
    }
}
