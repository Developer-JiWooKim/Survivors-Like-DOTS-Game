using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// 자석 아이템 태그. 주우면 필드의 모든 젬이 플레이어에게 끌려온다 (2026-09-17 결정).
    /// 풀링되며 <c>Active</c> 로 생존을 표시한다.
    /// </summary>
    public struct MagnetPickup : IComponentData
    {
    }

    /// <summary>
    /// 자석 드랍 설정 싱글턴. 기획서에 없는 M1 임시값.
    /// </summary>
    public struct MagnetDropSettings : IComponentData
    {
        public Entity Prefab;

        /// <summary>동시에 바닥에 있을 수 있는 자석 수. 가득 차면 새 자석은 떨어지지 않는다.</summary>
        public int PoolSize;

        /// <summary>적 1 마리가 죽을 때 자석을 떨어뜨릴 확률 (0~1).</summary>
        public float DropChance;
    }
}
