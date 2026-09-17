using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.MyAssets.Scripts.Editor
{
    /// <summary>
    /// URP 렌더러의 렌더러 기능(Renderer Feature) 켜고 끄기를 코드로 남긴다 (CLAUDE.md 3.4 — 에셋은 코드로).
    ///
    /// SSAO 를 끄는 이유 (ISSUE-005):
    /// URP 템플릿 기본값으로 켜져 있던 SSAO 는 노멀이 필요해 **DepthNormal 프리패스**를 강제한다.
    /// 그 프리패스가 적 1 만 마리를 한 번 더 그려 지오메트리 제출량이 정확히 2 배가 됐다 (Frame Debugger 로 확인).
    /// 단색 2D 탑다운에서는 SSAO 의 시각적 이득이 거의 없다.
    ///
    /// 기능을 **삭제하지 않고 끄기만** 한다 — 되돌릴 때 설정값이 그대로 남아 있게.
    /// </summary>
    public static class RendererFeatureSettings
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";

        [MenuItem("Tools/렌더러/SSAO 끄기")]
        public static void DisableSsao()
        {
            SetSsaoActive(false);
        }

        [MenuItem("Tools/렌더러/SSAO 켜기")]
        public static void EnableSsao()
        {
            SetSsaoActive(true);
        }

        private static void SetSsaoActive(bool active)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
            if (renderer == null)
            {
                Debug.LogError($"[RendererFeatureSettings] 렌더러를 찾지 못했습니다: {PcRendererPath}");
                return;
            }

            int changed = 0;
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (feature is not ScreenSpaceAmbientOcclusion)
                {
                    continue;
                }

                feature.SetActive(active);
                EditorUtility.SetDirty(feature);
                changed++;
            }

            EditorUtility.SetDirty(renderer);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RendererFeatureSettings] {PcRendererPath} SSAO {(active ? "켜기" : "끄기")} — 대상 {changed} 개");
        }
    }
}
