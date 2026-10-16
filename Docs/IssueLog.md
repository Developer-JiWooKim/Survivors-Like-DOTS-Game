# 이슈 기록 (Issue Log)

Unity 6 DOTS 3D 우주 슈터 — **발생한 이슈와 대응** 기록.
날짜별 작업 내용은 [WorkLog.md](WorkLog.md) 참조. 기록 규칙은 [CLAUDE.md](../CLAUDE.md) 1.2절.

이슈 ID는 `ISSUE-001` 부터 순번으로 부여하며, **한 번 부여한 번호는 재사용하지 않는다.**
(이전 프로젝트의 이슈 번호와는 별개다. 이전 이슈를 가리킬 때는 "이전 ISSUE-0XX" 로 쓴다.)

**상태 범례**

| 상태 | 의미 |
|---|---|
| ✅ 해결 | 원인을 파악하고 고쳤음 |
| 🔄 우회 | 근본 원인은 미해결. 임시 방편으로 진행 중 (언젠가 돌아와야 함) |
| ⏳ 진행중 | 현재 대응 중 |
| ⚠️ 미해결 | 원인 미파악 또는 보류 |
| 🔁 재발 | 해결했다고 판단했으나 다시 발생 |

**이전 프로젝트에서 넘어온 미해결 툴링 이슈** (스크립트를 그대로 가져왔으므로 여기서도 날 수 있다)
- 이전 ISSUE-002 ⚠️ — `unity-test.sh` 첫 실행이 exit 255 (Unity 로그는 정상). 재현되면 여기서 새 번호로 등록한다.
- 이전 ISSUE-015 🔄 — 다른 프로세스가 `127.0.0.1:7800` 을 잡아 `unity command` 타임아웃. 재부팅으로 우회했다.

---

## 이슈 목록

| ID | 발생일 | 작업 단위 | 분류 | 제목 | 상태 | 해결일 |
|---|---|---|---|---|---|---|
| [001](#issue-001) | 2026-10-16 | WU-00 | 툴링 | `unity-recompile.sh` 가 pipeline 0.8.0 응답을 못 읽음 — 성공도 exit 3, 컴파일 에러도 exit 3 | ✅ 해결 | 2026-10-16 |
| [002](#issue-002) | 2026-10-16 | WU-00 | 빌드 | 첫 플레이어 빌드 실패 — IL2CPP 모듈 미설치, 설치 후에는 Windows SDK 미설치 | ✅ 해결 | 2026-10-16 |

---

## 상세

### ISSUE-002

| | |
|---|---|
| **발생일시** | 2026-10-16 |
| **작업 단위** | WU-00 (기반 세팅 — 첫 빌드) |
| **분류** | 빌드 |
| **상태** | ✅ 해결 |
| **해결일** | 2026-10-16 |
| **관련 작업** | [WorkLog 2026-10-16](WorkLog.md) |

**증상**

`-executeMethod SurvivorsLikeDOTS.Editor.CI.BuildPlayers` 가 exit 1, 4 초 만에 실패 (`Logs/cli-build.log`).

```
Error building Player: Currently selected scripting backend (IL2CPP) is not installed.
[CI] 빌드 Builds/Release/SurvivorsLikeDOTS.exe (None) — Failed, 에러 1, 경고 1, 4 초
```

**재현 조건**

항상. `ProjectSettings.asset` 의 `scriptingBackend: Standalone: 1` (= IL2CPP, 프로젝트 템플릿 기본값).
설치된 Windows 플레이어 변형은 `win64_player_*_mono` 뿐이다 (`Editor/Data/PlaybackEngines/windowsstandalonesupport/Variations`).

**원인**

템플릿이 IL2CPP 를 기본으로 골라 두었고, 이 PC 의 Unity 6000.6.5f1 에는 Windows IL2CPP 모듈이 설치되지 않았다.
이전 프로젝트에서 IL2CPP 재측정을 못 한 것과 같은 원인 (모듈 미설치).

**시도한 것**

| 시도 | 결과 |
|---|---|
| 백엔드 선택지 제시 (A IL2CPP 모듈 설치 / B Mono 로 변경) → 사용자 A 선택, Hub 에서 "Windows Build Support (IL2CPP)" 설치 | 모듈 설치 확인 (`Variations/win64_player_*_il2cpp`) |
| 다시 빌드 | ❌ 72 초 후 실패: `error: Could not set up a toolchain for Architecture x64.` 상세: **`Windows SDK (version 10.0.19041.0 or newer) is not installed.`** VS 2022·2026 의 C++ 컴파일러는 감지됐다 (vswhere). 레지스트리 `HKLM\SOFTWARE\Wow6432Node\Microsoft\Microsoft SDKs\Windows\v10.0` 없음, `Windows Kits\10\Include` 폴더 없음 |
| 사용자가 Windows SDK 설치 (10.0.22621.0, 10.0.26100.0 — 레지스트리 `ProductVersion 10.0.26100` 확인) 후 다시 빌드 | ✅ Release 247 초 · Development 254 초, 에러 0 |

**해결**

IL2CPP 를 유지한다 (사용자 선택 A). 필요한 것 세 가지를 모두 갖췄다:
1. Unity 모듈 "Windows Build Support (IL2CPP)"
2. C++ 컴파일러 — VS 2022 / 2026 Community (이미 있었음)
3. **Windows SDK 10.0.19041 이상** — 빠져 있었음

**재발 방지**

- 새 PC 에서 세팅할 때는 위 세 가지를 같이 확인한다. 툴체인 요구사항은 에러 메시지 목록 전체를 본다.
- 첫 IL2CPP 빌드는 4 분대 (C++ 컴파일). `Builds/*/SurvivorsLikeDOTS_BackUpThisFolder_ButDontShipItWithYourGame` (약 1.4GB, 디버그 심볼) 은 배포에서 뺀다. `Builds/` 는 gitignore 대상.

> Claude 의 판단 실수: 처음 선택지를 낼 때 vswhere 로 VC Tools 만 확인하고 "C++ 도구는 이미 있다" 고 말했다. IL2CPP 는 **컴파일러 + Windows SDK** 둘 다 필요하다. 툴체인 요구사항은 에러 메시지의 목록 전체를 확인한다.


### ISSUE-001

| | |
|---|---|
| **발생일시** | 2026-10-16 |
| **작업 단위** | WU-00 (기반 세팅) |
| **분류** | 툴링 |
| **상태** | ✅ 해결 |
| **해결일** | 2026-10-16 |
| **관련 작업** | [WorkLog 2026-10-16](WorkLog.md) |

**증상**

이전 프로젝트에서 복사한 `Tools/unity-recompile.sh` 를 처음 돌렸더니, 컴파일은 성공했는데 exit 3 으로 끝났다.

```
[!] recompile_status 를 읽지 못했습니다.
...
    "result": {
      "status": "completed",
      "failed": false,
      "errors": [],
      "compilationFailed": false
    },
```

1차 수정 후 일부러 컴파일 에러(CS0029)를 넣으면 이번에는 exit 1 이 아니라 아래로 끝났다 (같은 상태로 두 번 돌려도 동일).

```
[!] 컴파일 결과를 판정하지 못했습니다 (실패 표시인데 에러 목록이 비어 있음 — 이전 결과로 보임).
```

**재현 조건**

항상. 이 프로젝트의 `com.unity.pipeline` 이 `0.8.0-exp.1` 이다. 이전 프로젝트의 응답 형식과 다르다.

| | 이전 프로젝트 | 이 프로젝트 (0.8.0) |
|---|---|---|
| `result` | 이스케이프된 JSON **문자열** `"{\"status\":\"completed\",...}"` | 들여쓴 JSON **객체** `"status": "completed"` (콜론 뒤 공백) |
| 봉투 | — | 바깥에 CLI 자체의 `"errors": []`, `"warnings": []` 가 따로 있음 |

**원인**

1. 상태 추출 정규식이 `"status":"..."` (공백 없음) 만 매칭 → 상태를 못 읽음 → exit 3.
2. 낡은 결과 방어 로직(이전 ISSUE-010: "실패인데 에러 목록이 `[]` 이면 낡은 결과로 보고 계속 폴링")이 **봉투의 `"errors": []`** 에 매칭됨.
   진짜 컴파일 에러는 `"errors": [\n "..."` 로 여러 줄이라 `[]` 패턴에 안 걸리는데, 봉투 쪽 `[]` 가 걸려서 **모든 실패가 "낡은 결과"** 로 판정됐다.

**시도한 것**

| 시도 | 결과 |
|---|---|
| 정규식에 콜론 뒤 공백 허용 (`"status": *"..."`, `"errors": *\[\]`, `"(failed)": *true`) | 성공 경로 ✅ exit 0. 에러 경로 ❌ 여전히 "판정 불가" exit 3 |
| **가설**: 에디터가 파일 변경을 먼저 감지해 자동 컴파일 → `clear_console` 이 에러 목록을 지움 → `recompile` 은 할 일이 없어 새 결과 없음. 대응으로 `clear_console` 전 상태를 보관했다가 끝까지 판정 불가면 그걸 쓰게 함 | ❌ 그대로 exit 3. **가설이 틀렸다** — 되돌림 |
| 단계별 수동 추적 (`--result-only` 로 매 단계 `recompile_status` 출력) | `clear_console` 후에도 에러 목록은 **남아 있었다**. 에러 목록은 정상으로 오고, 스크립트가 봉투의 `[]` 를 잘못 읽고 있었음을 확인 |
| `recompile_status` 호출에 `--result-only` 추가 (내부 결과만 받음) | ✅ |

**해결**

- `recompile_status` 를 `--result-only` 로 호출해 봉투를 아예 받지 않는다.
- 상태·실패·에러 목록 정규식이 콜론 뒤 공백을 허용한다.
- 에러 원문 추출을 `"errors":"..."` 문자열 형식에서 `error CS0029` 같은 **에러 코드 패턴**으로 바꿨다 (배열 형식이어도 잡히고, DC/EA/BC 계열 코드도 잡힌다).

**검증**: 임시 파일로 CS0029 주입 → exit 1 + 에러 원문 출력 ✅ / 파일 삭제 → exit 0 `[OK]` ✅

**재발 방지**

- `com.unity.pipeline` 을 올리면 **에러 주입 테스트(성공·실패 두 경로)** 를 다시 돌린다 (CLAUDE.md 3장에 기록).
- 배운 것: 수동 추적 전에 가설로 고치기부터 하면 한 바퀴를 버린다. 형식이 의심되면 **원본 응답을 단계별로 먼저 찍어 본다.**
- 참고: 0.8.0 에서 `recompile` 은 에러가 남아 있으면 `"status": "failed", "message": "Scripts still have compile errors; nothing was recompiled."` 를 바로 돌려준다. 지금 스크립트는 이 값을 쓰지 않는다 (폴링 결과로 충분).
