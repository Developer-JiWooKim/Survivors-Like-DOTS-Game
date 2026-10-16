using Unity.Entities;

namespace SurvivorsLikeDOTS.Player
{
    public struct PlayerAimSettings : IComponentData
    {
        public float RadiansPerPixel;
        public float RollFollowRate;
    }
}
