using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Enemy
{
    /// <summary>
    /// 적 이동 능력치. 이 컴포넌트를 가진 엔티티가 곧 적이다.
    ///
    /// 왜 지금은 Blob 스탯 테이블이 아닌가:
    /// 기획서 8.4 는 적 6종을 <c>EnemyTypeId(byte)</c> + Blob 스탯 테이블로 분기하도록 설계했다.
    /// 그건 **타입이 여러 개일 때** 청크 파편화를 막기 위한 구조다.
    /// M1 은 적이 1종뿐이라 Blob 은 구조만 복잡하게 만든다. 적 6종이 들어오는 M4 에서 전환한다.
    /// **단일 아키타입 원칙 자체는 지금도 지켜지고 있다** — 적은 모두 같은 컴포넌트 구성을 가진다.
    /// </summary>
    public struct EnemyMovement : IComponentData
    {
        /// <summary>초당 이동 거리 (월드 유닛).</summary>
        public float Speed;
    }
}
