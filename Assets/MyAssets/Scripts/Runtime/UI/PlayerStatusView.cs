using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.UI
{
    /// <summary>
    /// UI 가 읽는 플레이어 상태 사본 싱글턴. <see cref="PlayerStatusViewSystem"/> 이 프레임 시작에 채운다.
    ///
    /// 왜 UI 가 Health 를 직접 읽지 않나:
    /// Health 는 피해 적용 잡이 쓴다. MonoBehaviour 에서 읽으면 그 잡(과 앞선 이동·충돌 잡 체인)이
    /// 끝날 때까지 메인 스레드가 기다린다. 이 사본은 메인 스레드에서만 쓰므로 기다릴 것이 없다.
    /// 대가는 1 프레임 늦은 표시 — HUD 에서는 체감되지 않는다.
    /// </summary>
    public struct PlayerStatusView : IComponentData
    {
        public bool HasPlayer;
        public float Health;
        public float MaxHealth;
        public int Level;
        public int Xp;
        public int XpToNext;
    }
}
