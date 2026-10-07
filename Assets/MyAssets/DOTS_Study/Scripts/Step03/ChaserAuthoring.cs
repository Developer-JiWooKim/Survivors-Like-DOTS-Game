using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step03
{
    public sealed class ChaserAuthoring : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 10f;

        private sealed class ChaserBaker : Baker<ChaserAuthoring>
        {
            public override void Bake(ChaserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ChaserTag>(entity);
                AddComponent(entity, new MoveSpeed() { Speed = authoring._moveSpeed });
            }
        }
    }
}