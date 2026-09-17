#!/usr/bin/env bash
# 실행 중인 Unity 에디터에서 재컴파일한다. **에디터를 닫을 필요가 없다.**
#   사용법: bash Tools/unity-recompile.sh
#   종료 코드: 0=컴파일 성공 / 1=컴파일 에러 / 3=에디터 미연결·CLI 없음
#
# unity-check.sh 와의 차이:
#   unity-check.sh   에디터를 닫아야 함. 배치모드로 Unity 를 새로 띄운다. 느리지만 항상 동작.
#   unity-recompile  에디터가 켜져 있어야 함. com.unity.pipeline 을 통해 live 에디터에 명령. 빠름.
#
# 컴파일 에러가 있으면 에디터가 Safe Mode 로 부팅되는데, 그 상태에서는 Pipeline 이 로드되지 않아
# 이 스크립트가 연결조차 못 한다. 그럴 때는 unity-check.sh 로 넘어가야 한다.
set -u

if ! command -v unity >/dev/null 2>&1; then
  echo "[!] unity CLI 를 찾을 수 없습니다. Docs/Setup.md 4장을 참고하세요."
  exit 3
fi

UNITY_ARGS="--no-banner --no-pager --format json"

# --- 에디터 연결 확인 ---------------------------------------------------------
STATUS_RAW=$(unity status $UNITY_ARGS 2>&1)
if ! printf '%s' "$STATUS_RAW" | grep -q '"state": *"ready"'; then
  echo "[!] 준비된 Unity 에디터를 찾지 못했습니다."
  echo "    에디터가 꺼져 있거나, 컴파일 에러로 Safe Mode 에 들어가 Pipeline 이 로드되지 않았을 수 있습니다."
  echo "    대안: bash Tools/unity-check.sh  (에디터를 닫고 배치모드로 검증)"
  exit 3
fi

# --- 재컴파일 요청 ------------------------------------------------------------
unity command clear_console $UNITY_ARGS >/dev/null 2>&1
unity command recompile $UNITY_ARGS >/dev/null 2>&1

echo "[*] 재컴파일 요청됨. 완료를 기다립니다..."

# recompile_status 의 result 는 **이스케이프된 JSON 문자열**이라 그대로는 파싱되지 않는다.
#   "result": "{\"status\":\"completed\",...}"
# 역슬래시를 먼저 풀고 읽는다.
# 폴링 간격은 따로 두지 않는다. unity command 왕복 자체가 수백 ms 걸려 그게 간격 역할을 한다.
STATUS=""
INNER=""
for _ in $(seq 1 60); do
  RAW=$(unity command recompile_status $UNITY_ARGS 2>&1)
  INNER=$(printf '%s' "$RAW" | sed 's/\\"/"/g')
  # 상태값에 언더스코어가 들어간다 (up_to_date). 문자 클래스에 반드시 포함해야 한다.
  STATUS=$(printf '%s' "$INNER" | grep -oE '"status":"[a-zA-Z_]+"' | head -1 | cut -d'"' -f4)

  case "$STATUS" in
    # up_to_date: 바뀐 게 없어 재컴파일할 필요가 없었다는 뜻. 실패가 아니다.
    completed|up_to_date|idle|done)
      # "실패인데 에러 0 건" 은 **지난 컴파일의 낡은 결과**다 (ISSUE-010).
      # 새 컴파일이 시작되기 전에 상태를 읽었고, 에러 목록은 위의 clear_console 로 이미 비워졌다.
      # 진짜 결과가 나올 때까지 계속 폴링한다.
      if printf '%s' "$INNER" | grep -qE '"(compilationFailed|failed)":true' \
         && printf '%s' "$INNER" | grep -qE '"errors":\[\]'; then
        STALE=1
        continue
      fi
      STALE=0
      break
      ;;
  esac
done

if [ "${STALE:-0}" = "1" ]; then
  echo "[!] 컴파일 결과를 판정하지 못했습니다 (실패 표시인데 에러 목록이 비어 있음 — 이전 결과로 보임)."
  echo "    다시 실행해 보세요. 반복되면 에디터 콘솔을 직접 확인하세요. (ISSUE-010)"
  exit 3
fi

if [ -z "$STATUS" ]; then
  echo "[!] recompile_status 를 읽지 못했습니다."
  printf '%s\n' "$RAW" | head -20
  exit 3
fi

case "$STATUS" in
  completed|up_to_date|idle|done) ;;
  *)
    echo "[!] 제한 시간 안에 컴파일이 끝나지 않았습니다 (마지막 상태: $STATUS)"
    exit 3
    ;;
esac

# --- 결과 판정 ----------------------------------------------------------------
if printf '%s' "$INNER" | grep -qE '"(compilationFailed|failed)":true'; then
  echo ""
  echo "=== 컴파일 에러 ==="
  printf '%s' "$INNER" \
    | grep -oE '"(message|errors)":"[^"]*"' \
    | sed 's/^"[a-z]*":"//; s/"$//' \
    | sort -u
  echo ""
  echo "전체 응답:"
  printf '%s\n' "$INNER" | head -30
  exit 1
fi

echo "[OK] 컴파일 성공 (에디터 연결 상태)"
exit 0
