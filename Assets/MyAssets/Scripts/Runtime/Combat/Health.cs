using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 체력. 풀에서 재활성화할 때 <see cref="Max"/> 로 되돌리기 위해 최대치를 함께 든다.
    /// 8 byte — 기획서 8.4 의 적 1 개당 64 byte 예산에 포함된 항목이다.
    /// </summary>
    public struct Health : IComponentData
    {
        public float Current;
        public float Max;
    }
}
