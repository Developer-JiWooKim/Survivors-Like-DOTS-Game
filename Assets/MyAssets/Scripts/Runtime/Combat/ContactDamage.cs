using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 플레이어와 겹쳐 있는 동안 초당 입히는 접촉 피해.
    /// 적 타입마다 다른 값이라 적 엔티티에 둔다. M4 에서 Blob 스탯 테이블로 옮긴다 (기획서 8.3).
    /// </summary>
    public struct ContactDamage : IComponentData
    {
        public float PerSecond;
    }
}
