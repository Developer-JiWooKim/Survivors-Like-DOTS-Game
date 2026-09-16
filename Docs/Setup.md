# 개발 환경 세팅 (Setup)

다른 컴퓨터에서 이 프로젝트를 이어서 작업하기 위한 환경 재현 문서.
**위에서부터 순서대로** 진행하면 된다.

---

## 1. 필요한 것

| | 버전 / 비고 |
|---|---|
| **Unity** | **6000.6.0f1** (정확히 이 버전. 다르면 프로젝트 업그레이드가 강제되고 되돌릴 수 없다) |
| Unity Hub | 최신 |
| Git | 최신 (Git Bash 포함 설치 — CLI 스크립트가 bash를 쓴다) |
| VS Code | 최신 |
| .NET SDK | 별도 설치 불필요 (Unity 동봉 + VSCode 확장이 처리) |

Unity Hub 설치 시 **함께 체크할 모듈**:
- `Windows Build Support (IL2CPP)` — PC 빌드용
- `Microsoft Visual Studio` 는 **체크 해제** (VS Code를 쓴다)

> 프로젝트 버전은 `ProjectSettings/ProjectVersion.txt` 에서 확인할 수 있다.

---

## 2. 설치 순서

### 2.1 저장소 클론

```bash
git clone <repo-url>
cd Survivors-Like-DOTS-Game
```

`Library/`, `Temp/`, `Logs/`, `UserSettings/` 는 `.gitignore` 처리되어 있어 클론 직후에는 없다. 정상이다. Unity가 첫 실행 때 생성한다.

### 2.2 Unity로 첫 실행

1. Unity Hub → `Add` → 클론한 폴더 선택
2. 에디터 버전이 `6000.6.0f1` 로 잡히는지 확인 (다른 버전이면 설치부터)
3. 프로젝트 열기

**첫 실행은 수 분~십수 분 걸린다.** 패키지 리졸브 + 전체 에셋 임포트 + `Library/` 생성이 한꺼번에 일어난다. 진행 바가 멈춘 것처럼 보여도 기다린다.

패키지는 `Packages/manifest.json` 기준으로 자동 복원되므로 따로 설치할 게 없다.

### 2.3 Unity 에디터 설정

**Edit > Preferences > External Tools**
- `External Script Editor` → **Visual Studio Code** 선택
- 목록에 없으면 `Browse...` 로 `Code.exe` 직접 지정

이 설정을 해야 `.csproj` 와 `.vscode/` 설정이 Unity에 의해 생성·갱신된다.

---

## 3. VS Code 확장

`.vscode/extensions.json` 에 권장 확장이 들어 있어, 프로젝트를 열면 VS Code가 설치를 제안한다.

### 필수

| 확장 ID | 역할 |
|---|---|
| `visualstudiotoolsforunity.vstuc` | Unity 공식. 디버거 어태치, csproj 연동 |
| `ms-dotnettools.csdevkit` | C# Dev Kit (위 확장이 요구함) |
| `ms-dotnettools.csharp` | C# 언어 지원 |
| `ms-dotnettools.vscode-dotnet-runtime` | .NET 런타임 (자동 설치됨) |

한 줄로 설치:

```bash
code --install-extension visualstudiotoolsforunity.vstuc \
     --install-extension ms-dotnettools.csdevkit \
     --install-extension ms-dotnettools.csharp
```

### 선택

| 확장 ID | 언제 |
|---|---|
| `slevesque.shader` | `.shader` / `.hlsl` 작성 시 (M2 인스턴싱 머티리얼, M3 지형 셰이더) |
| `bierner.markdown-mermaid` | 문서에 다이어그램 그릴 때 |
| `eamodio.gitlens` | 커밋 이력 ↔ 작업 기록 대조 |

### 디버깅

`.vscode/launch.json` 에 `Attach to Unity` 설정이 들어 있다. Unity 에디터를 켠 상태에서 F5를 누르면 어태치된다.

---

## 4. CLI 검증 스크립트

**경로 설정은 필요 없다.** `Tools/_common.sh` 가 자동으로 탐지한다.

| 대상 | 탐지 방법 |
|---|---|
| 프로젝트 루트 | git 루트 → 실패 시 스크립트 상위 폴더. `ProjectSettings/ProjectVersion.txt` 존재로 검증 |
| 에디터 버전 | `ProjectVersion.txt` 에서 읽음 |
| `Unity.exe` | Unity Hub 보조 설치 경로(`%APPDATA%/UnityHub/secondaryInstallPath.json`) + C/D/E/F 드라이브 기본 위치 탐색 |

어느 위치에 클론해도, 어느 디렉터리에서 실행해도 동작한다.

### 자동 탐지가 실패하면

Unity를 찾지 못하면 설치된 버전 목록과 함께 에러가 뜬다. 특이한 위치에 설치했다면 `UNITY_PATH` 로 직접 지정한다.

```bash
UNITY_PATH='/d/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' bash Tools/unity-check.sh
```

> 프로젝트 경로를 하드코딩하지 않은 이유: 경로가 틀리면 Unity가 에러를 내는 게 아니라
> **그 경로에 새 빈 프로젝트를 생성해버린다.** 실패보다 나쁜 결과라서 탐지 + 검증 방식으로 만들었다.

---

## 5. 동작 확인

여기까지 하고 나면 아래가 전부 성공해야 한다.

```bash
# 1) 컴파일 검증  → [OK] 컴파일 성공
bash Tools/unity-check.sh

# 2) 테스트 실행  → [OK] 전체 통과
bash Tools/unity-test.sh EditMode
```

**실행 전 Unity 에디터를 반드시 닫아야 한다.** Unity는 한 프로젝트를 두 인스턴스가 열 수 없어서, 에디터가 켜져 있으면 스크립트가 `exit 2` 로 중단된다.

최초 실행은 임포트 때문에 수 분 걸릴 수 있다.

---

## 6. 알려진 현상 / 트러블슈팅

### `.slnx` 파일이 계속 수정됨으로 잡힌다
Unity가 패키지 설치·스크립트 변경 때마다 자동 재생성하는 파일이다. `.gitignore` 는 `*.sln` 만 무시하고 `*.slnx` 는 포함하지 않아 추적된다. **의도된 상태이며 무시해도 된다.** (커밋에서 빼고 싶으면 `git restore <file>`)

### VS Code에 빨간 줄이 뜨는데 컴파일은 성공한다
Entities 도입 후 자주 생긴다. `SystemAPI.Query<>()` 같은 코드는 **소스 제너레이터**가 컴파일 타임에 생성하는데, C# Dev Kit이 이를 못 따라가는 경우가 있다.

**판정 기준은 `Tools/unity-check.sh` 다.** VS Code 표시보다 이쪽을 믿는다.
그래도 거슬리면 Unity에서 `Edit > Preferences > External Tools > Regenerate project files`.

### `Tools/*.sh` 가 출력 없이 비정상 종료한다
`Logs/test-results-*.xml` 과 `Logs/cli-*.log` 를 먼저 확인한다. Unity 쪽이 정상 종료(`Exiting with code 0`)했다면 셸 문제이므로 그냥 재실행한다. (→ [IssueLog ISSUE-002](IssueLog.md#issue-002))

### `Logs/` 폴더가 VS Code 탐색기에 안 보인다
`.vscode/settings.json` 의 `files.exclude` 에 숨김 처리되어 있다. **의도된 설정이다.** CLI 로그를 직접 봐야 하면 터미널에서 `cat Logs/cli-compile.log` 로 읽는다.

### 에디터를 닫았는데도 `exit 2` (락)가 난다
Unity 프로세스가 완전히 종료되지 않은 경우다. 확인:

```bash
tasklist | grep -i "^Unity.exe"
```

참고로 락 검사는 **다른 프로젝트를 열어둔 Unity도 걸러낸다.** 오탐이지만, 잘못 실행해서 프로젝트가 깨지는 것보다 안전한 쪽을 택했다.

### `Unity ...을 찾지 못했습니다` 가 뜬다
`ProjectVersion.txt` 의 버전과 실제 설치된 버전이 다른 경우가 대부분이다. 에러 메시지에 **설치된 버전 목록이 함께 출력**되므로 대조해서 Unity Hub에서 맞는 버전을 설치한다. 특이한 위치에 설치했다면 `UNITY_PATH` 로 지정한다 (4장 참조).

---

## 7. 현재 프로젝트 상태

| | |
|---|---|
| 마일스톤 | **M0 진행 중** (환경 구축) |
| 설치된 주요 패키지 | URP 17.6, Input System 1.20, Test Framework 1.8 |
| **미설치** | **Entities / Entities.Graphics / Burst / Collections** — M0에서 설치 예정 |
| asmdef | `Assets.MyAssets.Scripts.Editor` (에디터 전용) 만 존재. `...Runtime` 은 M0에서 생성 |

DOTS 패키지가 설치되면 `Packages/manifest.json` 변경이 git으로 전파되므로, 다른 컴퓨터에서는 `git pull` 후 Unity를 열면 자동 복원된다.

---

## 관련 문서

- [CLAUDE.md](../CLAUDE.md) — 작업 지침
- [GameDesign.md](GameDesign.md) — 게임 기획서
- [WorkLog.md](WorkLog.md) — 날짜별 작업 기록
- [IssueLog.md](IssueLog.md) — 이슈 기록
