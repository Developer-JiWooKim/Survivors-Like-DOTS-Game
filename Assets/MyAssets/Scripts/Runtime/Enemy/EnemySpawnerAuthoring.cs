using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// SubScene 에 배치하는 적 스폰 설정. 베이킹 시 <see cref="EnemySpawner"/> 로 변환된다.
    /// </summary>
    public sealed class EnemySpawnerAuthoring : MonoBehaviour
    {
        [Tooltip("적 프리팹 에셋. 씬 안의 오브젝트가 아니라 Project 창의 프리팹을 지정해야 한다.")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("생성할 적 수. M1 목표는 1000.")]
        [SerializeField] private int _count = 1000;

        [Tooltip("스폰 링 안쪽 반경. 시작하자마자 플레이어에 붙지 않게 한다.")]
        [SerializeField] private float _minRadius = 12f;

        [Tooltip("스폰 링 바깥 반경.")]
        [SerializeField] private float _maxRadius = 40f;

        [Tooltip("죽은 적을 되살릴 링의 안쪽 반경 (플레이어 기준). 카메라 size 15, 16:9 화면의 모서리가 약 30.6 이라 그 밖으로 잡았다.")]
        [SerializeField] private float _respawnMinRadius = 32f;

        [Tooltip("재스폰 링의 바깥 반경 (플레이어 기준).")]
        [SerializeField] private float _respawnMaxRadius = 36f;

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
                    Count = authoring._count,
                    MinRadius = authoring._minRadius,
                    MaxRadius = authoring._maxRadius,
                    RespawnMinRadius = authoring._respawnMinRadius,
                    RespawnMaxRadius = authoring._respawnMaxRadius,
                    RandomSeed = authoring._randomSeed == 0 ? 1u : authoring._randomSeed,
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
