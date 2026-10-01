using Unity.Entities;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step01
{
    // 초당 회전량을 라디안으로 담는 float struct
    public struct RotationSpeed : IComponentData
    {
        public float RadiansPerSecond;
    }
}
