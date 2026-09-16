using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Benchmark
{
    /// <summary>
    /// SubScene 에 배치하는 벤치마크 스폰 설정. 베이킹 시 <see cref="BenchmarkSpawner"/> 로 변환된다.
    /// </summary>
    public sealed class BenchmarkSpawnerAuthoring : MonoBehaviour
    {
        [Tooltip("복제할 원본. 렌더러가 붙은 GameObject 를 지정한다.")]
        [SerializeField] private GameObject _prototype;

        [Tooltip("생성할 인스턴스 수.")]
        [SerializeField] private int _count = 10000;

        [Tooltip("인스턴스를 흩뿌릴 정사각 영역의 한 변 길이 (월드 유닛).")]
        [SerializeField] private float _areaSize = 120f;

        private sealed class BenchmarkSpawnerBaker : Baker<BenchmarkSpawnerAuthoring>
        {
            public override void Bake(BenchmarkSpawnerAuthoring authoring)
            {
                if (authoring._prototype == null)
                {
                    return;
                }

                // 스포너 자체는 화면에 그릴 게 없으므로 트랜스폼 컴포넌트를 붙이지 않는다.
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new BenchmarkSpawner
                {
                    // 원본은 위치가 바뀌어야 하므로 Dynamic. 이걸 None 으로 두면
                    // LocalTransform 이 안 붙어서 복제본 위치를 설정할 수 없다.
                    Prototype = GetEntity(authoring._prototype, TransformUsageFlags.Dynamic),
                    Count = authoring._count,
                    AreaSize = authoring._areaSize,
                });
            }
        }
    }
}
