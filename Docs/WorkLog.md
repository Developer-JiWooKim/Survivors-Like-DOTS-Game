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
| **⚠ 작업 방식** | **사용자가 직접 만든다** (2026-10-16 사용자 요청 — "내 손으로 만들어야 의미 있다"). Claude 는 작업 전에 할 일 목록을 주고 **누가 할지 묻는다.** 사용자가 하는 작업은 설계 설명·힌트·리뷰·컴파일 검증만 하고 코드를 대신 쓰지 않는다 |
| **진행 중** | **WU-01 우주선 6DOF 비행 + 체이스캠** — 아래 체크리스트. 지금 **6번 `PlayerAimSystem` b단계에서 멈춤** (사용자 작성 중) |
| **다음 할 일** | ① `PlayerAimSystem` 마무리 (아래 "이어서 할 것") ② 5번 `ShipFlightSystem` 본체 ③ 씬 구성 → 플레이 확인. 5·6번 설계·수식 표는 2026-10-16 엔트리 9절 |
| **정해진 방향** | 전투 = 주무기 직접 조준 + 보조 무기 자동(레벨업) · 시점 = 3인칭 체이스캠 6DOF · 비행 = 보조 관성 · 마우스 조준점 방식 · 끝을 정하지 않고 작업 단위로 확장 (CLAUDE.md 6장) |
| **아직 안 정한 것** | 아키타입 원칙 · 공간 해시 2D/3D · 성능 목표 수치(적 수) · 기획서 |
| **미해결 이슈** | 없음 (이전 프로젝트에서 넘어온 툴링 이슈 2건은 [IssueLog](IssueLog.md) 상단 참조) |
| **포맷 후 재시작** | 2026-10-16 이후 PC 포맷 예정. 아래 "포맷 후 다시 시작하기" 참조 |

**WU-01 체크리스트**
```
✅ 0  WU-00 완료 (SSAO 제거, IL2CPP 첫 빌드)
✅ 1  입력 액션 Ship 맵 (Thrust 3D, Roll, Aim, Fire, Boost, ReleaseCursor) — Claude
✅ 2  데이터 컴포넌트 ShipInput / ShipFlight / ShipVelocity / ShipControl — Claude
✅ 3  ShipInputSystem (입력 → ECS 싱글턴, 커서 잠금) — Claude 작성, 사용자 검토
✅ 4  PlayerTag + PlayerShipAuthoring (Baker) — 사용자
✅ 6a PlayerAim / PlayerAimSettings 컴포넌트 + Authoring 추가 — 사용자
⏳ 6  PlayerAimSystem — 사용자 작성 중 (a단계 완료, b단계 4번 줄까지)
⏳ 5  ShipFlightSystem — 사용자 (껍데기만 있음)
⬜ 7  ChaseCamera   ⬜ 8 조준 HUD   ⬜ 9 운석 머티리얼·프리팹   ⬜ 10 AsteroidField + 스폰
⬜ 11 씬 구성 (SubScene, 우주선 Example1NoInterior_Grey 배치, 카메라 far clip, 스카이박스)
⬜ 12 플레이 확인 → 빌드 + 성능 측정 → 기록
```

**이어서 할 것 — `PlayerAimSystem.cs` 리뷰 메모 (2026-10-16 마지막 상태, 컴파일은 통과)**
| 줄 | 지금 | 고칠 것 |
|---|---|---|
| 31 | `RefRW<LocalTransform>` | **`RefRO`** — 읽기만 한다. RW 로 두면 비행 잡과 쓰기 의존성이 생기고 변경 표시도 붙는다 |
| 41~42 | `math.mul(aim.ValueRO.Rotation, ...)` | **`aimRot`** 로 — 마우스 회전(38~39 줄)을 반영한 **새** 회전 기준이어야 한다. 지금은 한 프레임 전 값 |
| 44 다음 | (없음) | b단계 5~7번 줄: `t = 1 - exp(-RollFollowRate * dt)` → `newUp = normalizesafe(lerp(curUp, targetUp, t), curUp)` → `aimRot = quaternion.LookRotationSafe(aimFwd, newUp)` |
| 49 | `DesiredForward = aim.ValueRO.Rotation` (주석 처리됨) | 타입이 다르다 — `DesiredForward` 는 `float3`, 회전은 `quaternion`. **`aimFwd`** 를 넣는다. 이어서 `Thrust = input.Thrust`, `Roll = input.Roll` |
| 클래스 위 | (없음) | `[UpdateInGroup(typeof(SimulationSystemGroup))]`, `[UpdateBefore(typeof(ShipFlightSystem))]` |

그리고 `Flight/ShipFlightSystem.cs` 15 줄 `RequireForUpdate<PlayerTag>()` → `RequireForUpdate<ShipFlight>()` 권장 (비행은 적 우주선과 공유 — Flight 가 Player 를 참조하면 방향이 거꾸로).

**포맷 후 다시 시작하기**
1. 설치: Unity Hub → **Unity 6000.6.5f1** + 모듈 **Windows Build Support (IL2CPP)** · **Visual Studio** (C++ 데스크톱 개발 + **Windows SDK 10.0.19041 이상**) · Git · VS Code · unity CLI (`unity` 가 PATH 에 있어야 `Tools/*.sh` 가 동작 — 이전 저장소 `Docs/Setup.md` 4장 참조)
2. `git clone https://github.com/Developer-JiWooKim/Survivors-Like-DOTS-Game.git`
3. Unity Hub 로 프로젝트 열기 (첫 임포트 수 분)
4. **우주선 에셋 재임포트**: Package Manager > My Assets > Hi-Rez Spaceships Creator Free Sample → Import (gitignore 대상이라 저장소에 없다. 패키지 안 `.meta` 로 GUID 가 복원된다)
5. Claude 세션에서 "다음 작업 시작해" → CLAUDE.md 0장 절차대로 이 블록부터 읽는다. Claude 메모리(`~/.claude`)는 포맷으로 사라지므로 **규칙은 이 문서와 CLAUDE.md 에 있는 것이 전부**다
6. 이전 프로젝트(`Survivors-Like-DOTS-Prototype`)는 다시 받지 않아도 된다 — 필요한 건 옮겨 왔다. 참고할 일이 있으면 그때 clone. (옛 로컬 폴더의 origin 이 새 저장소를 가리키고 있었다 — 저장소 이름 변경 탓. 다시 받으면 해결)

---

## 2026-10-16 — WU-00 기반 세팅 · 게임 방향 결정 · WU-01 시작

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

### 9. WU-01 시작 — 설계 결정과 진행 (같은 날 이어서)

**결정 (사용자 — 전부 Claude 추천안 채택)**

| 항목 | 선택지 | 결정 · 이유 |
|---|---|---|
| 플레이어 우주선 | 엔티티 / GameObject | **엔티티 + `PlayerTag` 싱글턴** — 적·투사체 잡이 플레이어 위치를 바로 읽는다. 카메라만 GameObject 로 남아 매 프레임 렌더 직전 위치·회전을 복사 (경계는 이 한 곳) |
| 비행 모델 | 아케이드 / 관성 / 보조 관성 | **보조 관성** — 속도가 "원하는 속도" 를 `Acceleration` 만큼씩 따라간다. 값 하나로 아케이드 ↔ 관성 사이 조절 |
| 마우스 | A 조준점 (우주선이 따라 돔) / B 회전 속도 | **A** — 직접 조준 주무기와 맞음 |
| 키 배치 | — | W/S 전후 · A/D 좌우 · Space/Ctrl 상하 · Q/E 롤 · 마우스 조준 · 좌클릭 발사 · Shift 부스트(보류) · Esc 커서 해제 |
| 진행 | A 비행만 먼저 + 임시 입력 연결 / B 비행 + 조준 함께 | **B** (사용자) — 임시 코드 없이 5·6번을 같이 |

**왜 `PlayerTag` 를 따로 두나**: 이전 프로젝트는 플레이어 전용 컴포넌트로 구분해 태그가 없었다. 이번엔 비행 컴포넌트를 적 우주선도 쓰기 때문에 그것만으로는 플레이어를 가려낼 수 없다.

**왜 `ShipControl` 을 두나**: 비행 시스템이 `ShipInput` 을 직접 읽지 않게 한다. 플레이어는 조준 시스템이, 적은 AI 가 같은 컴포넌트를 채운다.

**우주선 에셋 측정** (배치모드 임시 스크립트, 측정 후 삭제)

| 프리팹 | 크기 (m) | 렌더러 | Tris |
|---|---|---|---|
| `Example1NoInterior_Grey` (**플레이어용 추천**) | 13.0 × 3.9 × 12.5 | 20 | 4,802 |
| `Example1_Grey` (조종석 내부 포함) | 같음 | 73 | 22,670 |

모듈은 Z 축으로 길다 → 앞이 +Z 로 추정 (플레이로 확인 필요). 콜라이더 21 개가 붙어 있으나 Unity Physics 패키지가 없어 베이킹되지 않는다.
비행 기본값(전진 60 · 후진 20 · 좌우 30 m/s · 가속 80 m/s² · 선회 90°/s · 롤 120°/s)은 이 크기 기준 첫 추정치.

**5·6번 수식 (사용자에게 준 설명의 요지 — 이어서 작업할 때 참고)**
- 5 ① 선회: `fwd = mul(rot, forward)` → `angle = acos(clamp(dot(fwd, desired), -1, 1))` → `axis = cross(fwd, desired)` → `step = min(angle, TurnRate·dt)` → `rot = mul(AxisAngle(normalize(axis), step), rot)` (**월드 기준이라 앞에 곱함**). angle≈0 이면 건너뛰고, 정반대(dot≈-1)면 축을 기체 위 방향으로.
- 5 ② 롤: `rot = mul(rot, RotateZ(-Roll·RollRate·dt))` (**로컬 기준이라 뒤에 곱함**, Unity Z+ 는 뒤에서 봐서 반시계라 부호 반전) → `normalize(rot)`.
- 5 ③ 이동: 로컬 원하는 속도 (x,y × Strafe, z × Forward/Reverse) → 월드로 → `v` 를 `Acceleration·dt` 만큼 MoveTowards → `pos += v·dt`. 정지→최고속 약 0.75 초.
- 6 a: `aim = mul(aim, RotateY(dx·k))`, `aim = mul(aim, RotateX(-dy·k))` — 화면 기준이라 뒤에 곱함, 마우스 delta 엔 dt 를 곱하지 않음.
- 6 b: 조준 up 을 기체 up 쪽으로 지수 감쇠 (`1 - exp(-rate·dt)`, FPS 독립) → `LookRotationSafe(aimFwd, newUp)`. 없으면 Q/E 롤 때 화면에서 기체만 기울고, 마우스 축과 기체 축이 어긋난다.
- 6 c: `ShipControl` ← `DesiredForward = aimFwd`, `Thrust`, `Roll`.

**베이킹 의존성 (사용자 질문)**: Baker 안에서 `authoring.transform` 을 직접 읽으면 의존성이 등록되지 않아, SubScene 을 연 채 우주선을 돌려도 그 Baker 가 다시 돌지 않는다 → `LocalTransform` 은 새 방향인데 `ShipControl.DesiredForward` 는 옛 방향. `Baker.GetComponent<Transform>()` 을 거치면 등록된다. (Entities 6.6 에서 자기 GameObject 의 Transform 이 따로 추적되는지는 미검증 — 플레이 확인 때 SubScene 연 채 회전시켜 Entities Hierarchy 로 확인 가능)

**작업 방식 변경 (사용자 요청)**: 1~3 번은 요청 전에 Claude 가 이미 작성했다. 이후 "내 손으로 만들어야 의미 있다" 는 요청으로 4 번부터 사용자가 직접 작성 — Claude 는 할 일 목록 → 사용자 선택 → 설명·리뷰·컴파일 검증.

**기타 결정**
- 우주선 에셋은 계속 gitignore (공개 저장소 — Asset Store EULA). 포맷 후 My Assets 에서 재임포트.
- 빌드 크기 약 105MB 의 대부분은 엔진·패키지 코드 (`GameAssembly.dll` 41MB 등). 스트리핑 수준 조정·패키지 정리는 출시 가까이 별도 작업으로 (측정 비교 필요). → 다음 후보

### 배운 것

- 툴 스크립트는 "복사해 오면 이전 수정이 다 들어 있다" 가 맞지만, **의존하는 패키지 버전이 바뀌면 그 수정들이 오히려 오판의 원인**이 된다 (ISSUE-001 — 이전 ISSUE-010 방어 로직이 새 형식에서 모든 실패를 삼켰다). 가져온 스크립트는 성공·실패 두 경로를 먼저 돌려 본다.

커밋: (사용자 커밋 대기)
