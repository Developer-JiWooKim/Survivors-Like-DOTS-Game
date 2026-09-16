#!/usr/bin/env bash
# Unity 배치모드 테스트 실행.
#   사용법: bash Tools/unity-test.sh [EditMode|PlayMode]   (기본 EditMode)
#   종료 코드: 0=전체 통과 / 1=실패 있음 / 2=에디터 락 / 3=환경 문제
#
# 경로는 _common.sh 가 자동 탐지한다. 자동 탐지가 실패하면 UNITY_PATH 로 지정할 수 있다.
set -u

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

PLATFORM="${1:-EditMode}"
case "$PLATFORM" in
  EditMode|PlayMode) ;;
  *) echo "[!] 지원하지 않는 플랫폼: $PLATFORM (EditMode 또는 PlayMode)"; exit 3 ;;
esac

LOG="$LOG_DIR/cli-test-$PLATFORM.log"
LOG_WIN="$LOG_DIR_WIN/cli-test-$PLATFORM.log"
RESULTS="$LOG_DIR/test-results-$PLATFORM.xml"
RESULTS_WIN="$LOG_DIR_WIN/test-results-$PLATFORM.xml"

assert_no_editor_lock

# 이전 실행 결과가 남아 있으면 "이번 실행이 결과를 만들었는지" 판단할 수 없다.
rm -f "$RESULTS"

echo "[*] $PLATFORM 테스트 실행 중..."
echo "    프로젝트: $PROJ_WIN"
echo "    Unity:    $UNITY_VERSION"

# -runTests 사용 시에는 -quit 을 주지 않는다. 테스트 러너가 스스로 종료한다.
# PlayMode 테스트는 -nographics 에서 렌더가 없으므로 렌더 의존 테스트는 넣지 말 것.
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJ_WIN" \
  -runTests -testPlatform "$PLATFORM" \
  -testResults "$RESULTS_WIN" \
  -logFile "$LOG_WIN"
CODE=$?

if [ ! -f "$RESULTS" ]; then
  echo "[!] 결과 파일이 생성되지 않았습니다. 테스트가 실행되지 못했을 가능성이 큽니다."
  echo "--- 로그 끝부분 ---"
  tail -n 40 "$LOG" 2>/dev/null
  exit 1
fi

# NUnit XML 루트에 집계가 들어있다.
echo ""
echo "=== 결과 ==="
grep -oE '(total|passed|failed|skipped)="[0-9]+"' "$RESULTS" 2>/dev/null | head -n 4

FAILED=$(grep -oE 'result="Failed"' "$RESULTS" 2>/dev/null | wc -l)
if [ "$FAILED" -gt 0 ]; then
  echo ""
  echo "=== 실패한 테스트 ==="
  grep -oE '<test-case [^>]*name="[^"]*"[^>]*result="Failed"' "$RESULTS" 2>/dev/null \
    | grep -oE 'name="[^"]*"' | sort -u
  echo ""
  echo "상세: Logs/test-results-$PLATFORM.xml"
  exit 1
fi

if [ "$CODE" -ne 0 ]; then
  echo "[!] Unity 비정상 종료 (exit $CODE)"
  tail -n 40 "$LOG" 2>/dev/null
  exit 1
fi

echo "[OK] 전체 통과"
exit 0
