using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 화면 부근 타일을 그리는 쿼드 풀 설정 (2026-09-18 결정).
    ///
    /// 왜 화면 부근만 엔티티로 그리나:
    /// 262,144 타일을 전부 엔티티로 두는 건 논외고, 맵 전체를 한 장의 텍스처로 올리는 방법도 있었다.
    /// 후자를 택하지 않은 이유는 타일별 스프라이트·연출(M6 아트 교체)로 갈 길을 막지 않기 위해서다.
    /// 대신 카메라가 움직일 때마다 어느 타일을 보여줄지 재계산하는 비용을 진다.
    ///
    /// 재계산을 싸게 만드는 장치 — **풀 엔티티를 창(window) 격자에 고정한다**:
    /// 풀의 i 번째 엔티티는 항상 창 안의 (i % ViewWidth, i / ViewWidth) 칸을 담당한다.
    /// 창이 움직이면 "어느 엔티티에 어느 타일을 배정할지" 를 다시 푸는 대신, 모든 엔티티가
    /// 자기 자리에 해당하는 타일을 다시 읽기만 하면 된다. 배정 로직도, 들어오고 나가는 타일 추적도 없다.
    /// ~1,000 엔티티의 위치·색을 매 프레임 병렬로 덮어쓰는 편이 훨씬 싸고 버그가 없다.
    /// </summary>
    public struct TileRenderPool : IComponentData
    {
        public Entity Prefab;

        /// <summary>창의 가로·세로 타일 수. 카메라 시야(~30×17 u)보다 넉넉해야 가장자리가 비지 않는다.</summary>
        public int ViewWidth;

        public int ViewHeight;

        /// <summary>타일 쿼드의 z. 카메라가 z = -10 에서 +z 를 보므로 값이 클수록 뒤에 그려진다. 적·플레이어(z=0)보다 뒤여야 한다.</summary>
        public float Depth;
    }

    /// <summary>
    /// 이 쿼드가 창 안에서 맡은 자리. 풀 생성 시 한 번 배정하고 이후 바뀌지 않는다.
    /// 값이 고정이라 렌더 잡이 창 원점만 알면 담당 타일을 바로 계산할 수 있다.
    /// </summary>
    public struct TileViewIndex : IComponentData
    {
        public int Value;
    }

    /// <summary>타일 쿼드임을 나타내는 태그. 다른 풀(적·젬)과 쿼리를 분리하기 위한 것.</summary>
    public struct TileViewTag : IComponentData
    {
    }
}
