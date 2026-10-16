using UnityEditor;
using UnityEngine;

namespace SurvivorsLikeDOTS.Editor
{
    /// <summary>
    /// Unity CLI 배치모드 진입점.
    ///
    /// 왜 필요한가:
    /// 에디터를 사람이 직접 열지 않고도 컴파일 성공 여부를 프로세스 exit code 로 받기 위함.
    /// 이게 없으면 "코드 작성 → 사람이 에디터 확인 → 에러 복사 → 수정" 왕복이 매번 발생한다.
    /// </summary>
    public static class CI
    {
        /// <summary>
        /// 컴파일 검증. 성공 0 / 실패 1 로 종료한다.
        ///
        /// 주의: 컴파일이 실패하면 이 메서드를 담은 어셈블리도 로드되지 않아
        /// 메서드 자체가 호출되지 않고 Unity 가 자체적으로 비정상 종료 코드를 반환한다.
        /// 즉 "여기 도달했다" 자체가 1차 성공 신호이고, 아래 플래그 확인은 2차 안전장치다.
        /// 그래서 호출 측 스크립트는 exit code 와 로그의 'error CS' 를 함께 본다.
        /// </summary>
        public static void CompileCheck()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[CI] 스크립트 컴파일 실패");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[CI] 컴파일 성공");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 빌드 실측용 플레이어 빌드 (릴리스 + 개발). 성공 0 / 실패 1.
        ///
        /// 왜 코드로 두나: 측정 조건의 일부가 빌드 옵션이다. 메뉴로 빌드하면 "그때 개발 빌드였나" 를 기억에 의존한다.
        /// 씬 목록·해상도·백엔드는 ProjectSettings 에 있으므로 여기서는 출력 경로와 옵션만 정한다.
        /// 에디터가 켜져 있으면 <c>unity command build</c> 로 같은 일을 할 수 있다 (2026-10-15 첫 빌드는 그쪽).
        /// </summary>
        public static void BuildPlayers()
        {
            bool ok = Build("Builds/Release/Survivors.exe", BuildOptions.None)
                      && Build("Builds/Development/Survivors.exe", BuildOptions.Development);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(string outputPath, BuildOptions options)
        {
            var buildOptions = new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = options,
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            UnityEditor.Build.Reporting.BuildSummary summary = report.summary;
            Debug.Log($"[CI] 빌드 {outputPath} ({options}) — {summary.result}, 에러 {summary.totalErrors}, 경고 {summary.totalWarnings}, {summary.totalTime.TotalSeconds:F0} 초");
            return summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        }
    }
}
