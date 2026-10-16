# 작업 기록 (Work Log)

Unity 6 DOTS 3D 우주 슈터 — **날짜별 작업 내용** 기록.
발생한 이슈는 [IssueLog.md](IssueLog.md) 에 별도 기록하고, 여기서는 이슈 ID로만 참조한다.

**최신 기록이 위로 오도록 역순 누적한다.**
기록 규칙은 [CLAUDE.md](../CLAUDE.md) 1장 참조.

---

## 📍 현재 위치 (세션 이어갈 때 여기부터)

> 작업을 마칠 때마다 이 블록을 갱신한다. 아래 상세 엔트리보다 **이 블록이 먼저 최신**이어야 한다.

| | |
|---|---|
| **진행 중** | **WU-00 기반 세팅 ✅ 완료 (커밋 대기)** — SSAO 제거 · `unity-check.sh` 두 경로 검증 · **IL2CPP 첫 빌드 성공** (Release·Development, [ISSUE-002](IssueLog.md#issue-002)) · 릴리스 빌드 15 초 실행 예외 0 |
| **다음 후보** | **WU-01 우주선 6DOF 비행 + 3인칭 체이스캠** (에셋: Hi-Rez Spaceships Creator Free — 임포트 완료). 시작할 때 정할 것: 우주선을 엔티티로 굽는지 / GameObject 로 두는지, 비행 모델(아케이드 vs 관성), 입력 매핑 |
| **정해진 방향** | 전투 = 주무기 직접 조준 + 보조 무기 자동(레벨업) · 시점 = 3인칭 체이스캠 6DOF · 끝을 정하지 않고 작업 단위로 확장 (CLAUDE.md 6장) |
| **아직 안 정한 것** | 아키타입 원칙 · 공간 해시 2D/3D · 성능 목표 수치(적 수) · 기획서 |
| **미해결 이슈** | 없음 (이전 프로젝트에서 넘어온 툴링 이슈 2건은 [IssueLog](IssueLog.md) 상단 참조) |
| **마지막 커밋** | `df1fdc2` (사용자 — 초기 커밋 이후. 이날 작업은 아직 커밋 전) |

---

## 2026-10-16 — WU-00 기반 세팅 · 게임 방향 결정

관련 이슈: [ISSUE-001](IssueLog.md#issue-001)

### 1. 게임 방향 결정 (사용자)

이전 프로젝트 인수인계 문서(`Assets/Handoff_NewProject.txt`) 를 읽고 시작했다.

| 질문 | 선택지 | 결정 |
|---|---|---|
| 전투 방식 | A 뱀서식 자동 공격 / B 직접 조준 슈터 / C 혼합 | **C 혼합** — 주무기 직접 조준 + 보조 무기는 레벨업으로 얻는 자동 무기. A는 3D 비행에서 할 일이 적어 심심하고, B는 빌드 성장·대량 적 시너지(DOTS 를 보여 줄 축)가 약해진다 |
| 시점·이동 | A 3인칭 체이스캠 6DOF / B 1인칭 조종석 / C 3D 그래픽 + 평면 이동 | **A** — 이전에 정한 "자유 비행" 과 맞고 우주선 에셋이 화면에 보인다. 대가: 화면 밖 적 표시(레이더 등)가 필요하고 공간 해시를 3D 로 다시 생각해야 한다 |
| 프로젝트의 끝 | Claude 제안: "버티컬 슬라이스 (한 판 플레이 + 출시 수준 퀄리티 + 빌드 실측)" | **끝을 정하지 않는다** (사용자 제안). 작업 단위 하나를 끝내면 다음 작업을 의논해 정하며 확장. → 끝이 없으면 범위가 흐려질 위험이 있어 **작업 단위마다 완료 기준**(컴파일 → 에디터 확인 → 빌드+수치 → 기록 → 의논)을 두기로 했다 (CLAUDE.md 6장) |

### 2. 이름 체계 (사용자)

| | 이전 프로젝트 | 이 프로젝트 |
|---|---|---|
| 네임스페이스 | 폴더 경로 그대로 `Assets.MyAssets.Scripts.Editor` | **`SurvivorsLikeDOTS` + `.폴더명`** → `SurvivorsLikeDOTS.Editor` |
| 어셈블리 | `Survivors.Runtime` / `.Editor` / `.Tests` (네임스페이스와 일부러 다르게) | **네임스페이스와 같게** `SurvivorsLikeDOTS` / `.Editor` / `.Tests` |

- 왜: 사용자 — "경로명을 다 쓰면 너무 길다". 네임스페이스가 짧아지니 어셈블리와 다르게 둘 이유도 없어져 하나로 맞췄다 (Claude 추천, 사용자 동의).
- 주의: `SurvivorsLikeDOTS` 아래에서 `Editor` 가 네임스페이스로 해석된다 → 커스텀 인스펙터는 `UnityEditor.Editor`.

### 3. 이전 저장소에서 가져온 것

| 파일 | 수정 |
|---|---|
| `Tools/_common.sh`, `unity-check.sh`, `unity-recompile.sh`, `unity-test.sh` | `unity-check.sh` 의 `-executeMethod` → `SurvivorsLikeDOTS.Editor.CI.CompileCheck`. `unity-recompile.sh` 는 [ISSUE-001](IssueLog.md#issue-001) 수정 |
| `.claude/skills/unity-cli`, `unity-pipeline` | 그대로 |
| `Assets/MyAssets/Scripts/Editor/CI.cs` | 네임스페이스 `Assets.MyAssets.Scripts.Editor` → `SurvivorsLikeDOTS.Editor`, 빌드 파일명 `Survivors.exe` → `SurvivorsLikeDOTS.exe` |
| `.editorconfig`, `.gitignore` | `.gitignore` 에 Asset Store 에셋 폴더 제외 추가 |

`.meta` 는 복사하지 않았다 (새 프로젝트 임포트가 생성).

### 4. 새로 만든 것

- `Assets/MyAssets/Scripts/SurvivorsLikeDOTS.asmdef` — 런타임. 이전 `Survivors.Runtime` 참조 + `UniTask`
- `Assets/MyAssets/Scripts/Editor/SurvivorsLikeDOTS.Editor.asmdef` — 에디터 전용
- 검증: `unity-recompile.sh` exit 0, `Library/ScriptAssemblies` 에 두 dll 생성 확인
- `CLAUDE.md`, `Docs/WorkLog.md`, `Docs/IssueLog.md`

### 5. 그대로 두기로 한 것

- **패키지** (Visual Scripting, AI Navigation, Collab Proxy, Timeline): Claude 는 제거를 추천했으나 사용자 판단 — "있다고 딱히 문제가 있는 건 아니다". 실제 비용은 컴파일·도메인 리로드가 조금 느려지는 정도. 문제가 되면 그때 측정하고 뺀다.
- **UniTask**: 사용자가 비동기용으로 넣어 둠. 비동기가 필요하면 UniTask 를 쓴다 — 매니지드 쪽 전용.
- **SSAO**: 지금 급하지 않다. 첫 빌드·첫 측정 전에 **제거** (끄기만 하면 빌드 검은 화면 — 이전 ISSUE-018).

### 6. 외부 에셋

- Hi-Rez Spaceships Creator Free Sample 을 사용자가 임포트 (`Assets/HiRezSpaceshipsCreatorFree/`, 약 135MB).
  - 머티리얼 22개가 URP Lit (셰이더 GUID `933532a4…`) → 핑크 문제 없음. `SpaceBG.mat` 만 내장 스카이박스 셰이더.
  - 구성: 예제 우주선 프리팹 (조종석 내부 포함 / 미포함 각 6종), 모듈 프리팹, FBX 1개, 텍스처 75장.
  - 라이선스상 재배포 불가 → `.gitignore` 로 제외. 다시 임포트해도 패키지 안의 `.meta` 로 GUID 가 같게 복원된다.

### 7. 배치모드 검증 · SSAO 제거 · 첫 빌드 시도 (에디터 닫은 상태)

- `unity-check.sh`: 성공 경로 exit 0 (약 16 초) ✅ / CS0029 주입 시 exit 1 + 에러 원문 ✅. 에디터 켜짐·꺼짐 두 경로가 모두 동작한다.
- **SSAO 제거**: `Assets/MyAssets/Scripts/Editor/RendererSetup.cs` 추가 — `-executeMethod SurvivorsLikeDOTS.Editor.RendererSetup.RemoveSSAO` (메뉴 `Tools/SurvivorsLikeDOTS/Remove SSAO From PC Renderer` 도 있음).
  - 왜 스크립트로: `.asset` 직접 편집 금지 규칙(CLAUDE.md 3.4). 그리고 기능 목록 `m_RendererFeatures` 와 ID 맵 `m_RendererFeatureMap` 이 인덱스로 짝이라 둘 다 지워야 하고, 서브에셋도 파일에서 빼야 URP 인스펙터의 Remove 와 같은 결과가 된다.
  - 결과: `PC_Renderer.asset` 의 `m_RendererFeatures: []`, SSAO 서브에셋 블록 삭제. 같이 생긴 diff 는 URP 17.6 이 다시 저장하며 생긴 것 (YAML 줄바꿈, `m_AssetVersion` 2 → 3, 새 필드 기본값) — 동작 변화 없음.
  - `Mobile_Renderer` 에는 원래 기능이 없다.
- **첫 빌드 실패** → [ISSUE-002](IssueLog.md#issue-002). 빌드가 IL2CPP 를 요구하는데 모듈이 없다. "빌드를 초반부터" 원칙 덕에 WU-00 에서 바로 드러났다.

### 8. 스크립팅 백엔드 결정 · 첫 빌드 성공

| 선택지 | 내용 | 결정 |
|---|---|---|
| **A. IL2CPP 유지 + 모듈 설치** | 출시 백엔드 그대로. 측정 수치가 배포 조건과 같다. 이전 프로젝트에서 못 한 IL2CPP 측정도 가능 | **채택** (사용자) |
| B. Mono 로 변경 | 바로 빌드 가능, 빌드 빠름, 이전 측정(Mono)과 같은 조건 | — 출시 때 결국 전환해야 하고 그때 문제를 늦게 안다 |

- 모듈 설치 후 다시 실패 (Windows SDK 없음) → SDK 설치 후 성공. 경위는 ISSUE-002.
- **빌드 결과 (IL2CPP, 빈 `GameScene`)**

  | | Release | Development |
  |---|---|---|
  | 빌드 시간 | 247 초 | 254 초 |
  | 에러 / 경고 | 0 / 2 | 0 / 4 |
  | 배포 크기 | 약 105MB (`GameAssembly.dll` 41MB · `UnityPlayer.dll` 37MB · `_Data` 27MB) | — |

  `..._BackUpThisFolder_ButDontShipItWithYourGame` (1.4GB) 는 IL2CPP 디버그 심볼 — 배포 제외.
- **릴리스 빌드 15 초 실행** (640×360 창, `-logFile Logs/player-release.log`): 종료 없이 살아 있음, **Exception 0**, D3D11 · GTX 1660.
  - 경고 4 건: `Shader 'Hidden/Universal Render Pipeline/{GaussianDepthOfField ×2, BokehDepthOfField, PaniniProjection}' is not supported or has been stripped from the build ... PostProcessing render passes will not execute.`
  - 원인: URP 전역 설정 `m_StripUnusedPostProcessingVariantsAndResources: 1` 이 **볼륨 프로파일에 없는 효과**의 셰이더를 뺐고, 그 패스만 건너뛴다는 안내. 정상 동작이라 이슈로 올리지 않았다.
  - ⚠ 나중에 이 효과(피사계 심도 등)를 **런타임에 코드로만** 켜면 빌드에서 동작하지 않는다. 빌드에 포함되는 볼륨 프로파일에 넣어 둬야 한다.
- 성능 수치: 빈 씬이라 측정하지 않았다 (의미 없는 숫자). 기준 측정은 WU-01 부터.

### 배운 것

- 툴 스크립트는 "복사해 오면 이전 수정이 다 들어 있다" 가 맞지만, **의존하는 패키지 버전이 바뀌면 그 수정들이 오히려 오판의 원인**이 된다 (ISSUE-001 — 이전 ISSUE-010 방어 로직이 새 형식에서 모든 실패를 삼켰다). 가져온 스크립트는 성공·실패 두 경로를 먼저 돌려 본다.

커밋: (사용자 커밋 대기)
