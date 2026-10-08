using Unity.Entities;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step05
{
    public struct Life : IComponentData
    {
        public float LifeTime;
        public float MaxLifeTime;
    }
}
