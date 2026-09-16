#!/usr/bin/env bash
# unity-check.sh / unity-test.sh 가 source 하는 공통 경로 탐지 로직.
#
# 왜 필요한가:
# 경로를 하드코딩하면 다른 컴퓨터에서 클론했을 때 깨진다. 특히 프로젝트 경로가 틀리면
# Unity 가 에러를 내는 게 아니라 "그 경로에 새 빈 프로젝트를 생성"해버리기 때문에
# 실패보다 나쁜 결과가 나온다. 그래서 경로는 추측하지 않고 탐지 + 검증한다.
#
# 제공하는 변수:
#   PROJ_UNIX     프로젝트 루트 (Git Bash 형식 /c/...)
#   PROJ_WIN      프로젝트 루트 (Windows 형식 C:/...)  ← Unity.exe 인자용
#   LOG_DIR       로그 디렉터리 (Git Bash 형식)
#   LOG_DIR_WIN   로그 디렉터리 (Windows 형식)
#   UNITY_VERSION ProjectVersion.txt 에서 읽은 에디터 버전
#   UNITY         Unity.exe 절대 경로
#
# 제공하는 함수:
#   to_win / to_unix        경로 형식 변환
#   assert_no_editor_lock   에디터 실행 중이면 exit 2

# --- 경로 형식 변환 -----------------------------------------------------------
# Unity.exe 는 네이티브 Windows 프로그램이라 /c/... 형식을 이해하지 못한다.
# 반대로 bash 의 test/mkdir 은 C:/... 를 다루기 번거롭다. 그래서 양쪽을 모두 들고 다닌다.
to_win() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -m "$1"
  else
    printf '%s' "$1" | sed -E 's#^/([a-zA-Z])/#\1:/#'
  fi
}

to_unix() {
  if command -v cygpath >/dev/null 2>&1; then
    cygpath -u "$1"
  else
    printf '%s' "$1" | sed -E 's#\\#/#g; s#^([a-zA-Z]):#/\1#'
  fi
}

# --- 프로젝트 루트 탐지 -------------------------------------------------------
# 1순위 git 루트, 2순위 스크립트 위치의 상위 폴더.
# 어느 쪽이든 ProjectVersion.txt 존재로 "정말 Unity 프로젝트인지" 검증한다.
_SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJ_UNIX="$(cd "$_SCRIPT_DIR/.." && pwd)"

if command -v git >/dev/null 2>&1; then
  _git_root="$(git -C "$PROJ_UNIX" rev-parse --show-toplevel 2>/dev/null || true)"
  if [ -n "$_git_root" ] && [ -f "$_git_root/ProjectSettings/ProjectVersion.txt" ]; then
    PROJ_UNIX="$_git_root"
  fi
fi

if [ ! -f "$PROJ_UNIX/ProjectSettings/ProjectVersion.txt" ]; then
  echo "[!] Unity 프로젝트 루트를 찾지 못했습니다."
  echo "    탐지된 경로: $PROJ_UNIX"
  echo "    ProjectSettings/ProjectVersion.txt 가 없습니다."
  echo "    Tools/ 폴더가 프로젝트 루트 바로 아래에 있는지 확인하세요."
  exit 3
fi

PROJ_WIN="$(to_win "$PROJ_UNIX")"
LOG_DIR="$PROJ_UNIX/Logs"
LOG_DIR_WIN="$(to_win "$LOG_DIR")"
mkdir -p "$LOG_DIR"

# --- 에디터 버전 --------------------------------------------------------------
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: *//p' \
  "$PROJ_UNIX/ProjectSettings/ProjectVersion.txt" | tr -d '\r' | head -n 1)"

if [ -z "$UNITY_VERSION" ]; then
  echo "[!] ProjectVersion.txt 에서 에디터 버전을 읽지 못했습니다."
  exit 3
fi

# --- Unity.exe 탐지 -----------------------------------------------------------
# UNITY_PATH 환경변수가 있으면 최우선. 자동 탐지가 실패하는 환경의 탈출구다.
_find_unity() {
  if [ -n "${UNITY_PATH:-}" ]; then
    if [ -f "$UNITY_PATH" ]; then
      printf '%s' "$UNITY_PATH"
      return 0
    fi
    echo "[!] UNITY_PATH 가 가리키는 파일이 없습니다: $UNITY_PATH" >&2
    return 1
  fi

  local roots=()

  # Unity Hub 보조 설치 경로 (Hub 설정에서 설치 위치를 바꾼 경우 여기에 기록된다)
  local _appdata=""
  [ -n "${APPDATA:-}" ] && _appdata="$(to_unix "$APPDATA")"
  local _sec_json="$_appdata/UnityHub/secondaryInstallPath.json"
  if [ -n "$_appdata" ] && [ -f "$_sec_json" ]; then
    local _sec
    _sec="$(tr -d '"\r\n' < "$_sec_json")"
    [ -n "$_sec" ] && roots+=("$(to_unix "$_sec")")
  fi

  # 드라이브별 기본 설치 위치
  local d
  for d in c d e f; do
    roots+=("/$d/Program Files/Unity/Hub/Editor")
    roots+=("/$d/Unity/Hub/Editor")
  done

  local r exe
  for r in "${roots[@]}"; do
    exe="$r/$UNITY_VERSION/Editor/Unity.exe"
    if [ -f "$exe" ]; then
      printf '%s' "$exe"
      return 0
    fi
  done

  # 못 찾았으면 설치된 다른 버전이라도 알려준다 (버전 불일치가 가장 흔한 원인)
  echo "[!] Unity $UNITY_VERSION 을 찾지 못했습니다." >&2
  echo "    탐색한 위치:" >&2
  for r in "${roots[@]}"; do
    [ -d "$r" ] && echo "      $r" >&2
  done
  echo "    설치된 버전:" >&2
  local found=0
  for r in "${roots[@]}"; do
    if [ -d "$r" ]; then
      local v
      for v in "$r"/*/; do
        [ -d "$v" ] && { echo "      $(basename "$v")" >&2; found=1; }
      done
    fi
  done
  [ "$found" -eq 0 ] && echo "      (없음)" >&2
  echo "" >&2
  echo "    해결: Unity Hub 에서 $UNITY_VERSION 을 설치하거나," >&2
  echo "          UNITY_PATH 환경변수로 직접 지정하세요." >&2
  echo "          예) UNITY_PATH='/d/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe' bash Tools/unity-check.sh" >&2
  return 1
}

UNITY="$(_find_unity)" || exit 3

# --- 에디터 락 확인 -----------------------------------------------------------
# Unity 는 한 프로젝트를 두 인스턴스가 열 수 없다. 에디터가 켜져 있으면 batchmode 가
# 락 획득에 실패하는데 에러 메시지가 불친절해서 여기서 먼저 걸러준다.
# 주의: 다른 프로젝트를 열어둔 Unity 도 여기에 걸린다. 오탐이지만 안전한 쪽이다.
assert_no_editor_lock() {
  if tasklist 2>/dev/null | grep -qi "^Unity\.exe"; then
    echo "[!] Unity 에디터가 실행 중입니다."
    echo "    프로젝트 락 때문에 CLI 를 실행할 수 없습니다. 에디터를 닫고 다시 시도하세요."
    exit 2
  fi
}
