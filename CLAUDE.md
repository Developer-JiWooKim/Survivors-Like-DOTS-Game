# CLAUDE.md — 작업 지침

Unity 6 DOTS 기반 **3D 우주 슈터 + 서바이버 라이크**. 이전 2D 프로토타입(`Survivors-Like-DOTS-Prototype` 저장소)의 확장판이다.
기획서는 아직 없다 — 작업 단위가 쌓이면서 [Docs/GameDesign.md](Docs/GameDesign.md) 로 만든다.
이 프로젝트는 **기술 포트폴리오 / 면접 준비**가 목적 중 하나다. 결과물뿐 아니라 **과정과 판단 근거가 남는 것**이 동등하게 중요하다.
퀄리티 목표는 **출시 가능한 수준**, 마감은 없다.

| | |
|---|---|
| Unity | 6000.6.5f1 |
| 렌더 | URP 17.6 / Universal Renderer (Forward+) — `Assets/Settings/PC_Renderer.asset` |
| 핵심 스택 | Entities 6.6.0 / Entities.Graphics 6.6.0 / Collections 6.6.0 / Burst (Entities 의존성) |
| 기타 | Input System 1.20.1, UniTask (비동기 — 매니지드 쪽 전용), `com.unity.pipeline` 0.8.0-exp.1 (CLI 연결) |
| 스크립트 루트 | `Assets/MyAssets/Scripts/` |

**정해진 게임 방향 (2026-10-16)**

| 항목 | 결정 |
|---|---|
| 전투 | **혼합** — 주무기는 직접 조준, 보조 무기는 레벨업으로 얻는 자동 무기 |
| 시점·이동 | **3인칭 체이스캠 + 6DOF 자유 비행** |
| 진행 | **프로젝트의 끝을 정하지 않는다.** 작업 단위 하나를 끝내면 다음 작업을 사용자와 의논해 정한다 (6장) |

---

## 0. 세션 시작 시 (필수)

**새 세션에서 작업을 이어갈 때는 코드를 건드리기 전에 아래를 먼저 읽는다.**
이 파일만 자동으로 로드되고 나머지는 읽지 않으면 모르는 상태다.

```bash
sed -n '1,80p' Docs/WorkLog.md       # 최상단 = 📍 현재 위치. 진행 중인 작업 단위와 다음 후보가 여기 있다
sed -n '1,30p' Docs/IssueLog.md      # 이슈 목록 표. ⏳진행중 / ⚠️미해결 / 🔄우회 를 확인
git log --oneline -10                # 마지막 커밋 이후 무엇이 바뀌었는지
```

읽고 나면 **현재 작업 단위 / 직전 작업 / 다음 작업 / 미해결 이슈**를 짧게 요약해 확인받고 시작한다.
사용자가 "다음 작업 시작해" 라고만 말해도 이 절차를 거친다.

---

## 1. 작업 기록 (필수)

기록은 **두 문서로 분리**한다. 각 문서는 계속 누적하며, 더 쪼개지 않는다.

| 문서 | 담는 것 |
|---|---|
| [Docs/WorkLog.md](Docs/WorkLog.md) | **날짜별 작업 내용** — 그날 무엇을 했고 왜 그렇게 했는지 |
| [Docs/IssueLog.md](Docs/IssueLog.md) | **발생한 이슈와 대응** — 언제 무슨 문제가 났고 어떻게 처리했는지 |

두 문서는 **이슈 ID(`ISSUE-00X`)로 상호 참조**한다. WorkLog 엔트리에 그날 발생한 이슈 ID를 적고, IssueLog 엔트리에는 관련 WorkLog 날짜를 적는다.

### 1.1 WorkLog — 날짜별 작업 내용

**기록 시점**: 작업 세션을 마칠 때, 성능 측정을 했을 때, 설계 판단이 내려졌을 때

**엔트리에 반드시 포함**
1. **작업 날짜** (YYYY-MM-DD) 와 해당 작업 단위 (`WU-0X`)
2. **작업 내용** — 무엇을 만들었는지, 어떤 파일이 생기고 바뀌었는지
3. **왜 그렇게 했는지** — 고려한 대안, 선택한 이유, 포기한 이유, 트레이드오프
4. **성능 수치** — 측정했다면 before·after, 측정 조건 (4장)
5. **배운 것 / 다음에 다르게 할 것**
6. **커밋 해시** — 나중에 코드와 기록을 대조할 수 있도록

**작업을 마칠 때는 WorkLog 상단의 `📍 현재 위치` 블록도 반드시 갱신한다.** 다음 세션이 이걸 먼저 읽는다.

### 1.2 IssueLog — 이슈와 대응

**기록 시점**: 이슈가 **발생한 시점에 먼저 등록**하고(상태 `⏳ 진행중`), 해결되면 그 엔트리를 갱신한다.
해결된 뒤에 몰아서 쓰면 시도했다 실패한 과정이 통째로 날아간다. **그 과정이 가장 가치 있는 부분이다.**

**엔트리에 반드시 포함**
1. **이슈 ID** (`ISSUE-00X`, 순번 부여, 재사용 금지)
2. **발생 일시**와 작업 단위, 분류
3. **상태** — ✅해결 / 🔄우회 / ⏳진행중 / ⚠️미해결 / 🔁재발
4. **증상** — 에러 메시지 원문, 프로파일러 수치 등 **관측된 사실 그대로**
5. **재현 조건** — 항상/간헐적, 특정 엔티티 수 이상 등
6. **원인**
7. **시도한 것과 각각의 결과** — 실패한 시도 전부 포함
8. **해결 방법**과 **재발 방지책**
9. **해결 여부와 해결일**

문서 상단의 **이슈 목록 표를 항상 최신으로 유지**한다.

### 1.3 기록 밀도

면접에서 "이 부분 왜 이렇게 하셨어요?" 라는 질문이 나왔을 때, **이 문서들만 다시 읽으면 답이 나오도록** 쓴다.
특히 **실패하고 되돌린 시도도 반드시 남긴다.** "A를 시도했으나 B 때문에 실패해서 C로 갔다"가 "C로 했다"보다 훨씬 가치 있다.

---

## 2. 판단은 반드시 사용자에게 (필수)

**작업 중 정해야 할 사안이 생기면 스스로 판단하지 말고 반드시 물어본다.**

물어봐야 하는 것:
- 설계·구조 선택 (자료구조, 시스템 분리 방식, 아키타입 구성 등)
- 기획서에 명시되지 않은 수치·규칙
- 기획서와 실제 구현이 충돌할 때
- 라이브러리·패키지 추가
- 기존 코드를 크게 바꾸거나 되돌려야 할 때
- 성능 목표와 코드 단순성이 충돌할 때

물어볼 때는 **선택지와 각각의 트레이드오프를 정리해서 제시**한다. 열린 질문이 아니라 판단에 필요한 재료를 갖춘 선택지를 준다. 추천안이 있으면 근거와 함께 밝히되, **결정은 사용자가 한다.**

막힌 사안이 있어도 그것과 무관한 작업은 먼저 진행한다.

### 2.1 설명 방식 (사용자에게 잘 통한 것)

- 추상적인 말보다 **값 하나를 줄 단위로 따라가는 표**.
- 이름을 바꿀 때는 "지금 이름 → 새 이름" 을 한 줄씩. 지울 줄은 코드 그대로 보여 준다.
  (슬래시 나열, "이 줄" 같은 지칭은 반대로 읽혔다)

---

## 3. 검증 — Unity CLI 사용

Claude는 CLI로 **컴파일과 테스트를 직접 검증한다.** 사용자에게 떠넘기지 않는다.

| 스크립트 | 에디터 | 속도 | 언제 |
|---|---|---|---|
| `bash Tools/unity-recompile.sh` | **켜져 있어야 함** | 빠름 | **기본값.** 컴파일 검증 |
| `bash Tools/unity-check.sh` | **닫혀 있어야 함** | 느림 | 위가 안 될 때 |
| `unity command run_tests --mode editor --filter_type assembly --filter SurvivorsLikeDOTS.Tests` | **켜져 있어야 함** | 빠름 | **테스트 기본값** |
| `bash Tools/unity-test.sh EditMode` | 닫혀 있어야 함 | 느림 | 위가 안 될 때 테스트 실행 |

- `unity-recompile.sh` 는 `com.unity.pipeline` 을 통해 **실행 중인 에디터에 재컴파일을 명령**한다.
  pipeline 0.8.0 의 응답 형식에 맞게 고쳐 두었다 ([ISSUE-001](Docs/IssueLog.md#issue-001)). 패키지를 올리면 에러 주입 테스트로 다시 확인한다.
- `unity-check.sh` 진입점: `Assets/MyAssets/Scripts/Editor/CI.cs` (`SurvivorsLikeDOTS.Editor.CI.CompileCheck`)
- 빌드: 에디터 켜짐 → `unity command build`, 꺼짐 → `-executeMethod SurvivorsLikeDOTS.Editor.CI.BuildPlayers` (`Builds/Release`, `Builds/Development`)
- 관련 스킬: `.claude/skills/unity-cli`, `.claude/skills/unity-pipeline`

### 3.1 반드시 지킬 것

- **C# 코드를 작성·수정한 뒤에는 반드시 컴파일을 검증한다.** 돌리기 전에 "컴파일된다"고 말하지 않는다.
- **먼저 `unity-recompile.sh` 를 시도한다.**
- **`unity-recompile.sh` 가 exit 3 (에디터 미연결)이면** 두 가지 경우다:
  1. 에디터가 꺼져 있다 → `unity-check.sh` 를 쓴다.
  2. **에디터가 컴파일 에러로 Safe Mode 에 들어갔다** → Pipeline 이 로드되지 않아 연결 자체가 안 된다. 이때도 `unity-check.sh` 로 원인을 찾는다.
- `unity-check.sh` 는 에디터 락 때문에 에디터가 켜져 있으면 exit 2 로 중단된다.
  이때 **임의로 Unity 프로세스를 죽이지 않는다.** 사용자에게 닫아달라고 요청한다.

### 3.2 런타임 예외는 로그에서 확인한다

```bash
L="Logs/Editor.log"                            # 프로젝트 루트 기준
ls -la --time-style=long-iso "$L"             # 수정 시각이 방금인지 먼저 확인한다
grep -ac "Exception" "$L"                     # 건수
grep -a "Exception:" "$L" | cut -c1-200 | sort | uniq -c | sort -rn | head   # 종류와 횟수
```

**로그는 프로젝트의 `Logs/Editor.log` 에 쓰인다.** `AppData/Local/Unity/Editor/Editor.log` 는 부팅 초기 이후 갱신되지 않아,
거기서 "예외 0건" 이 나와도 아무 뜻이 없다 (이전 프로젝트에서 실제로 그렇게 잘못 보고했다 — 실제로는 2,839건).
건수를 말하기 전에 **파일의 수정 시각**을 확인한다. 스택 트레이스에서 **우리 코드의 파일:줄**을 찾는다.

### 3.3 CLI로 검증되지 않는 것 (사용자 확인 필요)

| 영역 | 이유 |
|---|---|
| 렌더링 결과, 드로우콜, GPU 시간 | 배치모드는 렌더 파이프라인이 돌지 않음 |
| 실제 프레임레이트 | 배치모드 수치는 빌드와 다름 |
| 조작감·카메라 멀미·재미·타격감 | 당연히 불가 |

### 3.4 씬은 사용자가, 에셋은 Claude가

| 대상 | 누가 |
|---|---|
| **씬 안의 오브젝트 배치·계층·컴포넌트 연결** | **사용자** (에디터에서 직접) |
| **에셋 파일 생성** — 머티리얼, ScriptableObject, 프리셋 등 | **Claude** (`AssetDatabase` API + CLI) |
| C# 스크립트 (Authoring / System / MonoBehaviour), asmdef | Claude |

씬 작업을 요청할 때는 **에디터에서 뭘 눌러야 하는지 단계로** 적는다 (GameObject 이름·계층, 붙일 컴포넌트와 값, 연결할 참조).

- `*.meta`, `*.unity`, `*.prefab` 은 **직접 편집하지 않는다.** 예외: `Packages/manifest.json` (편집 후 리졸브).
- 새 파일의 `.meta` 는 에디터 임포트가 생성한다. Claude가 만들지 않는다.

### 3.5 외부 에셋 (Asset Store)

| 에셋 | 경로 | 비고 |
|---|---|---|
| Hi-Rez Spaceships Creator Free Sample | `Assets/HiRezSpaceshipsCreatorFree/` | 머티리얼 22개 URP Lit, `SpaceBG.mat` 만 내장 스카이박스 셰이더. 약 135MB |

- **라이선스상 재배포 불가 → `.gitignore` 로 제외한다.** 저장소를 새로 받으면 Package Manager > My Assets 에서 다시 임포트한다.
- **에셋 폴더 안의 파일은 고치지 않는다.** 고친 내용이 git 에 남지 않는다. 바꿀 게 있으면 `Assets/MyAssets/` 로 복사하거나 프리팹 배리언트를 만든다.

---

## 4. 성능 관련 원칙

- **측정하지 않은 성능은 주장하지 않는다.** 사용자가 에디터/빌드에서 측정한 값이 기준이다. CLI 수치는 참고용이며 출처를 명시한다.
- 측정 조건을 **고정하고 함께 기록**한다: 엔티티 수, 해상도, Editor/Build, Burst on/off, 스크립팅 백엔드(Mono/IL2CPP), 측정 PC.
- 측정 규칙: Profiler·Frame Debugger 끔, 일시정지 금지, 벤치마크는 고정 목표 수, **비교하는 두 조건 모두 Tris 를 기록해 검산**.
  숫자가 안 맞으면 측정 오류로 단정하지 않는다 — 측정 대상 자체의 효과였던 적이 있다.
- 최적화는 프로파일링 후에. 단 **풀링·공간 해시 같은 구조적 결정은 초기에** 정한다 — 나중에 끼워넣기엔 비용이 너무 크다.
- 아키타입 원칙(구조적 변경 최소화 등)은 아직 정하지 않았다. 정해지면 여기에 적고, 어겨야 할 상황이 오면 **먼저 물어본다.**

---

## 5. 코드 규약

- DOTS 시스템은 `ISystem` + `[BurstCompile]` 기본. managed 타입이 꼭 필요할 때만 `SystemBase`.
- **비동기는 UniTask.** 단 매니지드 코드(UI, 로딩, 연출)에서만. Burst/`ISystem` 안에서는 쓰지 않는다.
- **asmdef 를 사용한다.** 어셈블리명 = 네임스페이스 = `rootNamespace`. 파일명은 어셈블리명과 일치시킨다.
  | asmdef (= 파일명) | 위치 | 상태 |
  |---|---|---|
  | `SurvivorsLikeDOTS` | `Scripts/` | 생성 (런타임) |
  | `SurvivorsLikeDOTS.Editor` | `Scripts/Editor/` | 생성 (CLI 진입점 `CI.cs`) |
  | `SurvivorsLikeDOTS.Tests` | `Scripts/Tests/` | 첫 테스트를 쓸 때 생성 |
- **네임스페이스는 `SurvivorsLikeDOTS` 를 뿌리로, 하위 폴더를 `.폴더명` 으로 붙인다** (2026-10-16 사용자 결정 — 이전의 `Assets.` 부터 쓰는 방식은 너무 길었다).
  | 파일 | 네임스페이스 |
  |---|---|
  | `Assets/MyAssets/Scripts/Test.cs` | `SurvivorsLikeDOTS` |
  | `Assets/MyAssets/Scripts/Editor/CI.cs` | `SurvivorsLikeDOTS.Editor` |
  | `Assets/MyAssets/Scripts/Combat/DamageApplySystem.cs` | `SurvivorsLikeDOTS.Combat` |

  ⚠ `SurvivorsLikeDOTS` 아래에서 그냥 `Editor` 라고 쓰면 네임스페이스 `SurvivorsLikeDOTS.Editor` 로 해석된다.
  커스텀 인스펙터는 `: UnityEditor.Editor` 로 전부 쓴다.
- 클래스는 상속 의도가 없으면 `sealed`.
- 폴더 구조는 **기능 단위**로 (`Scripts/Flight/`, `Scripts/Combat/` …). 타입 단위(`Components/`, `Systems/`)로 몰지 않는다.
- 매직 넘버는 `IComponentData` 설정값 또는 상수로. 잡 안에 숫자를 박지 않는다.
- **코드 스타일은 [.editorconfig](.editorconfig) 가 강제한다.** 인덴트 4칸, Allman 중괄호, `private` 명시, private 필드 `_camelCase`.
- **주석은 한국어로, 왜(why)를 쓴다.**
- **테스트는 틀리면 디버깅이 지옥인 핵심 로직만.** (예: 공간 해시 조회 정확성 — 반드시 전수 비교, 피해 집계)

---

## 6. 진행 방식 — 작업 단위

**프로젝트의 끝은 정하지 않는다** (2026-10-16 사용자 결정). 대신 **작업 단위(WU)** 하나씩 끝내고, 끝날 때마다 다음 작업을 사용자와 의논해 정한다.

작업 단위 하나의 **완료 기준**:
1. 구현 + 컴파일 검증 (테스트가 있으면 테스트)
2. 사용자가 에디터에서 확인 (렌더·조작감 등 3.3 항목)
3. **빌드 1회 + 성능 수치 기록** — 빌드를 마지막에 몰아서 처음 돌리지 않는다 (이전 ISSUE-017)
4. WorkLog 기록 + `📍 현재 위치` 갱신
5. 다음 작업 단위 의논

- **작업 단위 범위를 지킨다.** 하는 도중 떠오른 다음 기능은 `📍 현재 위치` 의 "다음 후보" 에 적고 넘어간다.
- **한 번에 한 덩어리씩.** 하나 만들고 → 에디터에서 동작 확인 → 기록 → 다음.
- **커밋과 푸시는 사용자가 직접 한다.** Claude는 커밋하지 않는다.
- 커밋 메시지는 **Conventional Commits + 한국어** (feat / fix / perf / refactor / docs / chore / test).
  ```
  feat: 우주선 6DOF 비행 이동 구현
  fix: 체이스캠이 급선회 시 뒤집히는 문제 수정
  ```
- 답변은 한국어로.

---

## 7. 이전 프로젝트에서 겪은 함정 (미리 피할 것)

| 함정 | 대응 |
|---|---|
| URP **SSAO** 의 DepthNormal 프리패스가 지오메트리를 2배 제출 | 쓰지 않을 렌더러 기능은 **끄지 말고 제거** — 끄기만 하면 빌드에서 리소스가 스트리핑돼 검은 화면. 현재 `PC_Renderer` 에 SSAO 가 있다 → 첫 빌드 전에 처리 |
| 잡 완료 전에 메인 스레드가 컴포넌트를 읽어 예외 대량 발생 | 의존성 / `Complete` 지점을 명확히 |
| UI(managed)에서 `SetSingleton` → "access is not read-write" | 쓰기 접근 경로를 따로 둔다 |
| 공간 해시 키 충돌로 여러 칸 조회 시 같은 대상 중복 집계 | 조회 정확성은 **전수 비교 테스트** |
| Bash heredoc 으로 마크다운 작성 시 셸 파싱 실패 | 문서는 Write 도구로 쓴다 |

이전 저장소의 상세 기록: `../Survivors-Like-DOTS-Prototype/Docs/` (WorkLog, IssueLog, DOTS_Study_Summary, GameDesign).
