using Unity.Entities;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// 맵 크기와 생성 규칙. <see cref="TerrainGridSystem"/> 이 시작 시 한 번 읽는다.
    ///
    /// 여기 있는 비율·임계값은 **기획서에 없는 M3 임시값**이다. 기획서 4.2 는 "풀·물·바위는 맵 생성"
    /// 이라고만 정했고 얼마나 깔지는 정하지 않았다. 눈으로 보며 맞출 값이라 Authoring 에 노출해 뒀다.
    /// </summary>
    public struct TerrainSettings : IComponentData
    {
        public int Width;
        public int Height;

        /// <summary>맵 생성 난수 시드. 같은 시드 → 같은 맵.</summary>
        public uint Seed;

        /// <summary>노이즈 임계값. 클수록 그 지형이 적게 깔린다 (노이즈는 -1~1).</summary>
        public float RockThreshold;

        public float WaterThreshold;
        public float GrassThreshold;

        /// <summary>노이즈 주파수. 작을수록 덩어리가 커진다.</summary>
        public float RockFrequency;

        public float WaterFrequency;
        public float GrassFrequency;

        /// <summary>맵 중앙(플레이어 시작 지점) 이 반경 안에는 바위·물을 깔지 않는다. 시작하자마자 끼이는 걸 막는다.</summary>
        public float SpawnClearRadius;

        /// <summary>풀 타일의 초기 연료. 이만큼의 틱(10Hz) 동안 탄다 — 120 = 12 초.</summary>
        public byte GrassFuel;
    }
}
