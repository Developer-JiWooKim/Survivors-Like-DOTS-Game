using Unity.Entities;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step02
{
    public struct Spawner : IComponentData
    {
        public Entity Prefab;
        public int Count;
        public float Range;
        public uint Seed;
        public float MinRadiansPerSecond;
        public float MaxRadiansPerSecond;
    }
}