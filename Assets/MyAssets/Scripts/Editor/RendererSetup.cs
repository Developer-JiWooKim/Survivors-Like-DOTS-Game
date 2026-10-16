using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SurvivorsLikeDOTS.Editor
{
    // 렌더러 에셋 정리. 배치모드 진입점:
    //   -executeMethod SurvivorsLikeDOTS.Editor.RendererSetup.RemoveSSAO
    public static class RendererSetup
    {
        private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
        private const string SsaoTypeName = "ScreenSpaceAmbientOcclusion";

        // SSAO 는 "끄기" 가 아니라 "제거" 해야 한다.
        // - 켜 두면 DepthNormal 프리패스로 지오메트리가 2 배 제출된다 (이전 프로젝트 ISSUE-005).
        // - 끄기만 하면 빌드에서 SSAO 리소스가 스트리핑되어 파이프라인 생성이 실패하고 검은 화면이 된다 (이전 ISSUE-018).
        [MenuItem("Tools/SurvivorsLikeDOTS/Remove SSAO From PC Renderer")]
        public static void RemoveSSAO()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
            if (data == null)
            {
                Fail($"렌더러 에셋을 찾지 못함: {PcRendererPath}");
                return;
            }

            // 기능 목록과 ID 맵은 인덱스로 짝을 이룬다. 한쪽만 지우면 URP 에디터가 목록을 복구하려 들거나 어긋난다.
            // URP 인스펙터의 Remove 와 같은 결과가 되도록 SerializedObject 로 둘 다 지운다.
            var so = new SerializedObject(data);
            SerializedProperty features = so.FindProperty("m_RendererFeatures");
            SerializedProperty featureMap = so.FindProperty("m_RendererFeatureMap");

            int removed = 0;
            for (int i = features.arraySize - 1; i >= 0; i--)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (feature == null || feature.GetType().Name != SsaoTypeName)
                {
                    continue;
                }

                features.DeleteArrayElementAtIndex(i);
                featureMap.DeleteArrayElementAtIndex(i);

                // 목록에서 빼도 서브에셋은 파일 안에 남는다. 남겨 두면 스트리핑 판정에 걸릴 수 있어 같이 지운다.
                AssetDatabase.RemoveObjectFromAsset(feature);
                Object.DestroyImmediate(feature, true);
                removed++;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();

            Debug.Log($"[RendererSetup] {PcRendererPath} 에서 SSAO {removed}개 제거. 남은 기능 {features.arraySize}개");
        }

        private static void Fail(string message)
        {
            Debug.LogError($"[RendererSetup] {message}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
