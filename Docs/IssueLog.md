# 이슈 기록 (Issue Log)

Unity 6 DOTS 2D 뱀서라이크 — **발생한 이슈와 대응** 기록.
날짜별 작업 내용은 [WorkLog.md](WorkLog.md) 참조.

이슈 ID는 `ISSUE-001` 부터 순번으로 부여하며, **한 번 부여한 번호는 재사용하지 않는다.**

**상태 범례**

| 상태 | 의미 |
|---|---|
| ✅ 해결 | 원인을 파악하고 고쳤음 |
| 🔄 우회 | 근본 원인은 미해결. 임시 방편으로 진행 중 (언젠가 돌아와야 함) |
| ⏳ 진행중 | 현재 대응 중 |
| ⚠️ 미해결 | 원인 미파악 또는 보류 |
| 🔁 재발 | 해결했다고 판단했으나 다시 발생 |

---

## 이슈 목록

| ID | 발생일 | 마일스톤 | 분류 | 제목 | 상태 | 해결일 |
|---|---|---|---|---|---|---|
| [001](#issue-001) | 2026-09-16 | M-1 | 툴링 | Bash heredoc으로 마크다운 작성 시 셸 파싱 실패 | ✅ 해결 | 2026-09-16 |
| [002](#issue-002) | 2026-09-16 | M0 | 툴링 | `unity-test.sh` 첫 실행이 exit 255로 종료 (Unity 로그는 정상) | ⚠️ 미해결 | - |
| [003](#issue-003) | 2026-09-16 | M0 | 툴링 | `unity-check.sh` 가 DOTS 분석기 에러(DC0061)를 놓침 | ✅ 해결 | 2026-09-16 |
| [004](#issue-004) | 2026-09-16 | M0 | 성능 | 드로우콜 카운터가 BRG 인스턴싱을 0 으로 보고 | ✅ 해결 | 2026-09-16 |
| [005](#issue-005) | 2026-09-16 | M0 | 렌더링 | 지오메트리가 2배로 제출됨 (인스턴스 20004 vs 엔티티 10002) | ⏳ 진행중 | - |

> 분류: `DOTS` / `성능` / `렌더링` / `빌드` / `툴링` / `Unity` / `게임로직` / `기타`

---

<details>
<summary>엔트리 템플릿 (펼치기)</summary>

```markdown
### ISSUE-00X

| | |
|---|---|
| **발생일시** | YYYY-MM-DD HH:MM |
| **마일스톤** | M? |
| **분류** | |
| **상태** | |
| **해결일** | |
| **소요 시간** | |
| **관련 작업** | WorkLog YYYY-MM-DD |
| **관련 커밋** | `hash` |

**증상**
(무엇이 어떻게 잘못됐는지. 에러 메시지 원문, 프로파일러 수치 등 관측된 사실 그대로)

**재현 조건**
(어떤 상황에서 발생하는지. 항상/간헐적, 특정 엔티티 수 이상 등)

**원인**
(왜 발생했는지. 여기까지 도달한 과정도 함께)

**시도한 것**
| 시도 | 결과 |
|---|---|

**해결**
(최종적으로 어떻게 고쳤는지)

**재발 방지**
(같은 실수를 반복하지 않으려면)
```

</details>

---

## 이슈 상세

### ISSUE-001

| | |
|---|---|
| **발생일시** | 2026-09-16 |
| **마일스톤** | M-1 (기획) |
| **분류** | 툴링 |
| **상태** | ✅ 해결 |
| **해결일** | 2026-09-16 |
| **소요 시간** | 수 분 |
| **관련 작업** | [WorkLog 2026-09-16](WorkLog.md#2026-09-16--m-1-프로젝트-기획-및-작업-지침-수립) |
| **관련 커밋** | - |

**증상**

Bash heredoc(`cat > file <<'DOC' ... DOC`)으로 기획서 마크다운을 작성하자 파일이 생성되지 않고 셸 에러 발생.

```
/usr/bin/bash: -c: line 1: unexpected EOF while looking for matching `''
```

**재현 조건**

마크다운 본문에 코드 블록 백틱(```` ``` ````), 인라인 코드, 따옴표가 다량 포함된 긴 문서를 heredoc으로 작성할 때.

**원인**

`<<'DOC'` 로 따옴표를 감싸 변수 확장은 막았으나, 명령 전체가 `&&` 체인으로 한 줄에 전달되는 과정에서 본문 내 따옴표가 셸 파싱 단계에서 인용 부호로 해석됨.

**시도한 것**

| 시도 | 결과 |
|---|---|
| `<<'DOC'` 인용 heredoc | 실패 (동일 에러) |
| Write 파일 쓰기 도구로 전환 | ✅ 성공 |

**해결**

긴 마크다운/코드 파일 생성은 셸 heredoc 대신 파일 쓰기 도구를 사용.

**재발 방지**

- 여러 줄 마크다운·C# 코드 파일은 **항상** 파일 쓰기 도구로 생성한다.
- 셸은 파일 조회·검색·git 등 명령 실행 용도로만 사용한다.

---

### ISSUE-002

| | |
|---|---|
| **발생일시** | 2026-09-16 ~15:59 |
| **마일스톤** | M0 |
| **분류** | 툴링 |
| **상태** | ⚠️ 미해결 (재현되지 않음, 관찰 기록) |
| **해결일** | - |
| **관련 작업** | [WorkLog 2026-09-16 (M0)](WorkLog.md#2026-09-16--m0-unity-cli-검증-파이프라인-구축) |
| **관련 커밋** | - |

**증상**

`Tools/unity-test.sh EditMode` 최초 실행 시 스크립트가 **exit 255** 로 종료. `[*] EditMode 테스트 실행 중...` 이후 스크립트의 결과 출력 블록이 **전혀 실행되지 않음**.

그런데 Unity 쪽 로그는 정상 종료였다:

```
Test run completed. Exiting with code 0 (Ok). No tests were executed.
```

결과 XML(`Logs/test-results-EditMode.xml`, 750 byte)도 정상 생성되어 있었다.

**재현 조건**

재현되지 않음. 동일 명령을 재실행하니 정상 동작하고 exit 0 반환.

| 실행 | 결과 |
|---|---|
| 1회차 (asmdef 추가 직후) | exit 255, 스크립트 후처리 미실행 |
| 2회차 (동일 명령) | exit 0, 정상 출력 |

**원인**

미파악. 가설:
- Unity 배치모드 프로세스가 종료되는 과정에서 호출 측 셸 세션이 영향을 받음
- 1회차는 새 asmdef 임포트가 겹쳐 실행 시간이 길었음 → 타이밍 의존 가능성

**시도한 것**

| 시도 | 결과 |
|---|---|
| Unity 로그 확인 | Unity 자체는 exit 0 정상 종료 확인 |
| 결과 XML 존재·내용 확인 | 정상 생성됨 (`total="0"`) |
| XML 파싱 로직 단독 실행 | 정상 동작 (grep/wc 모두 정상) |
| 동일 명령 재실행 | ✅ 정상 (exit 0) |

→ **스크립트 로직과 Unity 양쪽 모두 문제없음을 확인.** 셸 프로세스 종료 경로 문제로 추정.

**해결**

미해결. 현재는 재실행으로 우회 가능.

**재발 방지 / 대응 방침**

- `unity-test.sh` 가 **출력 없이** 비정상 종료하면, 원인을 파고들기 전에 **`Logs/test-results-*.xml` 과 Unity 로그를 먼저 확인**한다. Unity 쪽이 정상이면 셸 문제이므로 재실행한다.
- 재발이 잦아지면 스크립트를 `run_in_background` 로 돌리거나 Unity 호출과 결과 파싱을 별도 명령으로 분리하는 것을 검토한다.
- **반복되면 상태를 🔁 재발 로 바꾸고 원인을 다시 판다.**

---

### ISSUE-003

| | |
|---|---|
| **발생일시** | 2026-09-16 |
| **마일스톤** | M0 |
| **분류** | 툴링 |
| **상태** | ✅ 해결 |
| **해결일** | 2026-09-16 |
| **관련 작업** | [WorkLog 2026-09-16 (M0)](WorkLog.md) |

**증상**

`BenchmarkSceneBuilder.cs` 의 asmdef 참조 누락으로 컴파일이 실패했는데, `unity-check.sh` 가 에러 요약을 출력하지 않고 "Unity 가 비정상 종료했습니다" 라는 일반 메시지만 띄웠다. 실제 에러는 로그 안에 있었다.

```
error DC0061: Assembly Assets.MyAssets.Scripts.Editor relies on Unity.Entities
which uses the Unity.Collections AllocatorHandle type but does not have a
reference to Unity.Collections.
```

**재현 조건**

Roslyn 표준 `CS####` 가 아닌 진단 코드로 컴파일이 실패할 때. 항상 재현된다.

**원인**

`unity-check.sh` 의 에러 추출 정규식이 `error CS[0-9]+` 로 **접두사를 `CS` 로 가정**하고 있었다.
DOTS 소스 제너레이터는 `DC####`, Burst 는 `BC####`, Unity 분석기는 `UNT####` 를 쓴다.
DOTS 프로젝트에서는 오히려 이쪽 에러가 더 자주 난다.

**시도한 것**

| 시도 | 결과 |
|---|---|
| 로그를 직접 열어 확인 | 에러는 로그에 정상 기록되어 있었음 → 스크립트 파싱 문제로 특정 |

**해결**

접두사를 가정하지 않고 파일·줄 정보까지 포함해 추출하도록 변경.

```bash
ERRORS=$(grep -oE "[^ ]+\.cs\([0-9]+,[0-9]+\): error [A-Z]+[0-9]+: .*" "$LOG" | sort -u)
```

**재발 방지**

- 로그 파싱은 **관측한 한 가지 형식에 맞추지 말고** 범용 패턴을 쓴다.
- 검증 스크립트가 "실패했다"고만 하고 이유를 못 대면, 그건 스크립트의 결함으로 취급한다.

---

### ISSUE-004

| | |
|---|---|
| **발생일시** | 2026-09-16 |
| **마일스톤** | M0 |
| **분류** | 성능 |
| **상태** | ✅ 해결 |
| **해결일** | 2026-09-16 |
| **관련 작업** | [WorkLog 2026-09-16 (M0)](WorkLog.md) |

**증상**

M0 첫 벤치마크(엔티티 10,000) 측정 결과, 드로우콜·배치 수가 0 으로 보고됐다.

```
FPS   203.7  (4.91 ms)
Main  5.13 ms
Ents  10006
Batch 0        ← 0
Draw  0        ← 0
SetPs 10
```

`Batch 0` / `Draw 0` 인데 `SetPass 10` 은 논리적으로 모순이다. 아무것도 안 그렸다면 SetPass 도 0 이어야 한다.
**즉 렌더가 실제로 몇 개의 드로우콜로 처리됐는지 알 수 없는 상태**이고, 이 값이 M0 의 핵심 확인 사항(인스턴싱이 먹었는가)이다.

**재현 조건**

에디터 플레이 모드, URP(Universal Renderer), `PerfOverlay` 의 `ProfilerRecorder` 로 측정.

**원인 (가설, 미확정)**

`ProfilerCategory.Render` 의 `"Draw Calls Count"` / `"Batches Count"` 는 빌트인 렌더 파이프라인용 카운터로 보이며,
SRP(URP) 에서는 값이 채워지지 않는 것으로 추정된다. `"SetPass Calls Count"` 만 동작한 정황이 이를 뒷받침한다.

**시도한 것**

| 시도 | 결과 |
|---|---|
| `ProfilerRecorder(ProfilerCategory.Render, "Draw Calls Count")` | 0 반환 (Valid 는 true) |
| `ProfilerRecorder(ProfilerCategory.Render, "Batches Count")` | 0 반환 |
| `ProfilerRecorder(ProfilerCategory.Render, "SetPass Calls Count")` | 10 반환 (정상 동작) |

| `UnityStats.batches` 사용 시도 | ❌ Unity 6 에서 제거된 멤버 (`error CS0117`). `UnityEditor.dll` 을 직접 뒤져 실존 멤버 확인 |
| `UnityStats.drawCalls` / `triangles` | ✅ 정상 동작 (16 / 40252) |
| `UnityStats.instancedBatchedDrawCalls` | ❌ **0 반환** — 이게 핵심 단서였다 |
| Game 뷰 Statistics 창 확인 | ✅ `Draw Calls : 16 (20004 instances)` — 여기서만 인스턴스 수가 보임 |

**원인 (확정)**

두 개의 별개 문제가 겹쳐 있었다.

1. **ProfilerRecorder 의 `Draw Calls Count` / `Batches Count` 는 URP(SRP) 에서 채워지지 않는다.**
   `SetPass Calls Count` 만 동작한 것이 단서였다. → 에디터에서는 `UnityStats` 로 우회.

2. **`instancedBatchedDrawCalls` 가 0 인 것은 버그가 아니라 정상이다.**
   이 카운터는 **구형 GPU Instancing(MeshRenderer 경로)** 을 센다.
   Entities Graphics 는 **BatchRendererGroup(BRG)** 이라는 완전히 다른 경로로 그리기 때문에
   여기에 집계되지 않는다.

**해결**

- 에디터에서는 `UnityStats.drawCalls` / `setPassCalls` / `triangles` 를 오버레이에 표시.
- **BRG 인스턴싱 여부를 판정하는 기준은 `instancedBatchedDrawCalls` 가 아니라
  "드로우콜 수 대비 엔티티 수" 다.** 엔티티 10,002 개가 드로우콜 16 개로 나왔으므로 인스턴싱은 정상 동작.
- 정확한 인스턴스 수는 Game 뷰 Statistics 창의 `Draw Calls : N (M instances)` 에서 읽는다.

**재발 방지**

- **DOTS 렌더 성능은 구형 렌더러용 카운터로 판단하지 않는다.** BRG 는 별개 경로다.
- 카운터가 0 일 때 "렌더가 안 된다"고 결론내기 전에 **`triangles` 로 실제 그려지는지부터 확인**한다.
  이번에도 `Tris 40252` 가 "그려지고는 있다"를 먼저 확정해줬다.
- 에디터 API 멤버는 버전에 따라 사라진다. 추측하지 말고 `UnityEditor.dll` 에서 확인한다.

**남은 한계**

`UnityStats` 는 에디터 전용이라 빌드에서는 못 쓴다. 빌드 실측 수단은 M2 전까지 따로 확보해야 한다.

---

### ISSUE-005

| | |
|---|---|
| **발생일시** | 2026-09-16 |
| **마일스톤** | M0 |
| **분류** | 렌더링 |
| **상태** | ⏳ 진행중 (동작에는 문제 없음, 최적화 여지) |
| **해결일** | - |
| **관련 작업** | [WorkLog 2026-09-16 (M0)](WorkLog.md) |

**증상**

엔티티 수 대비 렌더 제출량이 정확히 2 배다.

| 값 | 기대 | 실제 |
|---|---|---|
| 인스턴스 | 10,002 | **20,004** |
| 삼각형 | 20,000 (쿼드 1만 × 2) | **40,252** |
| 정점 | 40,000 | **80,500** |

Game 뷰 Statistics 원문:
```
Triangles : 40.3k
Vertices : 80.5k
Set Pass Calls : 10
Draw Calls : 16 (20004 instances)
```

**재현 조건**

에디터 플레이 모드, URP Universal Renderer(PC_Renderer), 엔티티 10,000.

**원인 (가설, 미확정)**

URP Universal Renderer 의 **깊이 프리패스(Depth Priming / DepthNormals prepass)** 가
같은 지오메트리를 한 번 더 제출하는 것으로 추정된다. 제출량이 정확히 2 배인 것이 이를 뒷받침한다.

2D 탑다운 뷰는 깊이 복잡도가 거의 없어 프리패스의 이득이 없다. 오히려 순수 비용일 가능성이 높다.

**대응 방침**

지금은 성능에 여유가 있어(2.94ms / 340 FPS) 문제가 되지 않는다. **M2 에서 적 1만 + 이동 + 충돌이
얹혀 프레임이 빠듯해지면** `PC_Renderer` 의 Depth Priming 설정을 끄고 before/after 를 측정한다.
지금 끄면 무엇 덕분에 빨라졌는지 구분이 안 되므로 미룬다.
