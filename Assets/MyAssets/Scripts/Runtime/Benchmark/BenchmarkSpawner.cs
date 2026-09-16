using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Benchmark
{
    /// <summary>
    /// M0 벤치마크용 스폰 설정.
    ///
    /// 왜 프로토타입을 Entity 로 들고 있나:
    /// SubScene 에 렌더 가능한 GameObject 를 하나만 두고 베이킹하면, Entities Graphics 가
    /// 렌더에 필요한 컴포넌트(MaterialMeshInfo, RenderBounds, RenderFilterSettings 등)를
    /// 전부 붙여준다. 그걸 원본 삼아 Instantiate 하면 렌더 컴포넌트 구성을 직접 만들 필요가 없고,
    /// 실제 게임에서 쓸 풀링 방식(원본 1개 → 대량 복제)과도 같은 경로가 된다.
    /// </summary>
    public struct BenchmarkSpawner : IComponentData
    {
        /// <summary>SubScene 에서 베이킹된 렌더 가능한 원본 엔티티.</summary>
        public Entity Prototype;

        /// <summary>생성할 인스턴스 수.</summary>
        public int Count;

        /// <summary>인스턴스를 흩뿌릴 정사각 영역의 한 변 길이 (월드 유닛).</summary>
        public float AreaSize;
    }
}
