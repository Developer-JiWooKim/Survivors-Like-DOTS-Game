using UnityEditor;
using UnityEngine;

namespace Assets.MyAssets.Scripts.Editor
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
    }
}
