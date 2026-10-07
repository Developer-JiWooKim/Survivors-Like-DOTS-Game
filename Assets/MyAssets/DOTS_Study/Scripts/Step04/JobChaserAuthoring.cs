using Assets.MyAssets.DOTS_Study.Scripts.Step03;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step04
{
    public sealed class JobChaserAuthoring : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 10f;
        private sealed class JobChaserBaker : Baker<JobChaserAuthoring>
        {
            public override void Bake(JobChaserAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<JobChaserTag>(entity);
                AddComponent(entity, new MoveSpeed() { Speed = authoring._moveSpeed });
            }
        }
    }
}
