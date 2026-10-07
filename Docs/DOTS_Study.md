# DOTS 학습 노트

본 작업(M3)을 잠시 멈추고, 이 프로젝트에 쓰인 DOTS 개념을 **직접 설계해 보며** 익히는 기록.
면접에서 "ECS가 왜 빠른가요?", "구조적 변경이 뭔가요?" 같은 질문에 이 문서만 보고 답할 수 있게 쓴다.

| | |
|---|---|
| 시작일 | 2026-10-01 |
| 코드 위치 | `Assets/MyAssets/DOTS_Study/Scripts/StepXX/` |
| 어셈블리 | `Survivors.Study` (게임 `Survivors.Runtime` 과 서로 참조하지 않음 — 폴더째 지워도 게임에 영향 없음) |
| 네임스페이스 | `Assets.MyAssets.DOTS_Study.Scripts.StepXX` |
| 씬 | `Assets/MyAssets/DOTS_Study/Scenes/StepXX.unity` (단계마다 하나) |
| 진행 방식 | 개념 → 과제 명세 → **직접 작성** → 리뷰 → 프로젝트 코드와 비교 |

---

## 📍 현재 위치 (세션 이어갈 때 여기부터)

> 단계를 마치거나 세션을 끝낼 때마다 이 블록을 갱신한다.

| | |
|---|---|
| **완료** | 1단계 (2026-10-01), 2단계 (2026-10-02), 3단계 (2026-10-07) |
| **지금** | **4단계 — Job 과 Burst. 코드와 씬은 끝났고, 측정과 확인 질문이 남았다.** `Scripts/Step04/` 의 4개 파일(`JobChaserTag`, `JobChaserAuthoring`, `ChaseJob`, `JobChaseSystem`)은 리뷰를 마쳤고 컴파일 통과, 에디터 로그 예외 0건. `.Run()` / `.Schedule()` / `.ScheduleParallel()` 세 방식 모두 Step04 씬에서 정상 동작하는 것을 사용자가 확인했다 (2026-10-07). 현재 코드는 `.ScheduleParallel()` 상태 |
| **다음 행동** | **사용자가 할 일 두 가지.** ① 4.2절 "측정" 표의 **A·B·C·D 네 방식을 Profiler 로 재서** ms 를 알려 준다 (함께: Count, CPU 모델·코어 수, D 에서 `ChaseJob` 이 워커 몇 개로 나뉘었는지). ② 4.2절 **확인 질문 7개**에 답한다 (1·2·4·6번은 Profiler 화면을 보며 답하는 질문). → Claude 가 채점하고 4.3절을 완성한 뒤 5단계 과제를 쓴다 |
| **측정 메모** | A 는 Step03 씬에서 `ChaseSystem.OnUpdate` 의 `[BurstCompile]` 을 **잠깐** 주석 처리해 잰다 (재고 나서 되돌린다). Burst 를 끄는 에디터 메뉴 경로는 패키지 소스에서 확인하지 못했다 (Burst 2.0 은 패키지에 에디터 코드가 없음). Count 는 A 에서 추격 시스템이 1ms 이상 나오는 값으로 정해 네 번 모두 같게 쓴다. `Step04_Sub` 스포너의 회전 속도는 Min 0 / Max 0 이다 (측정에는 영향 없음) |
| **확인할 것** | 3단계 질문 5번(왜 플레이어 이동만 `SystemBase` 인가)은 처음 답이 틀렸고 설명만 들은 상태다. **자기 말로 다시** 설명하게 한다. 아직 안 했다 |
| **본 작업** | M3 는 학습이 끝날 때까지 멈춤. 상태는 [WorkLog.md](WorkLog.md) 의 현재 위치 블록 참조 |

### Claude 가 지킬 진행 규칙 (사용자와 합의한 것, 2026-10-01)

새 세션의 Claude 는 이 대화를 모른다. 아래를 그대로 따른다.

1. **DOTS 코드는 사용자가 직접 쓴다.** Claude 는 `DOTS_Study/Scripts/StepXX/` 의 `.cs` 를 작성·수정하지 않는다. Claude 가 만드는 건 셋업(asmdef)과 이 노트뿐이다.
2. **정답 코드를 먼저 주지 않는다.** 순서는 힌트 → 할 일을 번호로 쪼갠 절차 → (그래도 막히면) 해당 한두 줄의 문법 예시. 처음 보는 API 문법은 보여 줘도 된다. 설계 판단이 들어간 부분은 직접 쓰게 한다.
3. **사용자가 "맞아?" 라고 물으면** 파일을 읽고 `bash Tools/unity-recompile.sh` 로 컴파일을 확인한 뒤, 맞은 줄과 틀린 줄을 나눠 말한다. 틀린 곳은 **왜 틀렸는지와 실행하면 무슨 일이 생기는지**를 설명하고 고치는 건 사용자에게 맡긴다.
4. **확인 질문은 사용자가 먼저 답한다.** 채점하고, 틀린 답은 "처음 답 → 왜 틀렸나" 를 결과 절에 남긴다. 틀렸다가 고친 과정이 이 노트에서 가장 가치 있는 부분이다.
5. **단계를 마치면**: 결과 절(X.3)에 작성 경위 · 틀렸다가 고친 것 · 확인 질문 답 · 프로젝트 코드와 비교를 적고, 커리큘럼 표와 위의 현재 위치 블록을 갱신하고, 다음 단계의 개념·과제 절을 쓴다.
6. **비교용 프로젝트 코드는 과제가 끝난 뒤에** 열어 보게 한다 (먼저 보면 베끼게 된다).
7. 사용자는 ChatGPT 에도 물어 가며 한다. 괜찮다. 대신 도움받은 부분은 **자기 말로 다시 설명**하게 해서 확인한다.
8. 에디터 메뉴 경로는 **패키지 소스에서 확인하고** 안내한다. (Entities 6.6 은 예전 버전과 다르다. 예: Archetypes 는 Window → Search → Archetypes. Entities Hierarchy 는 "Deprecated" 표기)
9. 설명 수준: 사용자는 MonoBehaviour 는 익숙하고 ECS 는 처음이다. struct 복사, 베이킹과 런타임의 구분, "시스템은 데이터만 본다" 에서 자주 막혔다. 새 개념은 이 셋과 연결해 설명하면 잘 통한다.

---

## 커리큘럼

| 단계 | 개념 | 실습 | 프로젝트에서 쓰인 곳 | 상태 |
|---|---|---|---|---|
| 1 | Entity / Component / System, World, 아키타입·청크 | 큐브 회전 | 전체 구조의 기반 | ✅ 2026-10-01 |
| 2 | Baking 심화, 프리팹 엔티티, Instantiate | N개 스포너 | `EnemyAuthoring`, `EnemySpawnerAuthoring` | ✅ 2026-10-02 |
| 3 | 태그 컴포넌트, 쿼리 필터, 다른 엔티티 읽기, 시스템 순서, `SystemBase` | 플레이어 추격 | `EnemyChaseSystem`, `PlayerMoveSystem` | ✅ 2026-10-07 |
| 4 | Job·Burst (`IJobEntity`, `ScheduleParallel`, 의존성) | 3단계를 병렬로, Profiler 비교 | ISSUE-006, `PlayerPosition` | ⏳ 진행중 |
| 5 | 구조적 변경, ECB, Enableable 컴포넌트 | 생성·삭제 vs 풀링 | `Active`, `PoolUtility` | ⬜ |
| 6 | 싱글턴, 시스템 간 통신 | 데미지 이벤트 | `DamageEventBus`, `TerrainGrid` | ⬜ |
| 7 | NativeContainer, 공간 해시 | 가까운 적 찾기 | `EnemySpatialHash` | ⬜ |

---

## 1단계 — Entity / Component / System

### 1.1 개념

**MonoBehaviour 와 무엇이 다른가**

| | MonoBehaviour (GameObject) | ECS |
|---|---|---|
| "물체" | GameObject — 데이터와 로직을 함께 가진 객체 | **Entity** — 그냥 ID (번호 + 버전). 데이터도 로직도 없다 |
| 데이터 | 클래스 필드 | **Component** — `IComponentData` 를 구현한 **struct**. 로직 없음 |
| 로직 | 각 객체의 `Update()` | **System** — 같은 컴포넌트를 가진 엔티티 **전부**를 한 번에 처리 |
| 메모리 | 객체마다 힙 여기저기에 흩어짐 | 같은 종류끼리 **연속된 배열**에 모임 |
| 1만 개 처리 | `Update()` 1만 번 호출, 캐시 미스 다발 | 시스템 1번이 배열을 순서대로 훑음 |

**아키타입 (Archetype)**
엔티티가 가진 **컴포넌트 타입의 조합**이다. `{LocalTransform, RotationSpeed}` 를 가진 엔티티는 모두 같은 아키타입이다.
값이 달라도(속도 1 vs 5) 타입 조합이 같으면 같은 아키타입이다.

**청크 (Chunk)**
같은 아키타입의 엔티티를 담는 **16KB 메모리 블록**. 안에서 컴포넌트별로 배열이 나뉜다.

```
청크 (아키타입 {LocalTransform, RotationSpeed})
┌──────────────────────────────────────────────┐
│ Entity[]         : e0  e1  e2  e3 ...        │
│ LocalTransform[] : T0  T1  T2  T3 ...        │  ← 회전 시스템은 이 두 줄만
│ RotationSpeed[]  : S0  S1  S2  S3 ...        │     순서대로 읽는다
└──────────────────────────────────────────────┘
```

이 배치(SoA, Structure of Arrays)를 쓰는 이유는, 시스템이 필요한 컴포넌트 배열만 **앞에서부터 연속으로** 읽게 되기 때문이다.
그러면 CPU 캐시가 거의 항상 맞는다. **"ECS가 빠른 이유"의 핵심이 이것이다.**

**구조적 변경 (Structural Change)**
엔티티에 컴포넌트를 **추가하거나 빼면** 아키타입이 바뀐다. 그러면 그 엔티티의 데이터를 다른 청크로 **복사·이동**해야 한다.
비싸고, 그동안 다른 잡이 그 데이터를 만지면 안 되니까 동기화 지점(sync point)이 생긴다.
→ 이 프로젝트 기획서 8.4 의 "구조적 변경 금지" 원칙이 여기서 나온다 (5단계에서 자세히).

**World**
엔티티와 시스템을 담는 컨테이너. 플레이를 누르면 Default World 가 자동으로 만들어지고,
**프로젝트에 있는 모든 시스템이 자동으로 등록된다.** 어셈블리를 나눠도 마찬가지다.
→ 게임 시스템이 학습 씬에서도 돈다. 실제로 학습 씬의 Entities Hierarchy 에 `PlayerInputState` 엔티티가 보일 것이다
(`PlayerInputSystem` 이 조건 없이 싱글턴을 만들기 때문).

**System 이 언제 도는가**
기본값은 **매 프레임 무조건** `OnUpdate` 가 호출된다. 쿼리 결과가 비어 있어도 호출된다.
`OnCreate` 에서 `state.RequireForUpdate<T>()` 를 걸면 **T 를 가진 엔티티가 하나라도 있을 때만** 돈다.
→ 학습 시스템은 전부 이걸로 가드한다. 게임 씬에는 학습 컴포넌트가 없으니 자동으로 꺼진다.

**Baking**
에디터에서 **SubScene 안에 넣은 GameObject** 를 엔티티로 변환하는 과정.
- `Authoring` (MonoBehaviour) — 인스펙터에서 값을 넣는 용도. **런타임에는 존재하지 않는다.**
- `Baker<TAuthoring>` — Authoring 을 읽어서 엔티티에 컴포넌트를 붙인다.
- `TransformUsageFlags.Dynamic` — "이 엔티티는 움직인다". `LocalTransform` 과 `LocalToWorld` 가 붙는다.
- MeshRenderer 는 Entities Graphics 가 알아서 베이킹한다. 렌더링 코드는 따로 쓰지 않아도 된다.

**수학 타입**
ECS 컴포넌트는 Burst 가 다루기 쉬운 `Unity.Mathematics` 를 쓴다. `Vector3` → `float3`, `Quaternion` → `quaternion`, `Mathf` → `math`.

### 1.2 과제

**목표**: SubScene 안의 큐브 3개 이상이 **각자 다른 속도로** Z축 회전한다.

**작성할 파일** (`DOTS_Study/Scripts/Step01/`, 네임스페이스 `Assets.MyAssets.DOTS_Study.Scripts.Step01`)

| 파일 | 내용 |
|---|---|
| `RotationSpeed.cs` | `IComponentData`. 초당 회전량을 **라디안**으로 담는 `float` 하나 |
| `RotationSpeedAuthoring.cs` | MonoBehaviour. 인스펙터에서는 **도(°)/초**로 입력받는다. 안에 `Baker` 를 두고, 베이킹할 때 라디안으로 바꿔서 `RotationSpeed` 를 붙인다 |
| `RotationSystem.cs` | `ISystem`. `RotationSpeed` 가 있을 때만 돌게 가드하고, 매 프레임 `LocalTransform` 을 Z축으로 회전시킨다 |

**조건**
- 시스템은 `partial struct` + `[BurstCompile]`
- 아직 Job 은 쓰지 않는다. 메인 스레드 `foreach` 로 처리한다 (4단계에서 Job 으로 바꾼다)
- 클래스는 `sealed`, 주석은 한국어로 **왜**를 쓴다

**API 힌트** (이름만 줌. 쓰는 법은 직접 찾아보기)
- `GetEntity(TransformUsageFlags)`, `AddComponent(entity, value)` — Baker 안에서
- `state.RequireForUpdate<T>()` — `OnCreate` 에서
- `SystemAPI.Query<RefRW<A>, RefRO<B>>()`, `.ValueRW`, `.ValueRO`
- `SystemAPI.Time.DeltaTime`
- `LocalTransform.RotateZ(float)` — 회전된 **새 값**을 돌려준다 (struct 라서 그렇다)
- `math.radians()`

**씬 작업**
1. `DOTS_Study/Scenes/Step01.unity` 를 새로 만든다
2. Hierarchy 우클릭 → **New Sub Scene → Empty Scene** → `DOTS_Study/Scenes/Step01_Sub.unity` 로 저장
3. SubScene 안에 Cube 3개를 만들고 `RotationSpeedAuthoring` 을 붙인다. 속도는 서로 다르게 (예: 30, 90, 180)
4. 카메라가 큐브를 보도록 배치하고 Play
5. Play 중에 **Window → Entities → Hierarchy** 를 열고 큐브 엔티티를 클릭 → Inspector 에서 컴포넌트를 확인한다

**확인 질문** (리뷰 전에 스스로 답해 보기)
1. 큐브 3개의 속도가 전부 다르면 아키타입은 몇 개인가? 왜 그런가?
2. 큐브 하나에서만 Authoring 을 빼면 아키타입은 몇 개가 되는가? 시스템은 그 큐브를 어떻게 다루는가?
3. Play 중 Entities Hierarchy 에서 Authoring 컴포넌트가 보이는가? 왜 그런가?
4. `RotationSpeed` 를 `RefRW` 로 받아도 동작은 같다. 그런데도 `RefRO` 로 받는 이유는 무엇일까? (추측해 보기. 4단계에서 답이 나온다)
5. `RequireForUpdate` 를 빼고 **게임 씬**을 Play 하면 무슨 일이 생기는가?

### 1.3 결과 / 배운 것

**완료: 2026-10-01.** 큐브 3개가 서로 다른 속도로 회전하는 것을 확인했다. 학습 씬의 Entities Hierarchy 에 `PlayerInputState` 엔티티가 보이는 것도 확인했다 (게임 시스템이 학습 씬에서도 돈다는 증거).

**작성 경위** — `RotationSpeedAuthoring` 은 혼자 작성했다. `RotationSystem` 은 ChatGPT 에 물어 가며 작성했고, 리뷰에서 자기 말로 다시 설명해 확인했다.

**리뷰에서 고친 것**

| 처음 | 고친 것 | 이유 |
|---|---|---|
| 필드명 `radian` | `RadiansPerSecond` | 단위가 이름에 있어야 쓰는 쪽에서 `* deltaTime` 이 필요하다는 게 보인다. public 필드는 PascalCase |
| 루프 안에서 `SystemAPI.Time.DeltaTime` | 루프 밖 지역 변수 | 프레임 안에서 값이 같다. Job 안에서는 `SystemAPI` 를 못 쓰므로 어차피 밖에서 넘겨야 한다 |
| `transform.ValueRW.RotateZ(...)` | `transform.ValueRO.RotateZ(...)` | 읽는 쪽은 RO 로 써야 의도가 드러난다 |
| "여기에 ~ 작성" 주석 | 왜 필요한지 적은 주석 | 주석은 what 이 아니라 why |

**확인 질문 답**

1. **아키타입 1개.** 엔티티는 3개지만 컴포넌트 **타입 조합**이 같다. 값(속도)이 달라도 아키타입은 안 갈린다.
2. **아키타입 2개.** `RotationSpeed` 가 있는 것과 없는 것. 쿼리는 **요구한 컴포넌트를 전부 가진 아키타입만** 고른다. 그래서 `RotationSpeed` 가 없는 큐브는 쿼리에 안 잡히고 **회전하지 않는다.**
   ⚠️ 처음 답은 "다른 아키타입의 엔티티를 불러와 사용" 이었다 — 틀림. 쿼리는 조건에 안 맞는 아키타입을 **건너뛴다.**
3. **안 보인다.** Authoring 은 **베이킹 때**(에디터에서, 플레이 전) Baker 가 읽고 버린다. 런타임이 불러오는 건 베이킹 결과(엔티티 데이터)뿐이다.
   ⚠️ 처음 답은 "컴파일 시" 였다 — 컴파일(C# → DLL)과 베이킹(GameObject → 엔티티 데이터)은 다른 단계다.
4. 수정하지 않으니까 읽기 전용으로 받는다 — 맞다. 더 깊은 이유: 시스템이 "나는 이 컴포넌트를 **쓴다**" 고 선언하면, 같은 컴포넌트를 건드리는 다른 잡과 **동시에 못 돌고** 순서를 기다려야 한다. 읽기끼리는 동시에 돌 수 있다. (4단계에서 직접 확인)
5. 엔티티가 없어도 `OnUpdate` 가 매 프레임 호출된다. 이 시스템은 루프가 0번 돌 뿐이라 해는 없다. 하지만 **쿼리 없이 일하는 시스템**(엔티티 생성, 입력 읽기 등)은 실제로 엉뚱한 씬에서 일을 한다 — `PlayerInputState` 가 학습 씬에 생긴 게 그 사례다.

**추가로 배운 것**

- `LocalTransform` 은 struct 다. `RotateZ` 는 원본을 안 바꾸고 **복사본을 돌려준다.** `ValueRW` 에 다시 대입해야 청크 안의 실제 데이터가 바뀐다.
- `foreach` 한 바퀴는 엔티티 하나. 하지만 내부적으로는 **청크 단위로 배열을 받아** 그 안을 순서대로 돈다. `IJobEntity` 는 이 청크들을 스레드에 나눠 주는 것이다.

---

## 2단계 — 프리팹 엔티티와 스포너

### 2.1 개념

**프리팹도 엔티티다**
Baker 안에서 `GetEntity(프리팹 GameObject, flags)` 를 부르면 그 프리팹도 베이킹되어 **엔티티가 된다.**
이 엔티티에는 `Prefab` 태그가 자동으로 붙고, **`Prefab` 태그가 있는 엔티티는 모든 쿼리에서 기본으로 제외된다.**
그래서 프리팹 엔티티 자체는 회전하지도, 그려지지도 않는다. "틀" 로만 존재한다.

**컴포넌트는 다른 엔티티를 `Entity` 필드로 가리킨다**
GameObject 참조(클래스)는 컴포넌트에 못 넣는다. 대신 `public Entity Prefab;` 처럼 ID 를 담는다.

**Instantiate**
`EntityManager.Instantiate(prefab, count, allocator)` 는 프리팹 엔티티의 컴포넌트를 **통째로 복사**해 `count` 개를 만든다 (`Prefab` 태그만 빼고).
아키타입이 이미 정해져 있어서 청크에 메모리를 한 번에 채운다 → `Instantiate(GameObject)` 1만 번과 비교가 안 되게 빠르다.
엔티티를 만드는 건 **구조적 변경**이다. 그래서 잡 안이 아니라 메인 스레드에서 `EntityManager` 로 한다.

**싱글턴**
어떤 컴포넌트를 가진 엔티티가 월드에 **딱 하나**면 `SystemAPI.GetSingleton<T>()` 로 쿼리 없이 바로 읽는다.
0개거나 2개 이상이면 예외가 난다 → `RequireForUpdate<T>()` 와 짝으로 쓴다.

**한 번만 도는 시스템**
스폰은 한 번만 해야 한다. `OnUpdate` 끝에서 `state.Enabled = false;` 로 자기 자신을 끈다.

**TransformUsageFlags**

| 값 | 의미 | 붙는 컴포넌트 |
|---|---|---|
| `None` | 위치가 필요 없다 (설정값만 담는 엔티티) | 없음 |
| `Renderable` | 그려지지만 안 움직인다 | `LocalToWorld` |
| `Dynamic` | 런타임에 움직인다 | `LocalTransform` + `LocalToWorld` |

필요 없는 컴포넌트를 안 붙이면 엔티티 하나의 크기가 줄고, 청크 하나에 더 많이 들어간다.

**시스템은 씬을 모른다**
2단계 씬에는 1단계의 `RotationSystem` 을 따로 넣지 않는다. 스폰된 큐브가 `RotationSpeed` 를 가지고 있으면 **알아서 돈다.**
시스템은 "어느 씬인지" 가 아니라 "어떤 데이터가 있는지" 만 본다.

### 2.2 과제

**목표**: 스포너 하나가 큐브 프리팹을 **N개** 만들어 원 안에 무작위로 흩뿌린다. 각 큐브는 **무작위 속도로** 회전한다. N = 100 → 10,000 까지 올려 본다.

**작성할 파일** (`DOTS_Study/Scripts/Step02/`, 네임스페이스 `...Scripts.Step02`)

| 파일 | 내용 |
|---|---|
| `Spawner.cs` | `IComponentData`. 프리팹 엔티티, 개수, 반경, 난수 시드, 회전 속도 최소·최대 |
| `SpawnerAuthoring.cs` | 인스펙터에서 프리팹(GameObject)과 값들을 받아 Baker 에서 `Spawner` 로 변환 |
| `SpawnSystem.cs` | `Spawner` 싱글턴을 읽어 N개를 만들고, 각각의 위치와 회전 속도를 정한 뒤 **자기 자신을 끈다** |

**조건**
- 1단계의 `RotationSpeed`, `RotationSpeedAuthoring`, `RotationSystem` 은 **수정 없이 재사용**한다
- 난수는 `Unity.Mathematics.Random` (`UnityEngine.Random` 은 Burst 에서 못 쓴다)
- 같은 시드면 매번 같은 배치가 나와야 한다

**API 힌트**
- `GetEntity(GameObject, TransformUsageFlags)` — Baker 에서 프리팹을 엔티티로
- `SystemAPI.GetSingleton<T>()`
- `state.EntityManager.Instantiate(Entity, int, Allocator)` → `NativeArray<Entity>` (`Unity.Collections`)
- `state.EntityManager.SetComponentData(entity, value)`
- `LocalTransform.FromPosition(float3)`
- `new Random(seed)` (시드 0 은 안 된다), `.NextFloat(min, max)`, `.NextFloat2Direction()`
- `state.Enabled = false`

**씬 작업**
1. Cube 를 하나 만들어 `RotationSpeedAuthoring` 을 붙이고 `DOTS_Study/Prefabs/StudyCube.prefab` 으로 저장. 씬에서는 지운다
2. `DOTS_Study/Scenes/Step02.unity` + SubScene `Step02_Sub.unity`
3. SubScene 안에 빈 GameObject `Spawner` 를 만들고 `SpawnerAuthoring` 을 붙인다. 프리팹 칸에는 **Project 창의 프리팹**을 끌어다 넣는다
4. 개수 100 으로 Play → 확인되면 10,000 으로
5. Play 중에 **Window → Search → Archetypes** 를 열고 `c=RotationSpeed` 로 검색해 큐브 아키타입을 찾는다 (Entities 6.6 에서는 별도 창이 아니라 검색 창 기능이다)

**확인 질문**
1. Entities Hierarchy 에서 프리팹 엔티티를 찾아보자. 왜 그 엔티티는 회전하지 않는가?
2. `Spawner` 엔티티의 `TransformUsageFlags` 는 무엇으로 했는가? 왜 그런가?
3. `state.Enabled = false` 를 빼면 무슨 일이 생기는가? (예상한 뒤 **개수 100 으로** 직접 해 보기)
4. Archetypes 검색에서 큐브 10,000 개가 **청크 몇 개**에 담겼는가? 청크 하나에 엔티티가 몇 개 들어가는가? 예상(16KB ÷ 엔티티 크기)과 맞는가?
5. 위치를 정할 때 `NextFloat2Direction() * NextFloat(0, 반경)` 으로 하면 큐브가 **가운데에 몰린다.** 왜 그런가? 고르게 퍼지게 하려면? (DOTS 가 아니라 수학 문제. 프로젝트의 `RingSampler.cs` 가 같은 문제를 푼다)

### 2.3 결과 / 배운 것

**완료: 2026-10-02.** 큐브 100개, 10,000개 모두 원 안에 퍼져 각자 다른 속도로 회전하는 것을 확인했다.

**작성 경위** — `Spawner`, `SpawnerAuthoring` 은 혼자 작성하고 리뷰로 고쳤다. `SpawnSystem` 은 단계별 힌트를 받으며 작성했다. 위치 쓰기 한 줄과 sqrt 보정은 예시를 받았고, 나머지는 직접 썼다.

**틀렸다가 고친 것** (가장 가치 있는 부분)

| 처음 쓴 것 | 왜 틀렸나 | 고친 것 |
|---|---|---|
| Authoring 이 `_prefab` 을 받기만 하고 Baker 가 안 씀. `Spawner` 에 프리팹 필드 없음 | 런타임에는 GameObject 프리팹이 없다. 베이킹 때 엔티티로 바꿔 ID 를 넘겨야 한다 | `public Entity Prefab` + Baker 에서 `GetEntity(authoring._prefab, Dynamic)` |
| "프리팹을 `EntityManager` 로 변환한다" 고 생각 | 변환은 **Baker**(베이킹 때), 복제는 **EntityManager**(런타임). 역할이 다르다 | — |
| `Range = authoring._seed` | 오타. 타입이 맞아 컴파일러가 못 잡는다 | `_range` |
| `Seed` 가 `int`, 기본값 0 | `Unity.Mathematics.Random` 은 `uint` 를 받고 **시드 0 이면 예외** | `uint`, 기본값 1 |
| `uint seed = Unity.Mathematics.Random;` | 전역 함수가 아니라 **struct 생성기**다. 만들어서 들고 다녀야 한다 | `var random = new Random(spawner.Seed)` |
| 속도를 루프 **밖**에서 한 번 뽑음 | 모든 큐브가 같은 값을 받는다 | 생성기는 루프 밖에서 한 번, 값은 루프 안에서 매번 |
| `LocalTransform.FromRotation(new quaternion(0,0,speed*dt,0))` 로 스포너가 직접 회전시키려 함 | ① 스포너의 일은 **속도 데이터를 적는 것**, 돌리는 건 `RotationSystem` 의 일 ② `SetComponentData` 는 컴포넌트를 **통째로** 덮어써서 앞줄에서 쓴 위치가 사라진다 ③ 쿼터니언 숫자는 각도가 아니다 | `SetComponentData(e, new RotationSpeed { ... })` |
| "컴포넌트를 추가해야 하나?" | 프리팹에 이미 붙어 있어 복제본도 가지고 태어난다. `Set` 은 값 덮어쓰기(싸다), `Add` 는 구조적 변경(비싸다) | `SetComponentData` 유지 |

**확인 질문 답**

1. 프리팹 엔티티에는 `Prefab` 태그가 붙어 모든 쿼리에서 빠진다. `RotationSystem` 이 건너뛰므로 안 돈다. ✅
2. `TransformUsageFlags.None`. 스포너는 한 번 생성하고 꺼지며 움직이지 않는다. ✅
   트레이드오프: `LocalTransform` 이 없어서 **원의 중심이 항상 원점**이다. 씬에서 스포너를 옮겨도 반영되지 않는다.
3. `Enabled = false` 가 없으면 매 프레임 `Count` 개씩 계속 만든다. ✅
4. **청크당 64개, 청크 157개** (실측: Chunk Capacity 64, Unused Entities 48 → 157 × 64 = 10,048 = 10,000 + 48).
   - 예상은 "엔티티 300바이트 가정 → 54개" 였다. 실측 64개에서 역산하면 엔티티 하나가 **약 250바이트**다.
   - 우리가 붙인 건 `RotationSpeed` 4바이트뿐이다. 나머지는 `LocalTransform`, `LocalToWorld`, 렌더링용 컴포넌트가 차지한다.
   - 청크당 상한은 128개다 (`TypeManager.MaximumChunkCapacity`). 엔티티가 아무리 작아도 128개를 넘지 않는다.
   - **프리팹 엔티티는 혼자 청크 하나(16KB)를 쓴다** (Capacity 64, Unused 63). `Prefab` 태그 때문에 아키타입이 달라서다.
   - 도구 위치: Entities 6.6 에서는 **Window → Search → Archetypes**, 검색어 `c=RotationSpeed`. (예전 버전의 Window → Entities → Archetypes 창은 없어졌다)
5. 거리를 `NextFloat(0, R)` 로 고르게 뽑으면, 넓이는 거리의 제곱에 비례하므로 안쪽 절반 반경(넓이 25%)에 큐브 50% 가 몰린다.
   `sqrt(NextFloat(0, 1)) * R` 로 뽑으면 넓이 기준으로 고르게 된다. 적용 전후를 눈으로 비교해 확인했다.
   (처음에는 답을 몰랐다. 설명을 듣고 적용했다)

**프로젝트 코드와 비교** — [EnemySpawnSystem.cs](../Assets/MyAssets/Scripts/Runtime/Enemy/EnemySpawnSystem.cs)

| | 내 `SpawnSystem` | 프로젝트 `EnemySpawnSystem` |
|---|---|---|
| 뼈대 | `RequireForUpdate` → `GetSingleton` → `Instantiate` → `Enabled = false` | **같다** |
| 만든 뒤 | 위치·속도를 바로 써서 전부 살아 있다 | 아무것도 안 쓴다. 20,000 개를 **꺼진 채로** 만들어만 둔다 (풀) |
| 켜는 일 | 없음 | 다른 시스템(`EnemySpawnDirectorSystem`)이 시간에 맞춰 켠다 |
| `Enabled = false` 위치 | 맨 끝 | **맨 앞**. 중간에 `return` 해도 두 번 돌지 않게 |
| 방어 코드 | 없음 | `Prefab == Entity.Null`, `PoolSize <= 0` 이면 그냥 끝낸다 |
| 실행 시점 | 기본 (`SimulationSystemGroup`) | `[UpdateInGroup(typeof(InitializationSystemGroup))]` — 게임플레이 시스템보다 먼저 |

"꺼진 채로 만든다" 가 무슨 뜻인지, 왜 그러는지는 5단계(Enableable 컴포넌트)에서 다룬다.

---

## 3단계 — 플레이어 추격

### 3.1 개념

**태그 컴포넌트**
필드가 없는 빈 `IComponentData` struct. 데이터는 0바이트지만 **아키타입을 가른다.** "이 엔티티는 플레이어다" 같은 표시로 쓴다.

```csharp
public struct PlayerTag : IComponentData { }
```

**쿼리 필터**
값을 읽을 필요는 없고 "가지고 있는지" 만 따질 때 쓴다.

```csharp
SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>().WithAll<ChaserTag>()   // ChaserTag 도 있어야 한다
                                                          .WithNone<PlayerTag>()  // PlayerTag 는 없어야 한다
```

**다른 엔티티의 데이터 읽기**
추격자는 **플레이어의 위치**를 알아야 한다. 추격자 10,000 개가 각자 플레이어를 찾으면 낭비다.
→ 루프 **전에 한 번** 읽어 지역 변수에 담고, 루프 안에서는 그 값을 쓴다.

```csharp
Entity player = SystemAPI.GetSingletonEntity<PlayerTag>();          // 그 태그를 가진 유일한 엔티티의 ID
float3 target = SystemAPI.GetComponent<LocalTransform>(player).Position;
```

**시스템 실행 순서**
같은 그룹 안에서 시스템 순서는 **지정하지 않으면 보장되지 않는다.**
추격 시스템이 플레이어 이동보다 먼저 돌면 "한 프레임 전 위치" 를 쫓게 된다.

```csharp
[UpdateAfter(typeof(StudyPlayerMoveSystem))]
public partial struct ChaseSystem : ISystem
```

실제 순서는 Play 중 **Window → Entities → Systems** 에서 볼 수 있다.

**`ISystem` vs `SystemBase`**

| | `ISystem` | `SystemBase` |
|---|---|---|
| 형태 | `partial struct` | `partial class` |
| Burst | ✅ | ❌ |
| 매니지드 객체 (클래스, `Keyboard.current` 등) | ❌ | ✅ |
| 언제 | **기본값** | 매니지드 API 를 꼭 써야 할 때만 |

키보드 입력(Input System)은 매니지드 클래스라서 Burst 로 못 읽는다. 그래서 입력을 읽는 시스템만 `SystemBase` 로 만든다.

```csharp
public partial class StudyPlayerMoveSystem : SystemBase
{
    protected override void OnCreate() { RequireForUpdate<PlayerTag>(); }
    protected override void OnUpdate() { /* SystemAPI.Query 등은 똑같이 쓴다 */ }
}
```

**조합 (Composition)**
추격자 프리팹에 `RotationSpeedAuthoring` 과 `ChaserAuthoring` 을 **둘 다** 붙이면, 그 큐브는 돌면서 쫓아온다.
1·2단계 코드는 한 줄도 안 고친다. 상속 없이 **컴포넌트를 붙이는 것만으로 행동이 합쳐진다.**

### 3.2 과제

**목표**: WASD 로 움직이는 플레이어 큐브 1개를, 스포너가 만든 추격자 큐브 N개가 쫓아온다.

**작성할 파일** (`DOTS_Study/Scripts/Step03/`, 네임스페이스 `...Scripts.Step03`)

| 파일 | 내용 |
|---|---|
| `PlayerTag.cs` | 태그 |
| `ChaserTag.cs` | 태그 |
| `MoveSpeed.cs` | `IComponentData`. 초당 이동 거리 `float` 하나. **플레이어와 추격자가 같이 쓴다** |
| `StudyPlayerAuthoring.cs` | `PlayerTag` + `MoveSpeed` 를 붙인다 |
| `ChaserAuthoring.cs` | `ChaserTag` + `MoveSpeed` 를 붙인다 |
| `StudyPlayerMoveSystem.cs` | `SystemBase`. WASD 를 읽어 플레이어를 XY 평면에서 움직인다. 대각선이 더 빠르면 안 된다 |
| `ChaseSystem.cs` | `ISystem` + Burst. 플레이어 이동 **뒤에** 돈다. 추격자를 플레이어 쪽으로 움직인다 |

**조건**
- 1·2단계 파일은 **수정하지 않는다.** 스포너를 그대로 재사용한다
- 아직 Job 은 쓰지 않는다 (4단계에서 `ChaseSystem` 을 Job 으로 바꾼다)
- Z 는 건드리지 않는다 (2D)
- 이름이 `Study~` 인 이유: 게임 쪽에 `PlayerAuthoring`, `PlayerMoveSystem` 이 이미 있어서 컴포넌트 추가 메뉴에서 헷갈린다

**API 힌트**
- `SystemAPI.GetSingletonEntity<T>()`, `SystemAPI.GetComponent<T>(entity)`
- `.WithAll<T>()`
- `[UpdateAfter(typeof(X))]`
- `Keyboard.current.wKey.isPressed` (`using UnityEngine.InputSystem;`)
- `math.normalizesafe(v)` — 길이 1로. 길이가 0 이면 0 을 돌려준다
- `transform.ValueRW.Position` — 필드 하나만 바꿀 수 있다 (`ValueRW` 는 복사본이 아니라 참조다)

**씬 작업**
1. `StudyCube` 프리팹을 복제해 `StudyChaser.prefab` 을 만들고 `ChaserAuthoring` 을 **추가로** 붙인다 (`RotationSpeedAuthoring` 은 그대로 둔다 — 스포너가 `RotationSpeed` 를 쓰기 때문)
2. `Step03.unity` + SubScene `Step03_Sub.unity`
3. SubScene 안에 `Spawner` (프리팹 = `StudyChaser`, Count 100) 와 `Player` 큐브 (`StudyPlayerAuthoring`, 색이나 크기를 다르게) 를 둔다
4. 플레이어 속도 8, 추격자 속도 3 정도로 시작
5. Play → WASD 로 도망다녀 본다. 확인되면 Count 10,000

**확인 질문**
1. `ChaserTag` 는 0바이트다. 추격자의 **Chunk Capacity** 는 2단계(64)와 같은가, 다른가? 예상한 뒤 Archetypes 검색으로 확인. `MoveSpeed` 는?
2. 플레이어도 `MoveSpeed` 를 가진다. `ChaseSystem` 에서 `.WithAll<ChaserTag>()` 를 빼면 무슨 일이 생기는가? (예상한 뒤 해 보기)
3. `[UpdateAfter]` 를 빼고 Window → Entities → Systems 에서 두 시스템의 순서를 본다. 어떻게 되어 있는가? 순서가 뒤집히면 무엇이 달라지는가?
4. 플레이어를 가만히 두면 추격자가 플레이어 위치에 도달한다. 그때 `math.normalize` (safe 가 아닌 것) 를 쓰면 무슨 일이 생기는가? (해 보기 — 큐브가 어떻게 되는지 관찰)
5. 왜 플레이어 이동만 `SystemBase` 이고 추격은 `ISystem` 인가? 전부 `SystemBase` 로 하면 무엇을 잃는가?
6. 추격자들이 결국 **한 점에 겹친다.** 왜 그런가? 안 겹치게 하려면 각 추격자가 무엇을 알아야 하는가? (답만 생각해 보기. 구현은 7단계)

### 3.3 결과 / 배운 것

**완료: 2026-10-07.** 추격자 100개, 10,000개 모두 일정한 속도로 **돌면서** 플레이어를 쫓아오는 것을 확인했다. 에디터 로그 예외 0건.
(10-02 에 과제를 받고 5일 쉬었다가 10-07 하루에 작성했다.)

**작성 경위** — 컴포넌트 3개와 Authoring 2개는 혼자 작성하고 리뷰로 고쳤다. `StudyPlayerMoveSystem` 은 한 번 막혀서 "MonoBehaviour 로 쓰던 이동 코드를 옮긴다" 는 절차 안내를 받고 작성했다. `ChaseSystem` 은 `[UpdateAfter(typeof(...))]` 문법 예시를 받았고 나머지는 직접 썼다.

**틀렸다가 고친 것**

| 처음 쓴 것 | 왜 틀렸나 | 고친 것 |
|---|---|---|
| 세 컴포넌트 파일에 `using Unity.Entities;` 없음 | `IComponentData` 를 못 찾아 CS0246. **에러 하나가 전체 재컴파일을 막아 1·2단계 씬도 Play 가 안 된다** | `using` 추가 |
| `new MoveSpeed(){ MoveSpeed = ... }` | 필드 이름은 `Speed` 다. 타입 이름과 필드 이름을 헷갈렸다 | `Speed = ...` |
| `Entity player = GetSingletonEntity<PlayerTag>()` 를 플레이어 이동 시스템에 씀 | 받아 놓고 안 쓴다. 쿼리가 플레이어를 직접 찾는다. 이 API 는 **남의** 데이터를 읽을 때 쓴다 | 삭제 |
| A 키 `(0, -1)`, D 키 `(0, 1)` | W·S 와 같은 벡터. `dir.x` 가 항상 0 이라 좌우로 못 가고 A 는 아래, D 는 위로 간다. **타입이 맞아 컴파일러가 못 잡는다** (2단계 `_seed` 오타와 같은 종류) | `(-1, 0)`, `(1, 0)` |
| `[UpdateAfter(StudyPlayerMoveSystem)]` 를 `OnUpdate` 메서드 위에 | ① 순서는 **시스템 타입**의 속성이라 구조체에 붙인다 ② 어트리뷰트에는 `typeof(...)` 로 넘긴다 | 구조체 위 + `typeof` |
| `OnUpdate` 에 `[BurstCompile]` 없음 | 구조체에만 붙이면 그 메서드는 Burst 로 컴파일되지 않는다 | 세 군데 모두 |
| `Position.x = (target.x - pos.x) * speed * dt` | `=` 는 옮기기가 아니라 **덮어쓰기**. 추격자 전부가 첫 프레임에 "플레이어 위치의 약 5%" 지점 한 곳으로 순간이동한다 | `+=` |
| 방향을 normalize 하지 않음 | `(플레이어 − 나)` 에는 **거리**가 섞여 있다. 먼 추격자는 빠르고 가까운 추격자는 느려져 끝내 도달하지 못한다. `MoveSpeed` 가 "초당 이동 거리" 가 아니게 된다 | `dir.z = 0` → `normalizesafe` → 이동. **Z 를 지우는 것이 normalize 보다 먼저**여야 XY 속도가 줄지 않는다 |
| (씬) 스포너의 Min/Max 회전 속도를 0 으로 둠 | 프리팹의 `RotationSpeed` 30 을 `SpawnSystem` 의 `SetComponentData` 가 0 으로 덮어썼다. `RotationSystem` 은 돌고 있었지만 매 프레임 0도를 적용했다. **코드는 멀쩡하고 데이터가 틀린** 경우 | 인스펙터에서 값 입력 |

**확인 질문 답**

1. **Chunk Capacity 64 로 2단계와 같다** (실측. 아키타입 2개 = 추격자 + 프리팹 엔티티, 둘 다 64).
   - 예상은 "같다" 였고 맞았다. 다만 처음 답에는 `MoveSpeed` 에 대한 언급이 없었다.
   - `ChaserTag` 는 0바이트라 용량에 영향이 없다. `MoveSpeed` 는 엔티티마다 4바이트를 더 쓰지만, 약 250바이트짜리 엔티티에서 64개 경계를 넘기지 못했다.
2. `.WithAll<ChaserTag>()` (와 `.WithNone<PlayerTag>()`) 를 빼면 **플레이어도 루프에 들어오지만 화면에서는 아무것도 달라지지 않는다** (실측).
   - 플레이어의 `dir` = 플레이어 위치 − 자기 위치 = 0 → `normalizesafe` 가 0 을 돌려줌 → 이동량 0.
   - 처음 답 "모든 엔티티에게 실행된다" 는 틀렸다. 쿼리는 여전히 `LocalTransform` + `MoveSpeed` 를 **둘 다 가진** 엔티티만 고른다. 스포너(Transform 없음)와 프리팹 엔티티(`Prefab` 태그)는 들어오지 않는다.
   - **티가 안 나는 버그**라는 점이 중요하다. 4번과 합쳐지면 터진다.
3. 추격이 플레이어 이동보다 먼저 돌면 **이전 프레임의 플레이어 위치**를 쫓는다. ✅
   - 순서는 지정하지 않으면 보장되지 않는다. (`[UpdateAfter]` 를 뺐을 때 Systems 창에서의 실제 순서는 기록하지 못했다)
4. `math.normalize` 는 길이 0 인 벡터를 0 으로 나눠 **NaN** 을 만든다. Play 하자마자 **모든 큐브가 화면에서 사라졌다.** Entities Hierarchy 에는 그대로 있다 (실측).
   - 처음 답 "조금씩 이동할 것, safe 가 아니라 완전한 1 이 아니어서" 는 틀렸다. `safe` 는 정밀도와 무관하다. 길이가 0 이 아니면 두 함수의 결과는 같고, **길이 0 일 때만** 갈린다.
   - 전부가 한꺼번에 사라진 이유 (2번 실험 상태에서 했기 때문): 플레이어가 루프에 들어와 `dir` = 0 → 플레이어 위치가 NaN → 다음 프레임 `targetPosition` 이 NaN → 추격자 1만 개의 `dir` 이 전부 NaN. **NaN 하나가 한 프레임 만에 전체로 번졌다.**
   - NaN 은 어떤 수를 더해도 NaN 이라 되돌아오지 않는다. 예외도 나지 않는다. 엔티티는 살아 있고 데이터만 망가진다.
5. 플레이어 이동만 `SystemBase` 인 이유는 `Keyboard.current` 가 **매니지드 클래스**라 Burst 로 읽을 수 없어서다. 전부 `SystemBase` 로 하면 추격 루프(1만 개)의 Burst 를 잃는다.
   - 처음 답 "플레이어 처리가 먼저 이루어져야 해서" 는 틀렸다. 순서는 `[UpdateAfter]` 가 정하고 시스템 종류와 무관하다. 두 개념(실행 순서 / Burst 가능 여부)을 섞었다.
6. 모든 추격자가 **같은 한 점**을 목표로 하고 **서로의 위치를 모르기** 때문에 겹친다. ✅
   안 겹치려면 각자 주변 추격자의 위치를 알아야 한다. 1만 개가 서로를 전부 보면 1억 번이라 "가까운 것만 찾는 방법" 이 필요하다 → 7단계 공간 해시.

**도구에서 겪은 것** — Step03 을 쓰는 내내 VS Code 자동완성과 빨간 줄이 안 나왔다. 원인은 파일이나 csproj 가 아니라 **C# 언어 서버가 떠 있지 않은 것**이었다 (VS Code 재시작 뒤 C# Dev Kit 이 솔루션을 열지 않음. 프로세스 목록에 Roslyn 서버 없음). **Developer: Reload Window** 로 해결. 자동완성이 한꺼번에 안 되면 코드보다 언어 서버를 먼저 의심한다.

**프로젝트 코드와 비교** — [EnemyChaseSystem.cs](../Assets/MyAssets/Scripts/Runtime/Enemy/EnemyChaseSystem.cs), [PlayerMoveSystem.cs](../Assets/MyAssets/Scripts/Runtime/Player/PlayerMoveSystem.cs)

| | 내 코드 | 프로젝트 |
|---|---|---|
| 뼈대 | `[UpdateAfter(플레이어 이동)]` → 플레이어 위치를 한 번 읽음 → 추격자 루프 | **같다** |
| 플레이어 위치 읽기 | `GetSingletonEntity` + `GetComponent<LocalTransform>` | `GetSingleton<PlayerPosition>()` — 위치의 **사본** 컴포넌트를 따로 둔다. 이유는 4단계에서 직접 겪는다 |
| 루프 | 메인 스레드 `foreach` | `IJobEntity` + `ScheduleParallel` (4단계) |
| 0 으로 나누기 방지 | `normalizesafe` | `distance > 1e-3f` 일 때만 나눈다. 같은 목적 |
| 도착 직전 | 한 걸음이 남은 거리보다 커서 플레이어를 지나쳤다 돌아오며 떤다 | `maxStep > distance` 면 남은 거리만큼만 간다 |
| 겹침 | 한 점에 겹친다 | 공간 해시로 이웃을 찾아 서로 민다 (7단계) |
| 필터 | `.WithAll<ChaserTag>()` | `[WithAll(typeof(Active))]` — 풀에서 꺼진 적은 건너뛴다 (5단계) |
| 입력 | 이동 시스템이 키보드를 직접 읽는다 → `SystemBase` | 입력 시스템이 `PlayerInputState` 싱글턴에 써 두고, 이동 시스템은 그걸 읽는다 → 이동은 `ISystem` + Burst (6단계) |
| 플레이어 쿼리 | `.WithAll<PlayerTag>()` | 태그 없이 `PlayerMovement` 컴포넌트 유무로 고른다. 플레이어만 가진 데이터가 있으면 태그가 필요 없다 |

---

## 4단계 — Job 과 Burst

### 4.1 개념

**지금 `ChaseSystem` 은 어디서 도는가**
`OnUpdate` 안의 `foreach` 는 **메인 스레드 하나**에서 돈다. CPU 코어가 8개여도 1개만 쓴다.
Burst 는 그 한 코어에서 도는 코드를 빠르게 만들 뿐이다. **Burst(빠른 코드)와 Job(여러 코어)은 별개다.**

| | 코드가 빠른가 | 코어를 여러 개 쓰는가 |
|---|---|---|
| `SystemBase` 의 `foreach` | ❌ | ❌ |
| `ISystem` + `[BurstCompile]` 의 `foreach` (3단계) | ✅ | ❌ |
| `[BurstCompile]` Job + `ScheduleParallel` | ✅ | ✅ |

**Job**
"이 일을 워커 스레드에서 해 달라" 고 맡기는 **struct**. 필드에 필요한 데이터를 담고, `Execute` 에 할 일을 쓴다.

**`IJobEntity`**
쿼리에 맞는 엔티티마다 `Execute` 가 한 번씩 불리는 Job. **`Execute` 의 매개변수가 곧 쿼리다.**

```csharp
[BurstCompile]
public partial struct SpinJob : IJobEntity
{
    public float DeltaTime;                                   // 시스템이 채워서 넘긴다

    private void Execute(ref LocalTransform transform, in RotationSpeed speed)
    {
        transform = transform.RotateZ(speed.RadiansPerSecond * DeltaTime);
    }
}
```

| `foreach` 에서 | Job 에서 |
|---|---|
| `RefRW<LocalTransform>` | `ref LocalTransform` |
| `RefRO<RotationSpeed>` | `in RotationSpeed` |
| `.WithAll<T>()` | 구조체 위에 `[WithAll(typeof(T))]` |
| `transform.ValueRW.Position` | `transform.Position` (`Ref` 포장이 없다) |

**Job 은 `SystemAPI` 를 못 부른다**
Job 은 시스템 밖 다른 스레드에서 돈다. `SystemAPI.Time.DeltaTime` 이나 `GetSingletonEntity` 를 쓸 수 없다.
→ 필요한 값은 시스템이 **메인 스레드에서 미리 읽어 필드에 담아** 넘긴다. struct 복사다.

**맡기는 세 가지 방법**

```csharp
new SpinJob { DeltaTime = dt }.Run();               // 메인 스레드에서 지금 바로. foreach 와 같다
new SpinJob { DeltaTime = dt }.Schedule();          // 워커 스레드 1개에 맡긴다
new SpinJob { DeltaTime = dt }.ScheduleParallel();  // 청크를 워커 여러 개에 나눠 맡긴다
```

`Schedule` 과 `ScheduleParallel` 은 **예약만 하고 바로 돌아온다.** `OnUpdate` 가 끝난 시점에 일은 아직 안 끝났을 수 있다.

**왜 청크 단위로 나누는가**
`ScheduleParallel` 은 엔티티가 아니라 **청크**를 워커에 나눠 준다. 1만 개 = 청크 157개 → 워커들이 나눠 가진다.
각 워커가 서로 다른 청크(서로 다른 메모리)를 만지므로 충돌하지 않는다. 1단계의 청크 구조가 여기서 쓰인다.

**안전 시스템 — `ref` 와 `in` 이 중요한 이유**
두 스레드가 같은 데이터에 동시에 쓰면 값이 깨진다(데이터 레이스). ECS 는 Job 마다 **어떤 컴포넌트를 읽고 쓰는지**를 보고 판단한다.

- 둘 다 **읽기만** 한다 → 동시에 돌려도 된다
- 하나라도 **쓴다** → 앞의 Job 이 끝난 뒤에 돌린다

이 판단 재료가 `ref`(쓴다) / `in`(읽는다) 이다. 읽기만 하면서 `ref` 로 받으면 ECS 가 "쓴다" 고 보고 불필요하게 줄을 세운다.
→ **1단계 확인 질문 4번("왜 `RefRO` 로 받는가")의 답이 이것이다.**

**의존성 (`state.Dependency`)**
시스템마다 "내가 예약한 Job" 의 핸들이 `state.Dependency` 에 들어 있다. ECS 가 이걸 보고 Job 들의 순서를 엮는다.
`IJobEntity` 를 인자 없이 `Schedule()` / `ScheduleParallel()` 하면 이 연결을 **자동으로** 해 준다. 지금은 이것만 알면 된다.

**메인 스레드에서 읽으면 기다린다**
Job 이 `LocalTransform` 에 쓰는 중인데 메인 스레드가 `LocalTransform` 을 읽으려 하면, 그 Job 이 **끝날 때까지 메인 스레드가 멈춰 기다린다** (동기화 지점).
`SystemAPI.GetComponent`, `SystemAPI.Query` 의 `foreach` 가 이 기다림을 자동으로 건다.
→ 이번 과제에서 직접 보게 된다. 프로젝트의 ISSUE-006 과 `PlayerPosition` 이 여기서 나왔다.

**측정은 Profiler 로**
"Job 으로 바꿨으니 빨라졌다" 는 측정 전에는 말할 수 없다. 엔티티가 적으면 Job 을 예약하는 비용이 더 클 수도 있다.
Window → Analysis → Profiler, CPU Usage 모듈, 아래쪽을 **Timeline** 으로 두면 메인 스레드와 워커 스레드가 줄별로 보인다.

### 4.2 과제

**목표**: 추격 루프를 Job 으로 옮기고, **네 가지 방식의 시간을 Profiler 로 재서 표로 남긴다.**

**작성할 파일** (`DOTS_Study/Scripts/Step04/`, 네임스페이스 `...Scripts.Step04`)

| 파일 | 내용 |
|---|---|
| `JobChaserTag.cs` | 태그. 4단계용 추격자 표시 |
| `JobChaserAuthoring.cs` | `JobChaserTag` + `MoveSpeed`(3단계 것) 를 붙인다 |
| `ChaseJob.cs` | `IJobEntity`. 3단계 `ChaseSystem` 의 루프 본문을 `Execute` 로 옮긴다 |
| `JobChaseSystem.cs` | `ISystem` + Burst. 플레이어 위치와 `deltaTime` 을 읽어 Job 에 담고 예약한다 |

**태그를 새로 만드는 이유**
3단계 파일은 수정하지 않는다. 그런데 새 시스템이 `ChaserTag` 를 쓰면 3단계 `ChaseSystem` 과 **둘 다 돌아서 두 배로 움직인다.**
4단계 추격자에 `JobChaserTag` 만 붙이면, 3단계 시스템은 `RequireForUpdate<ChaserTag>` 에 걸려 저절로 꺼진다.
덤으로 Step03 씬(메인 스레드)과 Step04 씬(Job)이 둘 다 살아 있어 **언제든 다시 비교**할 수 있다.

**조건**
- 1·2·3단계 파일은 수정하지 않는다. `PlayerTag`, `MoveSpeed`, `StudyPlayerAuthoring`, `StudyPlayerMoveSystem`, 스포너는 그대로 재사용한다
- `ChaseJob` 안에 숫자를 박지 않는다. 필요한 값은 전부 필드로 받는다
- 측정 전에는 "빨라졌다" 고 적지 않는다

**API 힌트**
- `partial struct X : IJobEntity`, `private void Execute(ref A a, in B b)`
- `[WithAll(typeof(T))]` — Job 구조체 위에
- `new X { ... }.Run()` / `.Schedule()` / `.ScheduleParallel()`
- `using Assets.MyAssets.DOTS_Study.Scripts.Step03;` — 3단계 컴포넌트를 쓰려면

**씬 작업**
1. `StudyChaser` 를 복제해 `StudyJobChaser.prefab` 을 만든다. `ChaserAuthoring` 을 **떼고** `JobChaserAuthoring` 을 붙인다 (Move Speed 3)
2. `Step04.unity` + SubScene `Step04_Sub.unity`. 안에 `Player` 와 `Spawner` (프리팹 = `StudyJobChaser`, 회전 속도 Min/Max 를 0 이 아닌 값으로)
3. Play 해서 3단계와 똑같이 쫓아오는지 먼저 확인한다

**측정** — 이 단계의 본체

| # | 방식 | 어떻게 만드는가 |
|---|---|---|
| A | 메인 스레드, Burst 없음 | Step03 씬. `ChaseSystem.OnUpdate` 의 `[BurstCompile]` 한 줄을 **잠깐** 주석 처리 (재고 나서 되돌린다) |
| B | 메인 스레드, Burst | Step03 씬 그대로 |
| C | Job, 워커 1개 | Step04 씬. `.Schedule()` |
| D | Job, 워커 여러 개 | Step04 씬. `.ScheduleParallel()` |

- 네 번 모두 **같은 조건**: 같은 Count, 같은 해상도, 에디터 Play 모드, 플레이어는 가만히
- Count 는 A 에서 추격 시스템이 **1ms 이상** 나오는 값으로 정한다 (10,000 으로 안 보이면 50,000 → 100,000 으로 올린다). 정한 값을 기록한다
- 재는 값: Profiler Timeline 에서 추격 시스템(또는 `ChaseJob`)이 차지한 **ms**. 30프레임쯤 보고 대표값을 적는다
- C·D 에서는 **워커 스레드 줄**에 `ChaseJob` 이 몇 개로 쪼개져 보이는지도 본다
- 함께 기록: CPU 모델과 코어 수, Count

**확인 질문**
1. A → B 는 몇 배 빨라졌는가? B → D 는? 코어 수만큼 빨라졌는가? 아니라면 왜 그럴 것 같은가?
2. C(`Schedule`)와 B(메인 스레드 Burst)는 **한 스레드가 같은 일을 한다.** 시간이 같은가? 다르다면 메인 스레드는 그동안 무엇을 하고 있는가? (Timeline 에서 보기)
3. `ChaseJob.Execute` 의 `in MoveSpeed` 를 `ref MoveSpeed` 로 바꿔도 동작은 같다. 무엇을 잃는가? (4.1 의 안전 시스템 절. 1단계 질문 4번의 답)
4. D 에서 Timeline 의 메인 스레드를 본다. `ChaseJob` 을 예약한 **뒤에 도는** 시스템 중에 `LocalTransform` 을 메인 스레드에서 만지는 것이 있다. 무엇이고, 그 시스템 줄에 무엇이 보이는가? (힌트: 1단계에서 만든 것)
5. `JobChaseSystem` 은 매 프레임 `SystemAPI.GetComponent<LocalTransform>(player)` 로 플레이어 위치를 읽는다. 이 줄이 **지난 프레임에 예약한** `ChaseJob` 과 어떤 관계인가? 프로젝트가 `PlayerPosition` 이라는 사본을 따로 둔 이유를 추측해 보기
6. Count 를 100 으로 내리고 B 와 D 를 다시 잰다. 여전히 D 가 빠른가? 이 결과가 "항상 Job 으로 바꾸면 좋다" 에 대해 말해 주는 것은?
7. `ChaseJob` 에 `Random` 필드를 하나 두고 `Execute` 에서 `NextFloat` 을 부른다고 하자. `ScheduleParallel` 에서 무슨 문제가 생기는가? (답만 생각해 보기)

### 4.3 결과 / 배운 것

> 과제를 마치면 여기에 적는다. **측정 표를 반드시 포함한다** (방식 / ms / Count / CPU).
> 아래는 2026-10-07 에 코드까지 끝낸 시점의 중간 기록이다. 측정과 확인 질문이 끝나면 이 절을 완성한다.

**작성 경위 (코드, 2026-10-07)** — `JobChaserTag`, `JobChaserAuthoring`, `JobChaseSystem` 은 혼자 작성했고 고칠 곳이 없었다. `ChaseJob` 은 `Execute` 본문에서 한 번 막혀 "3단계 루프가 바깥에서 가져다 쓴 값을 세어 본다" 는 힌트와 `ValueRO`/`ValueRW` 대응표를 받고 작성했다.

**틀렸다가 고친 것 (코드)**

| 처음 쓴 것 | 왜 고쳤나 | 고친 것 |
|---|---|---|
| `Execute` 에서 플레이어 위치를 어디서 가져올지 몰라 본문을 비워 둠 | Job 은 `SystemAPI` 를 못 부른다. `DeltaTime` 과 똑같이 시스템이 읽어 **필드로 넘겨준다** | 필드 추가 |
| `public LocalTransform targetPosition;` | 동작은 맞다. 다만 위치·회전·스케일을 통째로 받으면서 `.Position` 만 쓴다. Job 의 필드는 "바깥에서 필요로 하는 것" 의 목록이라 **쓰는 것만** 받는다. 이름(Position)과 타입(Transform)도 어긋났고, 표기가 `DeltaTime` 과 달랐다 | `public float3 TargetPosition;` |
| `Execute` 메서드 위에 `[BurstCompile]` | `IJobEntity` 는 구조체 위의 `[BurstCompile]` 하나로 `Execute` 까지 컴파일된다. 시스템의 `OnCreate`/`OnUpdate` 와 규칙이 다르다 | 삭제 |

**확인한 것** — `.Run()` → `.Schedule()` → `.ScheduleParallel()` 순서로 바꿔 가며 세 방식 모두 3단계와 똑같이 쫓아오는 것을 확인했다. `.Run()` 으로 먼저 본 이유: Job 으로 옮기다 생긴 실수와 병렬화에서 생긴 문제를 분리하기 위해서다.

**남은 것** — 측정 A·B·C·D, 확인 질문 7개, 프로젝트 코드와 비교.
