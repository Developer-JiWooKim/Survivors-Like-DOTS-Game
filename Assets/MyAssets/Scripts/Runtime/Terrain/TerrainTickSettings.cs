using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 지형 상태 전파 수치. 기획서 4.3 은 틱 주기(10Hz)와 "기름은 3 배" 만 정했고
    /// 실제 확률은 정하지 않았다 — 나머지는 **M3 임시값**이며 눈으로 보며 맞춘다.
    /// </summary>
    public struct TerrainTickSettings : IComponentData
    {
        /// <summary>틱 간격(초). 기획서 4.3 — 10Hz = 0.1.</summary>
        public float TickInterval;

        /// <summary>불타는 이웃 하나가 풀에 옮겨붙일 틱당 확률.</summary>
        public float IgniteChance;

        /// <summary>기름의 확산 확률 배수. 기획서 4.3 — 3 배.</summary>
        public float OilSpreadMultiplier;

        /// <summary>감전 지속 틱. 기획서 4.3 — 0.5 초 = 10Hz 에서 5 틱.</summary>
        public byte ShockDurationTicks;

        /// <summary>빙판 지속 틱. 기획서 4.2 — 15 초 후 물로 해동 = 150 틱.</summary>
        public byte IceDurationTicks;

        /// <summary>
        /// 감전 한 번이 훑을 수 있는 최대 칸 수. flood fill 비용이 물 덩어리 크기에 비례하는 것을 막는다.
        ///
        /// **성능 장치이자 밸런스 장치다.** 상한이 없으면 맵 절반이 물인 구역에서 전격 한 발로
        /// 화면을 청소할 수 있다 — 기획서 4.4 가 막으려는 종류의 일이다.
        /// </summary>
        public int ShockMaxCells;

        /// <summary>연소 타일 위 유닛이 받는 초당 피해. 기획서 4.4 는 "플레이어에게도 피해" 만 정했고 수치는 M3 임시값.</summary>
        public float BurnDamagePerSecond;

        /// <summary>감전 타일 위 유닛이 받는 초당 피해 (기획서 4.3 — "초당 피해"). 수치는 M3 임시값.</summary>
        public float ShockDamagePerSecond;

        /// <summary>기름이 탈 때의 피해 배수. 기획서 4.3 — 2 배.</summary>
        public float OilDamageMultiplier;
    }
}
