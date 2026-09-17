using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// SubScene 에 배치하는 XP 젬 풀 + 수집 설정.
    /// </summary>
    public sealed class XpGemPoolAuthoring : MonoBehaviour
    {
        [Tooltip("XpGemAuthoring 이 붙은 프리팹 에셋 (씬 안의 오브젝트 아님)")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("동시에 바닥에 있을 수 있는 젬 수. 가득 차면 가장 먼 젬에 경험치를 합친다")]
        [SerializeField] private int _capacity = 4000;

        [Header("수집")]
        [Tooltip("이 거리 안의 젬이 끌려오기 시작한다")]
        [SerializeField] private float _magnetRadius = 2.5f;

        [Tooltip("이 거리 안에 들어오면 수집된다")]
        [SerializeField] private float _pickupRadius = 0.5f;

        [Tooltip("끌려오기 시작할 때의 속도 (u/s)")]
        [SerializeField] private float _attractStartSpeed = 12f;

        [Tooltip("끌려오는 동안의 가속도 (u/s²)")]
        [SerializeField] private float _attractAcceleration = 24f;

        private sealed class XpGemPoolBaker : Baker<XpGemPoolAuthoring>
        {
            public override void Bake(XpGemPoolAuthoring authoring)
            {
                if (authoring._prefab == null)
                {
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new XpGemPool
                {
                    Prefab = GetEntity(authoring._prefab, TransformUsageFlags.Dynamic),
                    Capacity = authoring._capacity,
                });

                AddComponent(entity, new XpCollectSettings
                {
                    MagnetRadius = authoring._magnetRadius,
                    PickupRadius = authoring._pickupRadius,
                    AttractStartSpeed = authoring._attractStartSpeed,
                    AttractAcceleration = authoring._attractAcceleration,
                });
            }
        }
    }
}
