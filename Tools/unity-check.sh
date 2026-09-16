#!/usr/bin/env bash
# Unity 배치모드 컴파일 검증.
#   사용법: bash Tools/unity-check.sh
#   종료 코드: 0=컴파일 성공 / 1=컴파일 에러 / 2=에디터 락 / 3=환경 문제
#
# 경로는 _common.sh 가 자동 탐지한다. 자동 탐지가 실패하면 UNITY_PATH 로 지정할 수 있다.
set -u

source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

LOG="$LOG_DIR/cli-compile.log"
LOG_WIN="$LOG_DIR_WIN/cli-compile.log"

assert_no_editor_lock

echo "[*] 컴파일 검증 시작... (최초 실행은 임포트 때문에 수 분 걸릴 수 있음)"
echo "    프로젝트: $PROJ_WIN"
echo "    Unity:    $UNITY_VERSION"

"$UNITY" -batchmode -quit -nographics \
  -projectPath "$PROJ_WIN" \
  -executeMethod Assets.MyAssets.Scripts.Editor.CI.CompileCheck \
  -logFile "$LOG_WIN"
CODE=$?

# --- 결과 판정 ----------------------------------------------------------------
# exit code 만 믿지 않는다. 컴파일이 실패하면 CI.CompileCheck 를 담은 어셈블리 자체가
# 로드되지 않아 우리 코드의 종료 경로를 타지 못한다. 그래서 로그의 에러 코드도 함께 본다.
#
# 'error CS' 만 보면 안 된다: DOTS 소스 제너레이터는 DC0061 같은 자체 진단 코드를 쓰고,
# Burst 는 BC####, Unity 분석기는 UNT#### 를 쓴다. 접두사를 가정하지 않고 전부 잡는다.
ERRORS=$(grep -oE "[^ ]+\.cs\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*" "$LOG" 2>/dev/null | sort -u)

if [ -n "$ERRORS" ]; then
  echo ""
  echo "=== 컴파일 에러 ==="
  echo "$ERRORS"
  echo ""
  echo "전체 로그: Logs/cli-compile.log"
  exit 1
fi

if [ "$CODE" -ne 0 ]; then
  echo ""
  echo "[!] Unity 가 비정상 종료했습니다 (exit $CODE). 컴파일 에러가 아닌 다른 원인일 수 있습니다."
  echo "--- 로그 끝부분 ---"
  tail -n 40 "$LOG" 2>/dev/null
  exit 1
fi

echo "[OK] 컴파일 성공"
exit 0
