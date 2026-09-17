using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// SubScene 에 배치하는 적 스폰 설정. 베이킹 시 <see cref="EnemySpawner"/>, <see cref="SpawnDirector"/>,
    /// <see cref="EnemySeparation"/> 으로 변환된다.
    /// </summary>
    public sealed class EnemySpawnerAuthoring : MonoBehaviour
    {
        [Tooltip("적 프리팹 에셋. 씬 안의 오브젝트가 아니라 Project 창의 프리팹을 지정해야 한다.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("미리 만들어 둘 적 수 = 동시에 살아있을 수 있는 최대 수. 기획서 6.2 는 20,000")]
        [SerializeField] private int _poolSize = 20000;

        [Header("스폰 링 (플레이어 기준)")]
        [Tooltip("안쪽 반경. 카메라 size 15, 16:9 화면의 모서리가 약 30.6 이라 그 밖으로 잡았다.")]
        [SerializeField] private float _respawnMinRadius = 32f;

        [Tooltip("바깥 반경.")]
        [SerializeField] private float _respawnMaxRadius = 36f;

        [Header("시간 기반 예산 (기획서 6.2)")]
        [Tooltip("0 분 시점 목표 수. 기획서 50")]
        [SerializeField] private float _baseBudget = 50f;

        [Tooltip("분당 배율. 기획서 1.28 (20 분에 약 12,000)")]
        [SerializeField] private float _growthPerMinute = 1.28f;

        [Tooltip("테스트 전용: 목표 수 계산에만 곱하는 시간 배율. 10 이면 2 분에 20 분 시점의 적 수. 평소 1")]
        [SerializeField] private float _testTimeScale = 1f;

        [Tooltip("벤치마크 전용: 0 보다 크면 시간과 무관하게 적 수를 이 값으로 고정한다. 평소 0")]
        [SerializeField] private int _benchmarkFixedTarget;

        [Header("레벨 비례 체력")]
        [Tooltip("플레이어 레벨당 스폰 체력 배율. 체력 = 프리팹 체력 × 배율^(Lv-1). 새로 스폰되는 적에만 적용")]
        [SerializeField] private float _healthGrowthPerLevel = 1.1f;

        [Header("분리 (적끼리 겹치지 않기)")]
        [Tooltip("추격 방향 대비 밀어내는 힘의 배율. 클수록 덜 뭉치지만 플레이어에게 덜 다가간다")]
        [SerializeField] private float _separationStrength = 1.5f;

        [Tooltip("한 적이 검사하는 이웃 수 상한. 밀집 구간의 최악 비용을 자른다")]
        [SerializeField] private int _separationMaxNeighbors = 16;

        [Header("기타")]
        [Tooltip("난수 시드. 고정하면 매 실행 같은 배치가 나와 성능 비교가 가능하다.")]
        [SerializeField] private uint _randomSeed = 1;

        private sealed class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
        {
            public override void Bake(EnemySpawnerAuthoring authoring)
            {
                if (authoring._enemyPrefab == null)
                {
                    return;
                }

                // 스포너 자체는 그릴 게 없으므로 트랜스폼을 붙이지 않는다.
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new EnemySpawner
                {
                    Prefab = GetEntity(authoring._enemyPrefab, TransformUsageFlags.Dynamic),
                    PoolSize = authoring._poolSize,
                    RingMinRadius = authoring._respawnMinRadius,
                    RingMaxRadius = authoring._respawnMaxRadius,
                    RandomSeed = authoring._randomSeed == 0 ? 1u : authoring._randomSeed,
                });

                AddComponent(entity, new SpawnDirector
                {
                    BaseBudget = authoring._baseBudget,
                    GrowthPerMinute = authoring._growthPerMinute,
                    TestTimeScale = Mathf.Max(0f, authoring._testTimeScale),
                    HealthGrowthPerLevel = Mathf.Max(1f, authoring._healthGrowthPerLevel),
                    FixedTarget = Mathf.Max(0, authoring._benchmarkFixedTarget),
                });

                // 적 관련 전역 설정이라 스포너에 함께 둔다. 씬 오브젝트를 하나 더 만들 필요가 없다.
                AddComponent(entity, new EnemySeparation
                {
                    Strength = authoring._separationStrength,
                    MaxNeighbors = authoring._separationMaxNeighbors,
                });
            }
        }
    }
}
