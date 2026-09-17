using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 파편탄 (기획서 5.1 의 1번) — 최근접 적에게 자동 발사.
    /// 플레이어 엔티티에 붙는다. 수치는 기획서에 없어 M1 임시값이며 인스펙터에서 조정한다.
    /// </summary>
    public struct ShardWeapon : IComponentData
    {
        /// <summary>발사 간격 (초).</summary>
        public float Interval;

        /// <summary>다음 발사까지 남은 시간 (초).</summary>
        public float Cooldown;

        /// <summary>탄속 (월드 유닛/초).</summary>
        public float ProjectileSpeed;

        /// <summary>발당 피해.</summary>
        public float Damage;

        /// <summary>투사체 수명 (초). 탄속 × 수명 = 사거리.</summary>
        public float ProjectileLifetime;
    }
}
