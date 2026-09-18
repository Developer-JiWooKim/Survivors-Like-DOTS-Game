using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Editor
{
    /// <summary>
    /// 도형 프로토타입용 URP Unlit 머티리얼을 코드로 생성한다.
    ///
    /// 왜 코드로 만드나 (CLAUDE.md 3.4):
    /// 머티리얼은 눈으로 조정할 게 색 정도뿐인 반면, **어떤 셰이더에 GPU Instancing 을 켰는지**가
    /// 성능을 좌우한다. 코드로 두면 그 설정이 명시적으로 남고 재현 가능하다.
    /// 씬 안의 오브젝트 배치는 반대 이유로 사용자가 에디터에서 직접 한다.
    /// </summary>
    public static class UnlitMaterialBuilder
    {
        private const string MaterialDir = "Assets/MyAssets/Materials";

        /// <summary>
        /// 2D 게임이지만 URP "2D Renderer" 가 아니라 Universal Renderer 를 쓴다.
        /// 2D Renderer 의 스프라이트 셰이더는 DOTS 인스턴싱(DOTS_INSTANCING_ON) 변형을
        /// 지원하지 않아 대량 엔티티를 드로우콜 몇 개로 묶을 수 없기 때문이다.
        /// </summary>
        private const string ShaderName = "Universal Render Pipeline/Unlit";

        [MenuItem("Tools/도형 머티리얼 생성")]
        public static void CreateAll()
        {
            Create("PlayerQuad", new Color(0.95f, 0.95f, 0.95f, 1f));
            Create("EnemyQuad", new Color(0.85f, 0.25f, 0.25f, 1f));
            Create("BenchmarkQuad", new Color(0.85f, 0.25f, 0.25f, 1f));

            // 기획서 9장: 투사체 = 작은 흰 도형. 플레이어보다 약간 푸르게 해 겹쳐도 구분되게 한다.
            Create("ProjectileQuad", new Color(0.85f, 0.95f, 1f, 1f));

            // 기획서에 젬 색 지정은 없다. 적(파랑)·투사체(흰색)와 구분되는 녹색.
            Create("XpGemQuad", new Color(0.3f, 0.95f, 0.45f, 1f));

            // 자석 아이템. 젬(녹색)·적(파랑)과 한눈에 구분되는 주황.
            Create("MagnetQuad", new Color(1f, 0.5f, 0.1f, 1f));

            // 지형 타일. 실제 색은 타일마다 URPMaterialPropertyBaseColor 로 덮어쓰므로
            // (TileColors) 여기 색은 프리팹을 인스펙터에서 볼 때의 기본값일 뿐이다.
            // 머티리얼을 타일 종류별로 나누지 않는 이유는 TileViewAuthoring 주석 참조.
            Create("TerrainTileQuad", new Color(0.26f, 0.21f, 0.15f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Create(string materialName, Color color)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new FileNotFoundException($"셰이더를 찾지 못했습니다: {ShaderName}. URP 패키지를 확인하세요.");
            }

            if (!Directory.Exists(MaterialDir))
            {
                Directory.CreateDirectory(MaterialDir);
                AssetDatabase.Refresh();
            }

            string path = $"{MaterialDir}/{materialName}.mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);

                // 색은 처음 만들 때만 칠한다. 이미 있는 머티리얼의 색은 사용자가 에디터에서
                // 눈으로 조정한 값일 수 있어 덮어쓰지 않는다 (ISSUE-008).
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            // 이게 꺼져 있으면 엔티티 1만 개가 드로우콜 1만 개가 된다. 이건 기존 머티리얼에도 강제한다.
            material.enableInstancing = true;

            EditorUtility.SetDirty(material);
            Debug.Log($"[UnlitMaterialBuilder] {path} 생성/갱신 (GPU Instancing: {material.enableInstancing})");
        }
    }
}
