using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적끼리 겹치지 않게 밀어내는 힘(보이드의 separation) 설정 싱글턴.
    /// 분리 반경은 따로 두지 않는다 — 두 적의 <c>HitRadius</c> 합이 곧 "닿는 거리" 이기 때문이다.
    /// 기획서에 없는 수치라 M1 임시값이며 스포너 인스펙터에서 조정한다.
    /// </summary>
    public struct EnemySeparation : IComponentData
    {
        /// <summary>추격 방향(길이 1) 대비 분리 힘의 배율. 클수록 뭉치지 않지만 플레이어에게 다가가는 힘이 약해진다.</summary>
        public float Strength;

        /// <summary>
        /// 한 적이 검사하는 이웃 수 상한. 밀집 구간에서 한 적이 수십 마리를 훑는 최악의 비용을 자른다.
        /// 가까운 순이 아니라 발견 순이라 정밀도는 약간 떨어지지만, 밀어내는 방향은 충분히 나온다.
        /// </summary>
        public int MaxNeighbors;
    }
}
