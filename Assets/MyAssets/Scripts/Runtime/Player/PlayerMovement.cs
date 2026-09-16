using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Player
{
    /// <summary>
    /// 플레이어 이동 능력치. 이 컴포넌트를 가진 엔티티가 곧 플레이어다.
    ///
    /// 별도의 PlayerTag 를 두지 않는 이유:
    /// 태그 컴포넌트를 따로 만들면 아키타입에 필드 없는 타입이 하나 더 붙는다.
    /// 플레이어는 어차피 이 컴포넌트를 반드시 가지므로 이걸로 식별하면 충분하다.
    /// </summary>
    public struct PlayerMovement : IComponentData
    {
        /// <summary>초당 이동 거리 (월드 유닛).</summary>
        public float Speed;
    }
}
