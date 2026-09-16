using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Editor
{
    /// <summary>
    /// 벤치마크용 에셋을 코드로 생성한다.
    ///
    /// 왜 코드로 만드나 (CLAUDE.md 3.3):
    /// 머티리얼은 눈으로 조정할 게 별로 없는 반면, 어떤 셰이더에 어떤 옵션을 켰는지가
    /// 측정 결과를 좌우한다. 코드로 두면 그 설정이 명시적으로 남고 재현 가능하다.
    /// 씬 안의 오브젝트 배치는 반대 이유로 사용자가 에디터에서 직접 한다.
    /// </summary>
    public static class BenchmarkAssetBuilder
    {
        private const string MaterialDir = "Assets/MyAssets/Materials";
        private const string MaterialPath = MaterialDir + "/BenchmarkQuad.mat";

        [MenuItem("Tools/Benchmark/벤치마크 머티리얼 생성")]
        public static void CreateBenchmarkMaterial()
        {
            // 2D 게임이지만 URP "2D Renderer" 가 아니라 Universal Renderer 를 쓴다.
            // 2D Renderer 의 스프라이트 셰이더는 DOTS 인스턴싱(DOTS_INSTANCING_ON) 변형을
            // 지원하지 않아 1만 개를 드로우콜 몇 개로 묶을 수 없기 때문이다.
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new FileNotFoundException("URP Unlit 셰이더를 찾지 못했습니다. URP 패키지를 확인하세요.");
            }

            if (!Directory.Exists(MaterialDir))
            {
                Directory.CreateDirectory(MaterialDir);
                AssetDatabase.Refresh();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            // 이게 꺼져 있으면 엔티티 1만 개가 드로우콜 1만 개가 된다.
            // 벤치마크의 전제 자체가 무너지는 설정이라 코드로 못박아 둔다.
            material.enableInstancing = true;
            material.SetColor("_BaseColor", new Color(0.85f, 0.25f, 0.25f, 1f));

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BenchmarkAssetBuilder] {MaterialPath} 생성/갱신 완료 " +
                      $"(셰이더: {shader.name}, GPU Instancing: {material.enableInstancing})");
        }
    }
}
