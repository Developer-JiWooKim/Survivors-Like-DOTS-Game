using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step02
{
    public sealed class SpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count = 100;
        [SerializeField] private uint _seed = 1;
        [SerializeField] private float _range = 10;
        [SerializeField] private float _minRadiansPerSecond;
        [SerializeField] private float _maxRadiansPerSecond;

        private sealed class SpawnerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Spawner
                {
                    Prefab = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic),
                    Count = authoring._count,
                    Seed = authoring._seed,
                    Range = authoring._range,
                    MinRadiansPerSecond = math.radians(authoring._minRadiansPerSecond),
                    MaxRadiansPerSecond = math.radians(authoring._maxRadiansPerSecond)
                });
            }
        }
    }
}