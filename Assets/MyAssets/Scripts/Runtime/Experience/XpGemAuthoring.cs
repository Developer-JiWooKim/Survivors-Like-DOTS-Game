using Assets.MyAssets.Scripts.Runtime.Pooling;
using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// XP 젬 프리팹에 붙이는 Authoring. 값은 스폰 시점에 채운다.
    /// </summary>
    public sealed class XpGemAuthoring : MonoBehaviour
    {
        private sealed class XpGemBaker : Baker<XpGemAuthoring>
        {
            public override void Bake(XpGemAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new XpGem());

                // 켜진 채로 베이킹하고, 풀 생성 시스템이 인스턴스를 만든 직후 전부 끈다 (ProjectileAuthoring 과 같은 이유).
                AddComponent<Active>(entity);
            }
        }
    }
}
