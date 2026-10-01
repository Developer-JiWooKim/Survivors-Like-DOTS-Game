using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step02
{
    public sealed class SpawnerAuthoring : MonoBehaviour
    {
        [SerializeField] private float _degreePerSecond;

        private sealed class SpanwerBaker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {

            }
        }
    }
}