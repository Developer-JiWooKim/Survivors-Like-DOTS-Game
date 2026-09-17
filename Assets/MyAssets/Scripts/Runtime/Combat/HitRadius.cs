using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Combat
{
    /// <summary>
    /// 원-원 겹침 판정용 반경 (월드 유닛).
    ///
    /// 렌더 스케일에서 유도하지 않고 따로 두는 이유:
    /// 보이는 크기와 맞는 크기는 게임 감각상 일부러 다르게 잡는 경우가 많다
    /// (플레이어 판정은 작게, 적 판정은 약간 크게).
    /// M4 에서 적 타입이 늘면 Blob 스탯 테이블로 옮긴다 (기획서 8.3).
    /// </summary>
    public struct HitRadius : IComponentData
    {
        public float Value;
    }
}
