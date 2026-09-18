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
    }
}
