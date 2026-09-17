using Unity.Entities;
using Random = Unity.Mathematics.Random;

namespace Assets.MyAssets.Scripts.Runtime.Leveling
{
    /// <summary>
    /// 레벨업 3지선다 상태 싱글턴. ECS 와 UI(uGUI) 가 주고받는 유일한 창구다.
    ///
    /// 흐름:
    /// 1. <see cref="LevelUpSystem"/> 이 선택지를 채우고 <see cref="IsOpen"/> = true, 런을 일시정지
    /// 2. UI 가 <see cref="IsOpen"/> 을 보고 패널을 띄우고, 클릭하면 <see cref="SelectedIndex"/> 에 기록
    /// 3. <see cref="LevelUpApplySystem"/> 이 강화를 적용하고 닫은 뒤 런을 재개
    ///
    /// UI 가 플레이어 컴포넌트를 직접 고치지 않는 이유: 쓰기를 ECS 시스템 한 곳에 모아야
    /// 잡과의 동기화·적용 순서를 한 곳에서 통제할 수 있다. UI 는 "무엇을 골랐나" 만 남긴다.
    /// </summary>
    public struct LevelUpState : IComponentData
    {
        public const int OptionCount = 3;
        public const int NoSelection = -1;

        public bool IsOpen;
        public UpgradeType Option0;
        public UpgradeType Option1;
        public UpgradeType Option2;

        /// <summary>UI 가 고른 선택지 번호 (0~2). 아직 고르지 않았으면 <see cref="NoSelection"/>.</summary>
        public int SelectedIndex;

        public Random Random;

        public UpgradeType GetOption(int index)
        {
            return index switch
            {
                0 => Option0,
                1 => Option1,
                _ => Option2,
            };
        }
    }
}
