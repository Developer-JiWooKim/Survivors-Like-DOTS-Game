using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Run
{
    public enum RunPhase : byte
    {
        Playing,

        /// <summary>레벨업 3지선다 등 — 게임플레이만 멈추고 UI 는 계속 돈다.</summary>
        Paused,

        GameOver,
    }

    /// <summary>
    /// 런(한 판)의 진행 상태 싱글턴 (기획서 8.2 의 2번 RunStateSystem).
    /// <see cref="GameplaySystemGroup"/> 이 이 값을 보고 게임플레이 시스템 전체를 돌릴지 정한다.
    /// </summary>
    public struct RunState : IComponentData
    {
        public RunPhase Phase;
    }
}
