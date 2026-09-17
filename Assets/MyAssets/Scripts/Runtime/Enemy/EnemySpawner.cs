using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적 풀 + 스폰 링 설정 싱글턴.
    ///
    /// 왜 프리팹 에셋을 참조하나 (씬 안의 GameObject 가 아니라):
    /// Baker 가 **프리팹 에셋**을 <c>GetEntity</c> 로 변환하면 그 엔티티에 <c>Prefab</c> 컴포넌트가 붙어
    /// 모든 쿼리에서 자동으로 제외된다. 씬 안의 오브젝트를 원본으로 쓰면 그 원본 자체도
    /// 살아있는 적으로 취급돼 같이 플레이어를 쫓아온다.
    ///
    /// 시작 시 <see cref="PoolSize"/> 만큼 **전부 꺼진 채로** 만들고 (기획서 6.2 "엔티티 풀 20,000"),
    /// <see cref="EnemySpawnDirectorSystem"/> 이 <see cref="SpawnDirector"/> 의 목표 수만큼 켠다.
    /// </summary>
    public struct EnemySpawner : IComponentData
    {
        /// <summary>복제할 적 프리팹 엔티티.</summary>
        public Entity Prefab;

        /// <summary>미리 만들어 둘 적 수 = 동시에 살아있을 수 있는 최대 수.</summary>
        public int PoolSize;

        /// <summary>스폰 링의 안쪽 반경. **플레이어** 기준이며 화면 밖이어야 한다.</summary>
        public float RingMinRadius;

        /// <summary>스폰 링의 바깥 반경.</summary>
        public float RingMaxRadius;

        /// <summary>
        /// 리사이클 거리 (기획서 6.2). 플레이어에게서 이보다 멀어진 적은 죽이지 않고 **반대편 링**으로 옮긴다.
        /// 기획서는 35 (링 22~26 기준, 간격 9). 링을 32~36 으로 잡았으므로 같은 간격을 유지해 45 (2026-09-17 결정).
        /// 링 바깥 반경보다 작으면 스폰 직후 곧바로 리사이클되므로 반드시 더 커야 한다.
        /// </summary>
        public float RecycleDistance;

        /// <summary>난수 시드. 고정하면 매 실행 같은 배치가 나와 성능 비교가 가능하다.</summary>
        public uint RandomSeed;
    }

    /// <summary>
    /// 시간 기반 예산 스폰 (기획서 6.2): <c>목표 수 = BaseBudget × Growth^(경과 분)</c>.
    /// 20 분 시점 약 12,000 (기획서 6.3 난이도 곡선).
    /// </summary>
    public struct SpawnDirector : IComponentData
    {
        /// <summary>0 분 시점 목표 수. 기획서 50.</summary>
        public float BaseBudget;

        /// <summary>분당 배율. 기획서 1.28.</summary>
        public float GrowthPerMinute;

        /// <summary>
        /// **테스트 전용** 시간 배율. 목표 수 계산에만 곱한다 (이동·발사 등 게임 속도는 그대로).
        /// 10 이면 실제 2 분에 20 분 시점의 적 수가 된다. 기본 1.
        /// </summary>
        public float TestTimeScale;

        /// <summary>
        /// 플레이어 레벨당 스폰 체력 배율: <c>체력 = 프리팹 체력 × HealthGrowthPerLevel^(Lv − 1)</c>.
        /// 기획서에 없는 규칙 (2026-09-17 사용자 결정, ×1.1). 새로 스폰되는 적에만 적용된다.
        /// </summary>
        public float HealthGrowthPerLevel;

        /// <summary>이번 프레임 목표 수 (표시용, 시스템이 채움).</summary>
        public int Target;

        /// <summary>이번 프레임 시작 시점 살아있는 적 수 (표시용, 시스템이 채움).</summary>
        public int Alive;

        /// <summary>지금 스폰되는 적의 최대 체력 (표시용, 시스템이 채움).</summary>
        public float SpawnHealth;

        public float HealthAt(float baseHealth, int playerLevel)
        {
            return baseHealth * math.pow(HealthGrowthPerLevel, math.max(playerLevel - 1, 0));
        }

        /// <summary>
        /// **벤치마크 전용** 고정 목표 수. 0 보다 크면 시간과 무관하게 이 수를 유지한다.
        /// 시간 기반 예산은 계속 늘어나 같은 조건으로 반복 측정하기 어렵기 때문 (M1 게이트 1,000 / M2 게이트 10,000).
        /// </summary>
        public int FixedTarget;

        /// <summary>
        /// 벤치마크 모드. 고정 목표 수가 켜져 있으면 함께 켜진다 — 옵션을 하나로 묶어 실수로 한쪽만 켜 두는 일을 막는다.
        /// 켜지면 플레이어가 죽지 않고(체력 바닥 시 최대치로 복구), 레벨업 일시정지가 뜨지 않는다.
        /// 전투·사망·젬·재스폰은 그대로 돌아 실제 플레이에 가까운 부하로 잰다.
        /// </summary>
        public bool IsBenchmark => FixedTarget > 0;

        public int BudgetAt(float elapsedSeconds)
        {
            if (FixedTarget > 0)
            {
                return FixedTarget;
            }

            float minutes = elapsedSeconds * TestTimeScale / 60f;
            return (int)(BaseBudget * math.pow(GrowthPerMinute, minutes));
        }
    }
}
