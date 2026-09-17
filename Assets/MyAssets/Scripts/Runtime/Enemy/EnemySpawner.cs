using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// M1 적 스폰 설정. 시작할 때 한 번만 사용된다.
    ///
    /// 왜 프리팹 에셋을 참조하나 (씬 안의 GameObject 가 아니라):
    /// Baker 가 **프리팹 에셋**을 <c>GetEntity</c> 로 변환하면 그 엔티티에 <c>Prefab</c> 컴포넌트가 붙어
    /// 모든 쿼리에서 자동으로 제외된다. 씬 안의 오브젝트를 원본으로 쓰면 그 원본 자체도
    /// 살아있는 적으로 취급돼 같이 플레이어를 쫓아온다.
    ///
    /// 시작 시 Count 만큼 일괄 생성하고, 이 엔티티들이 그대로 풀이 된다.
    /// 죽은 적은 파괴하지 않고 <see cref="EnemyRespawnSystem"/> 이 재스폰 링으로 되살린다.
    /// 예산 기반 스폰 디렉터·화면 이탈 리사이클(기획서 6.2)은 M2 이후 작업이다.
    /// </summary>
    public struct EnemySpawner : IComponentData
    {
        /// <summary>복제할 적 프리팹 엔티티.</summary>
        public Entity Prefab;

        /// <summary>생성할 적 수.</summary>
        public int Count;

        /// <summary>스폰 링의 안쪽 반경. 플레이어 바로 위에 생기지 않게 한다.</summary>
        public float MinRadius;

        /// <summary>스폰 링의 바깥 반경.</summary>
        public float MaxRadius;

        /// <summary>죽은 적을 되살릴 링의 안쪽 반경. 원점이 아니라 **플레이어** 기준이다.</summary>
        public float RespawnMinRadius;

        /// <summary>재스폰 링의 바깥 반경.</summary>
        public float RespawnMaxRadius;

        /// <summary>난수 시드. 고정하면 매 실행 같은 배치가 나와 성능 비교가 가능하다.</summary>
        public uint RandomSeed;
    }
}
