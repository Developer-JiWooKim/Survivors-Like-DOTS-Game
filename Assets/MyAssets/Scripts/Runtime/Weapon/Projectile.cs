using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 날아가는 투사체의 상태. 풀에서 꺼낼 때 발사 시스템이 채운다.
    /// </summary>
    public struct Projectile : IComponentData
    {
        /// <summary>초당 이동량 (월드 유닛). 발사 시점에 방향 × 탄속으로 고정된다.</summary>
        public float2 Velocity;

        /// <summary>명중 시 입히는 피해.</summary>
        public float Damage;

        /// <summary>남은 수명 (초). 0 이하가 되면 풀로 돌아간다. 사거리를 시간으로 표현한 것.</summary>
        public float RemainingLifetime;

        /// <summary>앞으로 더 뚫고 지나갈 수 있는 적 수. 명중할 때 0 이면 사라진다.</summary>
        public int PierceRemaining;

        /// <summary>명중 시 폭발 반경. 0 이면 폭발 없음.</summary>
        public float ExplosionRadius;

        /// <summary>폭발에 휘말린 적(직격 대상 제외)이 받는 피해.</summary>
        public float ExplosionDamage;

        /// <summary>
        /// 이미 맞힌 적. 관통하는 탄이 겹쳐 있는 동안 같은 적을 매 프레임 다시 때리지 않게 한다.
        ///
        /// FixedList 인 이유: DynamicBuffer 는 별도 버퍼 헤더와 (넘치면) 힙 할당이 붙는다.
        /// 128 byte 고정 목록은 컴포넌트 안에 그대로 들어가 할당이 없다. 엔티티 15 개까지 담기며,
        /// 최대 관통 10 (+ 직격 1) 을 넉넉히 덮는다. 투사체는 최대 수백 개라 크기 증가는 문제되지 않는다.
        /// </summary>
        public FixedList128Bytes<Entity> HitHistory;
    }
}
