using Unity.Entities;
using Unity.Mathematics;

namespace SurvivorsLikeDOTS.Player
{
    public struct PlayerAim : IComponentData
    {
        public quaternion Rotation;
    }
}
