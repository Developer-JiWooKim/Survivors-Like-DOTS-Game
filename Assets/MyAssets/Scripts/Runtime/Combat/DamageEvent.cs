using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 한 번의 피해. 병렬 잡이 <c>NativeStream</c> 에 쌓고 <see cref="DamageApplySystem"/> 이 단일 스레드로 적용한다.
    /// 속성(Element) 필드는 지형 연계가 들어오는 M3 에서 추가한다.
    /// </summary>
    public struct DamageEvent
    {
        public Entity Target;
        public float Amount;
    }
}
