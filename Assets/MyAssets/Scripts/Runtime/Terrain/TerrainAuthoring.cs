using Unity.Entities;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Runtime.Terrain
{
    /// <summary>
    /// SubScene 에 배치하는 지형 설정 + 타일 렌더 풀.
    /// 맵 크기와 생성 규칙은 눈으로 보며 맞출 값이라 전부 인스펙터에 노출한다 (기획서에 없는 M3 임시값).
    /// </summary>
    public sealed class TerrainAuthoring : MonoBehaviour
    {
        [Header("맵 (기획서 4.1 — 512×512, 1u = 1타일)")]
        [Tooltip("가로 타일 수")]
        [SerializeField] private int _width = 512;

        [Tooltip("세로 타일 수")]
        [SerializeField] private int _height = 512;

        [Tooltip("맵 생성 시드. 같은 시드면 같은 맵이 나온다")]
        [SerializeField] private uint _seed = 1234;

        [Header("지형 분포 (노이즈 -1~1 기준 임계값. 클수록 적게 깔린다)")]
        [Tooltip("바위 임계값")]
        [Range(-1f, 1f)]
        [SerializeField] private float _rockThreshold = 0.62f;

        [Tooltip("물 임계값")]
        [Range(-1f, 1f)]
        [SerializeField] private float _waterThreshold = 0.55f;

        [Tooltip("풀 임계값")]
        [Range(-1f, 1f)]
        [SerializeField] private float _grassThreshold = 0.05f;

        [Header("지형 덩어리 크기 (노이즈 주파수. 작을수록 덩어리가 커진다)")]
        [SerializeField] private float _rockFrequency = 0.06f;
        [SerializeField] private float _waterFrequency = 0.04f;
        [SerializeField] private float _grassFrequency = 0.03f;

        [Tooltip("맵 중앙(플레이어 시작 지점) 이 반경 안에는 바위·물을 깔지 않는다")]
        [SerializeField] private float _spawnClearRadius = 12f;

        [Tooltip("풀 타일의 초기 연료. 10Hz 틱이므로 120 = 12초 동안 탄다")]
        [Range(0, 255)]
        [SerializeField] private int _grassFuel = 120;

        [Header("상태 전파 (기획서 4.3)")]
        [Tooltip("틱 간격(초). 기획서 4.3 — 10Hz = 0.1")]
        [SerializeField] private float _tickInterval = 0.1f;

        [Tooltip("불타는 이웃 하나가 풀에 옮겨붙일 틱당 확률. 기획서에 없는 M3 임시값")]
        [Range(0f, 1f)]
        [SerializeField] private float _igniteChance = 0.12f;

        [Tooltip("기름의 확산 확률 배수. 기획서 4.3 — 3배")]
        [SerializeField] private float _oilSpreadMultiplier = 3f;

        [Header("타일 렌더 풀")]
        [Tooltip("TileViewAuthoring 이 붙은 프리팹 에셋 (씬 안의 오브젝트 아님). 비우면 지형이 보이지 않는다")]
        [SerializeField] private GameObject _tilePrefab;

        [Tooltip("한 번에 그리는 창의 가로 타일 수. 카메라 시야(~30u)보다 넉넉해야 가장자리가 비지 않는다")]
        [SerializeField] private int _viewWidth = 44;

        [Tooltip("한 번에 그리는 창의 세로 타일 수. 카메라 시야(~17u)보다 넉넉하게")]
        [SerializeField] private int _viewHeight = 28;

        [Tooltip("타일 쿼드의 z. 적·플레이어(z=0)보다 뒤에 그려지도록 양수")]
        [SerializeField] private float _depth = 1f;

        private sealed class TerrainBaker : Baker<TerrainAuthoring>
        {
            public override void Bake(TerrainAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new TerrainSettings
                {
                    Width = Mathf.Max(1, authoring._width),
                    Height = Mathf.Max(1, authoring._height),
                    Seed = authoring._seed,
                    RockThreshold = authoring._rockThreshold,
                    WaterThreshold = authoring._waterThreshold,
                    GrassThreshold = authoring._grassThreshold,
                    RockFrequency = authoring._rockFrequency,
                    WaterFrequency = authoring._waterFrequency,
                    GrassFrequency = authoring._grassFrequency,
                    SpawnClearRadius = authoring._spawnClearRadius,
                    GrassFuel = (byte)authoring._grassFuel,
                });

                AddComponent(entity, new TerrainTickSettings
                {
                    TickInterval = authoring._tickInterval,
                    IgniteChance = authoring._igniteChance,
                    OilSpreadMultiplier = authoring._oilSpreadMultiplier,
                });

                // 프리팹이 없으면 렌더를 끈 것으로 본다 (창 0×0). 싱글턴은 항상 둬서 조회 쪽 분기를 없앤다
                // — XpGemPoolAuthoring 의 자석 처리와 같은 방식.
                bool hasPrefab = authoring._tilePrefab != null;
                AddComponent(entity, new TileRenderPool
                {
                    Prefab = hasPrefab ? GetEntity(authoring._tilePrefab, TransformUsageFlags.Dynamic) : Entity.Null,
                    ViewWidth = hasPrefab ? Mathf.Max(0, authoring._viewWidth) : 0,
                    ViewHeight = hasPrefab ? Mathf.Max(0, authoring._viewHeight) : 0,
                    Depth = authoring._depth,
                });
            }
        }
    }
}
