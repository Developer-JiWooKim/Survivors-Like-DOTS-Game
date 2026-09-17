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
    }
}
