using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 바닥에 떨어진 XP 젬 (기획서 7.1). 풀링되며 <c>Active</c> 로 생존을 표시한다.
    /// </summary>
    public struct XpGem : IComponentData
    {
        /// <summary>소형 젬의 경험치. 기획서 7.1 의 소(1)/중(5)/대(20) 중 M1 은 소형만 떨어뜨린다.</summary>
        public const int SmallValue = 1;

        /// <summary>
        /// 이 젬이 주는 경험치. 풀이 가득 찼을 때 다른 젬의 경험치가 합쳐져 커질 수 있다.
        /// 젬 종류(소·중·대)를 따로 두지 않고 값으로 표현한다 — 종류별 모양은 값 구간으로 정하면 된다 (M3 연출).
        /// </summary>
        public int Value;

        /// <summary>자석 반경에 한 번 들어오면 true. 이후에는 반경을 벗어나도 계속 끌려온다 (장르 관례).</summary>
        public bool Attracted;

        /// <summary>현재 흡인 속도. 끌려오는 동안 가속한다.</summary>
        public float Speed;
    }
}
