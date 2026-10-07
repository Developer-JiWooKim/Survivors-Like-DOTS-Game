using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step03
{
    public sealed class StudyPlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 10f;

        private sealed class StudyPlayerBaker : Baker<StudyPlayerAuthoring>
        {
            public override void Bake(StudyPlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent(entity, new MoveSpeed() { Speed = authoring._moveSpeed });
            }
        }
    }
}