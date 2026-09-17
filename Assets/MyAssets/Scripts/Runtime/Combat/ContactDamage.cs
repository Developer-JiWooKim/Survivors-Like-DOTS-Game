using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 플레이어에 닿았을 때 입히는 1 회 피해 (기획서 3 장 "적 접촉 시 데미지").
    /// 적 타입마다 다른 값이라 적 엔티티에 둔다. M4 에서 Blob 스탯 테이블로 옮긴다 (기획서 8.3).
    /// </summary>
    public struct ContactDamage : IComponentData
    {
        public float Damage;
    }
}
