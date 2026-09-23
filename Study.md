# Study Log — Unity 6.6 ECS

Unity 6.0 ECS를 대략 알고 있는 상태에서, 6.6(Editor 6000.6.2f1, `com.unity.entities` 6.6.0)을 공부하며 이 프로젝트를 만들어가는 기록.

## 기록 방식
- 6.0 대비 **바뀐 점** 위주로 짧게 적는다 (API 이름 변경, 동작 방식 변경, deprecated/신규 기능 등).
- 새로 알게 된 개념도 짧게 적는다.
- 최신 항목이 위로 오도록 역순으로 추가한다.
- 형식: `### YYYY-MM-DD — 주제` + 2~5줄 요약. 상세 설명이 필요했던 경우에만 하위에 덧붙인다.

---

### 2026-09-23 — SystemAPI 소스 제너레이터가 기존 메서드를 바꾸는 방법 (6.0 대비 변경 없음)

Roslyn 소스 제너레이터는 파일 추가만 가능한데, `OnUpdate` 안의 `SystemAPI.Query`가 어떻게 바뀌나?

- 1단계 (Roslyn): partial struct 파일에 `OnUpdate`를 **다시 쓴 복사본** `__OnUpdate_XXXX`를 만들고 `[DOTSCompilerPatchedMethod("OnUpdate_...")]`를 붙임. `#line`으로 원본 파일 줄 번호를 매핑해서 에러와 디버깅은 원본 코드 기준으로 나옴.
- 2단계 (IL 후처리, `Unity.Entities.CodeGen`, Mono.Cecil): 컴파일된 DLL에서 이 attribute가 붙은 메서드의 IL 본문을 원래 `OnUpdate`로 복사함.
- 생성 파일 보는 법: `DOTS_OUTPUT_SOURCEGEN_FILES` define(Scripting Define Symbols 또는 `Assets/csc.rsp`의 `-define:`) → `Temp/GeneratedCode/<Assembly>/*.g.cs`.
- `SystemAPI.GetSingleton`은 `OnCreateForCompiler`에서 미리 만든 `EntityQuery`로, `SystemAPI.Time`은 `state.WorldUnmanaged.Time`으로 치환됨. Query도 매 프레임이 아니라 생성 시 한 번 빌드됨.

### 2026-09-23 — SystemAPI.Query의 RefRW/RefRO 내부 동작 (6.0 대비 변경 없음)

소스 제너레이터 코드(`Unity.Entities/SourceGenerators/Source~`)로 확인한 내용.

- 루프 앞에 `CompleteDependencyBeforeRW<T>`(읽는/쓰는 Job 모두 대기) 또는 `CompleteDependencyBeforeRO<T>`(쓰는 Job만 대기)를 자동으로 삽입함.
- `RefRW`로 요청하면 Chunk마다 `GetComponentDataWithTypeRW`가 호출돼 **`ValueRW`를 안 건드려도** 그 Chunk의 change version이 올라감 → 다른 System의 `WithChangeFilter`가 무력화됨. 읽기만 하면 반드시 `RefRO`를 쓸 것.
- 루프 변수는 내부적으로 `UncheckedRefRW`/`UncheckedRefRO`로 치환돼, 칸마다 하던 안전 검사를 Chunk 단위 한 번으로 줄임.
- 함정: `var t = x.ValueRW;`는 복사본이라 수정해도 원본에 반영되지 않음 (`ref var` 또는 직접 `x.ValueRW.Field = ...`로 써야 함).

### 2026-09-23 — [6.6 변경] Managed component 전면 deprecated

`com.unity.entities` 6.6 소스에 `First deprecated in 6.6.` 표시로 아래가 전부 `[Obsolete]` 처리됨 (아직 경고만, 이후 제거 예정).

- `class` 기반 `IComponentData` → `struct IComponentData`로 바꾸고, `UnityEngine.Object` 참조는 `UnityObjectRef<T>`로.
- `EntityManager.AddComponentObject / GetComponentObject / SetComponentObject`, `SystemAPI.ManagedAPI.*`, managed `ISharedComponentData`(`...Managed` 접미사 API)
- `GetTransformAccessArray` (managed `UnityEngine.Transform` 접근)
- Companion Component(`SpriteRenderer`, `Light` 등)는 **이 deprecation의 대상이 아님**. 6.6부터 Entity가 companion을 unmanaged `CompanionComponent<T>`(`UnityObjectRef<T>` 래퍼)로 참조하도록 Unity가 내부 이관함. 내부 구현엔 아직 `AddComponentObject`가 남아 있지만 패키지가 알아서 처리하는 부분이라 사용자는 조치할 게 없음.
  - 시스템에서 companion을 읽을 때만 바뀜: `GetComponentObject<T>` → `EntityManager.GetCompanion<T>`, `SystemAPI.ManagedAPI.UnityEngineComponent<T>` → `SystemAPI.Query<RefRO<CompanionComponent<T>>>()` 후 `.CompanionRef`, `WithAll<Light>()` → `WithAll<CompanionComponent<Light>>()`.

### 2026-09-23 — TransformUsageFlags는 Baker 간 OR로 합쳐진다 (6.0 대비 변경 없음)

`PlayerBaker`에서 `GetEntity(authoring.bulletPrefab, TransformUsageFlags.None)`로 구웠는데도 Bullet Entity에 `LocalTransform`/`LocalToWorld`가 붙음.

- 원인: Bullet 프리팹에 `SpriteRenderer`가 있고, Entities Graphics의 `SpriteRendererCompanionBaker`(`EntitiesGraphicsConversion.cs`)가 같은 GameObject에 대해 `GetEntity(TransformUsageFlags.Dynamic)`을 요청함. 한 GameObject에 대한 모든 Baker의 요청이 OR로 합쳐지므로 `None | Dynamic = Dynamic`.
- `None`은 "나는 Transform이 필요 없다"는 의미일 뿐, 다른 Baker의 요청을 **막지 않는다**. 정말로 Transform을 빼야 한다면 `ManualOverride`를 써야 함.
- API 자체는 6.0과 동일. `com.unity.entities`가 6.4부터 core package가 되어 이후 변경사항은 패키지 CHANGELOG가 아니라 Unity "What's new" 문서에 기록됨.

### 2026-09-23 — SystemBase에서 Input System Enable() 타이밍 (버전 무관, ECS 시스템 생명주기 이슈)

`OnCreate()`에서 `new Controls(); _controls.Enable();`을 호출했는데, `InputComponent`(싱글톤)에 값이 항상 0으로만 들어오는 문제.

- 원인: `Debug.Log`로 `OnCreate()`가 정상 호출되고 `Enable()`도 예외 없이 실행됐지만, 그 이후 어느 시점에 `_controls.asset.enabled`가 다시 `False`가 되어 있었음 (Unity Editor MCP로 `World.DefaultGameObjectInjectionWorld`에서 실제 필드를 reflection으로 찍어서 확인). `InputAction.ReadValue()`는 액션이 disable 상태면 예외 없이 조용히 기본값(0,0)만 반환하기 때문에 겉보기엔 "컴포넌트는 등록됐는데 값만 안 들어오는" 것처럼 보임.
- 이건 6.0→6.6 API 변경이 아니라 **일반적인 ECS 시스템 생명주기 vs Input System 초기화 타이밍 문제**: `OnCreate()`는 월드 부트스트랩 시점에 아주 일찍 실행되는데, Input System 쪽 디바이스/이벤트 초기화와의 순서가 보장되지 않아 `OnCreate()`에서 건 `Enable()`이 이후에 풀리는 경우가 있었다.
- 해결: `Enable()`/`Disable()` 호출을 `OnCreate`/`OnDestroy` 대신 `OnStartRunning`/`OnStopRunning`으로 옮기고, `OnDestroy`에서는 `_controls.Dispose()`만 호출 (생성된 `Controls.cs` 상단 예제 주석의 MonoBehaviour 패턴과 동일한 대응: Awake→OnCreate, OnEnable→OnStartRunning, OnDisable→OnStopRunning, OnDestroy→OnDestroy).
- 검증: Unity Editor MCP eval로 `Keyboard`에 `KeyboardState(Key.W)`를 직접 큐잉해서 W키를 시뮬레이션하고, 수정 전/후 싱글톤의 `Movement` 값을 비교해서 수정 후에만 `(0, 1)`로 정상 반영되는 것을 확인함.
