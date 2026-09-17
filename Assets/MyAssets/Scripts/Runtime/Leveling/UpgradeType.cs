using Assets.MyAssets.Scripts.Runtime.Weapon;

namespace Assets.MyAssets.Scripts.Runtime.Leveling
{
    /// <summary>
    /// M1 레벨업 선택지. 무기·패시브 체계(기획서 5장)가 들어오는 M3 에서 교체한다.
    /// 수치는 <see cref="UpgradeTable"/> 에 모아 둔다.
    /// </summary>
    public enum UpgradeType : byte
    {
        // 수치 강화
        ShardDamage,
        ShardFireRate,
        ShardProjectileSpeed,
        MoveSpeed,
        MaxHealth,

        // 발사 형태 강화 (2026-09-17 추가 — "화력을 퍼붓는 맛")
        TargetCount,
        ProjectileCount,
        Spread,
        Pierce,
        Explosion,
    }

    /// <summary>
    /// 강화 수치. 기획서에 없는 M1 임시값 (사용자 승인).
    /// </summary>
    public static class UpgradeTable
    {
        public const int Count = 10;

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

        public const int MaxTargetCount = 12;
        public const int MaxProjectilesPerTarget = 7;

        public const float SpreadStepDegrees = 15f;
        public const float MaxSpreadDegrees = 90f;

        /// <summary>관통 상한. <c>Projectile.HitHistory</c>(15 칸)에 여유를 두고 정했다.</summary>
        public const int MaxPierce = 10;

        /// <summary>폭발을 처음 고르면 이 반경으로 해금된다.</summary>
        public const float ExplosionUnlockRadius = 1f;
        public const float ExplosionRadiusStep = 0.5f;

        /// <summary>
        /// 폭발 반경 상한. 반경이 커질수록 조회 셀이 제곱으로 늘어난다 (3.0 → 9×9 = 81 칸).
        /// </summary>
        public const float MaxExplosionRadius = 3f;

        /// <summary>
        /// 이미 최대치라 더 고를 의미가 없는 선택지인지. 레벨업 후보에서 뺀다.
        /// 수치 강화 5 종은 상한이 없어 항상 후보이므로, 후보가 3 개 미만이 되는 일은 없다.
        /// </summary>
        public static bool IsMaxed(UpgradeType upgrade, in ShardWeapon weapon)
        {
            return upgrade switch
            {
                UpgradeType.TargetCount => weapon.TargetCount >= MaxTargetCount,
                UpgradeType.ProjectileCount => weapon.ProjectilesPerTarget >= MaxProjectilesPerTarget,
                UpgradeType.Spread => weapon.SpreadDegrees >= MaxSpreadDegrees,
                UpgradeType.Pierce => weapon.Pierce >= MaxPierce,
                UpgradeType.Explosion => weapon.ExplosionRadius >= MaxExplosionRadius,
                _ => false,
            };
        }
    }
}
