namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 타일 종류 (기획서 4.2). <see cref="TileData.Type"/> 에 byte 로 들어간다.
    ///
    /// 왜 enum 을 byte 로 고정하나:
    /// 타일 1 개가 4 byte 를 넘으면 512×512 그리드가 1 MB 를 넘고, 확산 잡이 읽는 캐시 라인당
    /// 타일 수가 줄어 10Hz 틱 비용이 바로 올라간다. 기획서 4.1 의 4 byte 예산은 그 이유로 지킨다.
    /// </summary>
    public enum TileType : byte
    {
        Dirt = 0,
        Grass = 1,
        Water = 2,
        Oil = 3,
        Ice = 4,
        Crack = 5,
        Rock = 6,

        /// <summary>
        /// 물 위에 뜬 기름 (2026-09-18 결정 — [ISSUE-012](../../../../../Docs/IssueLog.md)).
        ///
        /// 왜 별도 종류인가:
        /// 기름을 칠하면 타일을 덮어쓰기 때문에 "원래 물이었다" 는 정보가 사라지고, 다 타면
        /// 기획서 4.3 대로 흙이 되어 **물이 영구히 없어진다.** 물은 방화선이자 감전 매개이고
        /// 빙판의 재료라, 기름 한 번으로 호수를 지울 수 있으면 지형 소모가 최적 전략이 된다.
        ///
        /// <see cref="TileData"/> 의 4 byte 예산(기획서 4.1)에는 "원래 무엇이었나" 를 담을 자리가 없다.
        /// Type 값 하나를 더 쓰는 것으로 같은 정보를 공짜로 표현한다 — 다 타면 <see cref="Water"/> 로 돌아간다.
        /// </summary>
        OilOnWater = 7,
    }

    /// <summary>타일 종류를 묶어서 묻는 질문들. 종류가 늘 때 분기를 한 곳에서만 고치기 위한 것.</summary>
    public static class TileTypes
    {
        /// <summary>기름으로 취급하는가 — 확산 확률 3 배(기획서 4.3)가 붙는 종류.</summary>
        public static bool IsOil(TileType type)
        {
            return type == TileType.Oil || type == TileType.OilOnWater;
        }

        /// <summary>
        /// 물로 취급하는가 — 감전 flood fill(기획서 4.3)과 동결(물 → 빙판)의 대상.
        /// 기름이 떠 있어도 아래는 물이므로 전격·냉기 콤보는 그대로 성립해야 한다.
        /// </summary>
        public static bool IsWaterLike(TileType type)
        {
            return type == TileType.Water || type == TileType.OilOnWater;
        }
    }

    /// <summary>
    /// 타일이 지금 어떤 상태에 걸려 있는지 (기획서 4.1 의 <c>State</c> 비트).
    /// 한 타일이 여러 상태에 동시에 걸릴 수 있어 enum 이 아니라 비트 플래그다.
    /// (예: 물에 독이 퍼진 뒤 감전 — 오염 + 감전)
    /// </summary>
    [System.Flags]
    public enum TileStateFlags : byte
    {
        None = 0,
        Burning = 1,
        Shocked = 2,
        Poisoned = 4,
        Frozen = 8,
    }

    /// <summary>
    /// 타일 1 칸. 기획서 4.1 대로 **정확히 4 byte**.
    ///
    /// 왜 이 크기에 집착하나:
    /// 512×512 = 262,144 칸이라 1 byte 늘 때마다 그리드가 256 KB 씩 커진다. 연소 확산 잡은
    /// 매 틱 그리드를 통째로 훑으므로, 크기가 곧 메모리 대역폭이고 곧 틱 비용이다.
    /// 필드를 늘리고 싶어지면 (예: 타일마다 난수 시드) 그 전에 기획서 4.1 을 고쳐야 한다.
    /// — 2026-09-18 결정: 연소 난수는 시드를 저장하지 않고 (타일 인덱스 + 틱) 해시로 만든다.
    /// </summary>
    public struct TileData
    {
        /// <summary><see cref="TileType"/>.</summary>
        public byte Type;

        /// <summary>연료·잔여량 0~255. 불타는 동안 매 틱 감소하고, 0 이 되면 소화되며 <see cref="TileType.Dirt"/> 가 된다.</summary>
        public byte Fuel;

        /// <summary><see cref="TileStateFlags"/> 비트 조합.</summary>
        public byte State;

        /// <summary>현재 상태의 잔여 틱. 10Hz 틱이므로 255 = 25.5 초가 표현 한계다.</summary>
        public byte Timer;

        public TileType TypeValue => (TileType)Type;

        public TileStateFlags StateValue => (TileStateFlags)State;

        public bool HasState(TileStateFlags flag) => (State & (byte)flag) != 0;

        public static TileData Of(TileType type, byte fuel = 0)
        {
            return new TileData
            {
                Type = (byte)type,
                Fuel = fuel,
                State = (byte)TileStateFlags.None,
                Timer = 0,
            };
        }
    }
}
