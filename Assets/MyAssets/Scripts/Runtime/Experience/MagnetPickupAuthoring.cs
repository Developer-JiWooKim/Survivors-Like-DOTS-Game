using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 자석 아이템 프리팹에 붙이는 Authoring.
    /// </summary>
    public sealed class MagnetPickupAuthoring : MonoBehaviour
    {
        private sealed class MagnetPickupBaker : Baker<MagnetPickupAuthoring>
        {
            public override void Bake(MagnetPickupAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MagnetPickup>(entity);

                // 켜진 채로 베이킹하고, 풀 생성 시스템이 인스턴스를 만든 직후 전부 끈다 (ProjectileAuthoring 과 같은 이유).
                AddComponent<Active>(entity);
            }
        }
    }
}
