using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 파편탄 (기획서 5.1 의 1번) — 가까운 적들에게 자동 발사.
    /// 플레이어 엔티티에 붙는다. 수치는 기획서에 없어 M1 임시값이며 인스펙터에서 조정한다.
    ///
    /// 한 번 발사(volley) = <see cref="TargetCount"/> × <see cref="ProjectilesPerTarget"/> 발.
    /// 가까운 적 T 마리를 각각 노리고, 타겟마다 P 발을 <see cref="SpreadDegrees"/> 부채꼴로 퍼뜨린다.
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

        /// <summary>한 번에 노리는 적 수. 사거리 안 적이 더 적으면 가까운 적부터 다시 노린다.</summary>
        public int TargetCount;

        /// <summary>타겟 하나당 발 수.</summary>
        public int ProjectilesPerTarget;

        /// <summary>타겟당 발들이 퍼지는 전체 각도 (도). 1 발이면 무시된다.</summary>
        public float SpreadDegrees;

        /// <summary>투사체가 추가로 뚫고 지나갈 수 있는 적 수. 0 이면 첫 명중에서 사라진다.</summary>
        public int Pierce;

        /// <summary>명중 시 폭발 반경. 0 이면 폭발 없음 (레벨업으로 해금).</summary>
        public float ExplosionRadius;

        /// <summary>폭발 피해 = 발당 피해 × 이 비율. 직격당한 적은 폭발 피해를 추가로 받지 않는다.</summary>
        public float ExplosionDamageRatio;
    }
}
