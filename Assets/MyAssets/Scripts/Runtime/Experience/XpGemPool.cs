using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Experience
{
    /// <summary>
    /// XP 젬 풀 설정 싱글턴. 시작 시 <see cref="Capacity"/> 개를 만들어 전부 꺼둔다.
    /// </summary>
    public struct XpGemPool : IComponentData
    {
        public Entity Prefab;

        /// <summary>
        /// 동시에 바닥에 있을 수 있는 젬 수. 젬에는 수명이 없어 줍지 않으면 계속 쌓인다.
        /// 가득 차면 새 젬을 만들지 않고 플레이어에게서 가장 먼 젬에 경험치를 합친다.
        /// </summary>
        public int Capacity;
    }

    /// <summary>
    /// 젬 흡인·수집 수치 싱글턴. 기획서에 없는 M1 임시값.
    /// </summary>
    public struct XpCollectSettings : IComponentData
    {
        /// <summary>이 거리 안에 들어온 젬이 플레이어에게 끌려오기 시작한다.</summary>
        public float MagnetRadius;

        /// <summary>이 거리 안에 들어오면 수집된다.</summary>
        public float PickupRadius;

        /// <summary>끌려오기 시작할 때의 속도 (u/s).</summary>
        public float AttractStartSpeed;

        /// <summary>끌려오는 동안의 가속도 (u/s²). 플레이어가 도망쳐도 결국 따라잡게 한다.</summary>
        public float AttractAcceleration;
    }
}
