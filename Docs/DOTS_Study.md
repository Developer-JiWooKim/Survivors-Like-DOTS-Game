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

## 커리큘럼

| 단계 | 개념 | 실습 | 프로젝트에서 쓰인 곳 | 상태 |
|---|---|---|---|---|
| 1 | Entity / Component / System, World, 아키타입·청크 | 큐브 회전 | 전체 구조의 기반 | ✅ 2026-10-01 |
| 2 | Baking 심화, 프리팹 엔티티, Instantiate | N개 스포너 | `EnemyAuthoring`, `EnemySpawnerAuthoring` | ⏳ 진행중 |
| 3 | 쿼리 (`SystemAPI.Query`, `RefRW`/`RefRO`), 싱글턴 읽기 | 플레이어 추격 | `EnemyChaseSystem` | ⬜ |
| 4 | Job·Burst (`IJobEntity`, `ScheduleParallel`, 의존성) | 3단계를 병렬로, Profiler 비교 | ISSUE-006 | ⬜ |
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
5. Play 중에 **Window → Entities → Archetypes** 를 열어 큐브 아키타입을 찾는다

**확인 질문**
1. Entities Hierarchy 에서 프리팹 엔티티를 찾아보자. 왜 그 엔티티는 회전하지 않는가?
2. `Spawner` 엔티티의 `TransformUsageFlags` 는 무엇으로 했는가? 왜 그런가?
3. `state.Enabled = false` 를 빼면 무슨 일이 생기는가? (예상한 뒤 **개수 100 으로** 직접 해 보기)
4. Archetypes 창에서 큐브 10,000 개가 **청크 몇 개**에 담겼는가? 청크 하나에 엔티티가 몇 개 들어가는가? 예상(16KB ÷ 엔티티 크기)과 맞는가?
5. 위치를 정할 때 `NextFloat2Direction() * NextFloat(0, 반경)` 으로 하면 큐브가 **가운데에 몰린다.** 왜 그런가? 고르게 퍼지게 하려면? (DOTS 가 아니라 수학 문제. 프로젝트의 `RingSampler.cs` 가 같은 문제를 푼다)

### 2.3 결과 / 배운 것

> 과제를 마치면 여기에 적는다.
