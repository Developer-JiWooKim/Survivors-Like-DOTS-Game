using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.MyAssets.DOTS_Study.Scripts.Step01
{
    // 인스펙터에서 도/초로 입력받고, 베이킹할 때 라디안으로 바꿔서 RotationSpeed를 붙임
    public sealed class RotationSpeedAuthoring : MonoBehaviour
    {
        [SerializeField] private float _degreePerSecond;

        private sealed class RotationBaker : Baker<RotationSpeedAuthoring>
        {
            public override void Bake(RotationSpeedAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RotationSpeed
                {
                    RadiansPerSecond = math.radians(authoring._degreePerSecond),
                });
            }
        }
    }
}