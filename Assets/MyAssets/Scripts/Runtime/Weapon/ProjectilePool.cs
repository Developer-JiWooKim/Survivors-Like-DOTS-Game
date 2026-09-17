using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Weapon
{
    /// <summary>
    /// 투사체 풀 설정 싱글턴. 시작 시 <see cref="Capacity"/> 개를 미리 만들어 전부 꺼둔다.
    /// </summary>
    public struct ProjectilePool : IComponentData
    {
        public Entity Prefab;
        public int Capacity;
    }
}
