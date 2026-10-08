using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step05
{
    public sealed class LifeAuthoring : MonoBehaviour
    {
        [SerializeField] private float _lifeTime;
        [SerializeField] private float _maxLifeTime;
        private sealed class LifeBaker : Baker<LifeAuthoring>
        {
            public override void Bake(LifeAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Life()
                {
                    LifeTime = authoring._lifeTime,
                    MaxLifeTime = authoring._maxLifeTime
                });

                AddComponent<Alive>(entity);
            }
        }
    }
}