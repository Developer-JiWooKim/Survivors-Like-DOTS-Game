namespace Assets.MyAssets.Scripts.Runtime.Leveling
{
    /// <summary>
    /// M1 레벨업 선택지. 무기·패시브 체계(기획서 5장)가 들어오는 M3 에서 교체한다.
    /// 수치는 <see cref="UpgradeTable"/> 에 모아 둔다.
    /// </summary>
    public enum UpgradeType : byte
    {
        ShardDamage,
        ShardFireRate,
        ShardProjectileSpeed,
        MoveSpeed,
        MaxHealth,
    }

    /// <summary>
    /// 강화 수치. 기획서에 없는 M1 임시값 (사용자 승인).
    /// </summary>
    public static class UpgradeTable
    {
        public const int Count = 5;

        /// <summary>파편탄 피해 배율 (+20%).</summary>
        public const float ShardDamageMultiplier = 1.2f;

        /// <summary>발사 간격 배율 (×0.85 = 연사 약 +18%).</summary>
        public const float ShardIntervalMultiplier = 0.85f;

        /// <summary>탄속 배율 (+20%). 수명은 그대로라 사거리도 함께 늘어난다.</summary>
        public const float ShardSpeedMultiplier = 1.2f;

        /// <summary>플레이어 이동속도 배율 (+10%).</summary>
        public const float MoveSpeedMultiplier = 1.1f;

        /// <summary>최대 체력 증가량. 늘어난 만큼 현재 체력도 회복한다.</summary>
        public const float MaxHealthBonus = 20f;
    }
}
