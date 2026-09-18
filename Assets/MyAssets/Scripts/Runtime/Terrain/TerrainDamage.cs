namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 타일 위에 서 있는 유닛이 받는 초당 피해 (기획서 4.3·4.4). 순수 함수만 둔다.
    ///
    /// **연소 피해는 적과 플레이어 모두에게 들어간다** (기획서 4.4). 화면 전체를 불바다로 만들면
    /// 자기 무덤이 되는 것이 이 게임의 균형점이라, 플레이어만 빼면 지형 콤보가 무조건 이득이 된다.
    /// (방화범 캐릭터의 "자기 불에 면역" 은 캐릭터 시스템이 들어오는 M5 에서 예외로 붙인다.)
    /// </summary>
    public static class TerrainDamage
    {
        /// <summary>
        /// 이 타일 위에서 초당 몇의 피해를 받는가. 연소와 감전은 **더해진다** —
        /// 기름 웅덩이에 불이 붙은 채 전격이 들어오는 상황이 기획서 5.4 가 노리는 콤보다.
        /// </summary>
        public static float PerSecondFor(in TileData tile, in TerrainTickSettings settings)
        {
            float damage = 0f;

            if (tile.HasState(TileStateFlags.Burning))
            {
                damage += settings.BurnDamagePerSecond;

                // 기획서 4.3 — 기름은 피해 2 배. 물 위 기름도 타는 것은 기름이므로 같이 적용한다.
                if (TileTypes.IsOil(tile.TypeValue))
                {
                    damage *= settings.OilDamageMultiplier;
                }
            }

            if (tile.HasState(TileStateFlags.Shocked))
            {
                damage += settings.ShockDamagePerSecond;
            }

            return damage;
        }
    }
}
