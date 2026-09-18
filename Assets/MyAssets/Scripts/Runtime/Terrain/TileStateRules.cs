namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 시간이 지나면 풀리는 상태(감전·동결·오염)의 전진 규칙 (기획서 4.3).
    /// <see cref="BurnRules"/> 와 같은 이유로 순수 함수만 둔다 — EditMode 테스트로 검증하기 위해서.
    ///
    /// **연소만 <see cref="TileData.Timer"/> 를 쓰지 않는다.** 연소의 시계는 <see cref="TileData.Fuel"/> 이다
    /// (기획서 4.3 — 매 틱 Fuel--, 0 이면 소화). 그래서 불타는 타일도 Timer 를 감전·동결에 그대로 쓸 수 있다.
    ///
    /// **Timer 가 하나뿐이라는 제약** (기획서 4.1 의 4 byte 예산):
    /// 지금은 문제가 되지 않는다. 감전은 물 타일에, 동결은 그 물을 **빙판으로 바꿔** 걸리므로
    /// 두 상태가 같은 타일에 동시에 존재할 수 없다(<see cref="Freeze"/> 가 감전을 지운다).
    /// 오염(독 안개, M4)이 들어오면 감전과 겹칠 수 있어 그때 Timer 를 어떻게 나눌지 정해야 한다.
    /// </summary>
    public static class TileStateRules
    {
        /// <summary>이 타일이 <see cref="TileData.Timer"/> 를 쓰는 상태에 걸려 있는가.</summary>
        public static bool HasTimedState(in TileData tile)
        {
            const byte Timed = (byte)(TileStateFlags.Shocked | TileStateFlags.Poisoned);
            return (tile.State & Timed) != 0 || tile.TypeValue == TileType.Ice;
        }

        /// <summary>
        /// 감전시킨다. 기획서 4.3 — 지속 0.5 초.
        /// 호출자(<see cref="TerrainEffectSystem"/>)가 물 덩어리인지 이미 판단한 뒤 부른다.
        /// </summary>
        public static TileData Shock(in TileData tile, byte durationTicks)
        {
            TileData next = tile;
            next.State |= (byte)TileStateFlags.Shocked;

            // 이미 감전 중이면 남은 시간을 **덮어쓴다**(연장). 연쇄 낙뢰가 같은 웅덩이를 여러 번 때릴 때
            // 짧은 쪽으로 깎이면 "더 때렸는데 더 빨리 풀린다" 가 된다.
            next.Timer = durationTicks > tile.Timer ? durationTicks : tile.Timer;
            return next;
        }

        /// <summary>
        /// 얼린다. 기획서 4.2·4.3 — 물 + 냉기 → 빙판, 15 초 후 물로 해동.
        /// </summary>
        public static TileData Freeze(in TileData tile, byte durationTicks)
        {
            TileData next = tile;
            next.Type = (byte)TileType.Ice;
            next.Timer = durationTicks;

            // 얼면 물이 아니게 되므로 감전은 풀린다. Timer 가 하나뿐이라 겹칠 수도 없다 (클래스 주석 참조).
            next.State &= unchecked((byte)~(byte)TileStateFlags.Shocked);
            return next;
        }

        /// <summary>
        /// 타이머를 한 틱 전진시킨다. 0 이 되면 걸려 있던 상태를 푼다.
        /// </summary>
        public static TileData StepTimer(in TileData tile)
        {
            TileData next = tile;

            if (!HasTimedState(tile) || next.Timer == 0)
            {
                return next;
            }

            next.Timer--;
            if (next.Timer > 0)
            {
                return next;
            }

            // 빙판은 물로 돌아간다 (기획서 4.2). 나머지는 상태 비트만 푼다.
            if (next.TypeValue == TileType.Ice)
            {
                next.Type = (byte)TileType.Water;
            }

            next.State &= unchecked((byte)~(byte)(TileStateFlags.Shocked | TileStateFlags.Poisoned));
            return next;
        }
    }
}
