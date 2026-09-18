using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 타일 렌더 쿼드 프리팹에 붙이는 Authoring.
    ///
    /// <c>URPMaterialPropertyBaseColor</c> 를 붙이는 이유:
    /// 타일 종류가 7 가지인데 머티리얼을 7 개로 나누면 인스턴싱 배치가 7 개로 쪼개진다.
    /// 머티리얼 하나에 **인스턴스별 색 오버라이드**를 쓰면 종류가 몇이든 드로우콜은 그대로다.
    /// 연소·감전 같은 상태 색도 같은 장치로 공짜로 표현된다.
    /// 머티리얼의 셰이더가 <c>_BaseColor</c> 를 갖고 GPU Instancing 이 켜져 있어야 한다
    /// (Tools/도형 머티리얼 생성 이 보장한다).
    /// </summary>
    public sealed class TileViewAuthoring : MonoBehaviour
    {
        private sealed class TileViewBaker : Baker<TileViewAuthoring>
        {
            public override void Bake(TileViewAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<TileViewTag>(entity);

                // 실제 값은 TileRenderPoolSystem 이 풀을 만들 때 배정한다.
                AddComponent(entity, new TileViewIndex { Value = 0 });

                AddComponent(entity, new URPMaterialPropertyBaseColor());
            }
        }
    }
}
