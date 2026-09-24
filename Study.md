# Study Log — Unity 6.6 ECS

Unity 6.0 ECS를 대략 알고 있는 상태에서, 6.6(Editor 6000.6.2f1, `com.unity.entities` 6.6.0)을 공부하며 이 프로젝트를 만들어가는 기록.

## 기록 방식
- 6.0 대비 **바뀐 점** 위주로 짧게 적는다 (API 이름 변경, 동작 방식 변경, deprecated/신규 기능 등).
- 새로 알게 된 개념도 짧게 적는다.
- 최신 항목이 위로 오도록 역순으로 추가한다.
- 형식: `### YYYY-MM-DD — 주제` + 2~5줄 요약. 상세 설명이 필요했던 경우에만 하위에 덧붙인다.

---

### 2026-09-24 — `World.DefaultGameObjectInjectionWorld` (6.0 대비 변경 없음)

- `World`의 `public static World { get; set; }` 프로퍼티다. `DefaultWorldInitialization.Initialize`가 기본 World("Default World")를 만들 때 값을 넣는다. 시스템 **밖**의 코드(MonoBehaviour, Editor 도구)가 World에 접근할 때 쓰는 진입점이다. 시스템 안에서는 `state.World`/`World`를 쓴다.
- null인 경우: 기본 World가 만들어지기 전, Dispose된 후, `ICustomBootstrap`이 false를 반환하거나 `UNITY_DISABLE_AUTOMATIC_SYSTEM_BOOTSTRAP`을 쓸 때. 그래서 `null`과 `IsCreated`를 먼저 확인한다.
- setter가 public이라 다른 World를 넣으면 이 값을 읽는 모든 코드(Entity Inspector 포함)가 그 World를 보게 된다.
- 이름은 예전(0.x) GameObject를 이 World로 "inject"하던 변환 방식에서 왔다(`World.Active`의 후속). 지금은 "기본 World"라는 의미만 남았다.

### 2026-09-24 — UI Toolkit: `UIDocument` 대신 `PanelRenderer` (6.6 신규)

- 6.6에는 `UIDocument`와 같은 역할을 하는 `PanelRenderer`가 새로 생겼다. `Renderer`를 상속하며 `panelSettings`와 `visualTreeAsset`을 가진다. `UIDocument`는 아직 `[Obsolete]`가 아니고, 둘 다 `IPanelComponent`를 구현한다.
- **`rootVisualElement`는 internal이라 쓸 수 없다.** 대신 `RegisterUIReloadCallback((renderer, root, version) => ...)`(`VersionedUIReloadCallback`)으로 root를 받는다. 이미 로드된 상태에서 등록하면 **즉시 한 번 호출된다**(직접 확인함). UXML이 live reload되면 새 root로 다시 호출되므로, `Q<Label>()`로 찾은 참조는 콜백 안에서 갱신한다.
- ⚠️ `(renderer, root)` 2인자 `UIReloadCallback` 오버로드는 **6.6에서 이미 `[Obsolete]`**다. `version`이 이전과 같으면 UI가 실제로 바뀌지 않은 것이므로 다시 찾는 작업을 건너뛴다. 메서드 그룹을 넘길 때 시그니처가 2인자면 obsolete 쪽에 바인딩되고 warning만 뜨므로 주의한다.
- `OnEnable`에서 Register, `OnDisable`에서 `UnregisterUIReloadCallback`을 호출한다(같은 versioned 오버로드).
- 반대 방향: `PanelRenderer.FindPanelRenderer(VisualElement)`로 element가 속한 PanelRenderer를 찾을 수 있다.

### 2026-09-24 — `VisualElementReference<T>`: element를 `Q()` 대신 Inspector에서 직렬화 (6.6 신규)

- `[SerializeField] VisualElementReference<Label> _ref;`를 선언하면 Inspector에 선택기(`VisualElementReferencePropertyDrawer`)가 뜬다. `PanelRenderer`의 UI에서 element를 골라 넣는다. 이 방식은 `PanelRenderer` 전용이고 `UIDocument`에서는 쓸 수 없다.
- 직렬화되는 값은 `m_PanelRenderer`와 `m_AuthoringPath.m_PathIds`(int 배열)다. 이름이 아니라 UXML의 **`authoring-id` 속성** 기반이라 `name`을 바꿔도 참조가 유지된다.
- 직접 작성한 UXML에는 `authoring-id`가 없다. 선택기로 고르면 에디터가 UXML 파일을 다시 써서 해당 element에 `authoring-id="..."`를 추가한다(`VisualElementReferenceTools.AddMissingAuthoringIds`). 경로는 계층 전체가 아니라 템플릿 중첩 단위라서, 같은 UXML 안의 element는 ID가 하나다.
- 런타임: `RegisterReferenceResolvedCallback(Action<T>)`으로 element를 받는다. 이미 resolve됐으면 즉시 호출된다. `RegisterReferenceUnloadedCallback`은 live reload 등으로 문서가 파괴될 때 호출되니, 여기서 캐시를 비운다. `OnEnable`에서 Register, `OnDisable`에서 Unregister한다.
- 호출 순서 실측(Play Mode, `visualTreeAsset` 교체 + `EditorApplication.Step()`):
  - Register 시점에 이미 resolve돼 있으면 → 그 자리에서 **동기**로 `Resolved` 호출.
  - 문서가 파괴되면 → **다음 프레임**에 `Unloaded(옛 element)` 호출. 인자는 이전에 받은 **같은 인스턴스**이고, 이미 panel에서 떨어져 있다(`panel == null`). 옛 element에 걸었던 이벤트를 해제하는 데 쓸 수 있다.
  - 다시 로드되면 → `Resolved(새 인스턴스)` 호출. **이 시점에는 아직 `panel == null`**이고 같은 프레임 안에서 붙는다. 그래서 `Resolved` 안에서 `resolvedStyle`, `worldBound`, `panel`에 의존하는 코드를 쓰면 안 된다(text 설정, 이벤트 등록은 괜찮다). 같은 프레임의 `UIReloadCallback`은 `Resolved` 다음에 오고, 그때는 root가 panel에 붙어 있다.
  - `enabled`를 껐다 켜는 것만으로는 문서가 다시 만들어지지 않는다(콜백 없음).
  - `UIReloadCallback`의 version은 `visualTreeAsset = null`처럼 빈 문서가 될 때도 증가한다.

### 2026-09-24 — ECS 데이터를 UI Toolkit에 표시하기 (6.0 대비 변경 없음)

- UI(`UIDocument`/`PanelRenderer`)는 GameObject이므로 SubScene이 아니라 일반 Scene에 둔다. MonoBehaviour가 `World.DefaultGameObjectInjectionWorld.EntityManager`로 만든 `EntityQuery`에서 `TryGetSingleton`으로 읽는 방식이 제일 단순하다.
- 점수 싱글톤은 bake할 데이터가 없어서 System `OnCreate`에서 `EntityManager.CreateSingleton<T>()`로 만든다.
- 죽은 적 수는 `WithAll<EnemyTag, DestroyEntityFlag>()` 쿼리의 `CalculateEntityCount()`로 한 번에 센다. enableable component의 enabled bit도 반영된다. 반대로 `EntityManager.CreateEntityQuery(typeof(DestroyEntityFlag))`로 만든 쿼리도 **꺼진 엔티티는 제외된다**. 모두 가져오려면 `EntityQueryOptions.IgnoreComponentEnabledState`를 쓴다.
- `Label.text`는 쓸 때마다 layout/repaint가 다시 일어나므로 값이 바뀌었을 때만 쓴다.

### 2026-09-24 — ECS에서 사운드 재생 + 6.6에서 managed component deprecated

- Entities에는 오디오 API가 없다(6.0과 동일). Burst 시스템은 싱글톤 `DynamicBuffer<PlaySoundRequest>`에 요청만 `Add`하고, `PresentationSystemGroup`의 `SystemBase`가 main thread에서 `AudioSource.PlayOneShot`으로 재생한 뒤 buffer를 `Clear`한다. SubScene의 GameObject는 bake 후 사라지므로 `AudioSource`는 런타임에 직접 만든다.
- **6.6 변경점:** class `IComponentData`(managed component), `IBaker.AddComponentObject`, `EntityManager.GetComponentObject`, `SystemAPI.ManagedAPI`가 모두 **deprecated**(CS0618, "First deprecated in 6.6")다. managed ISharedComponentData도 없어질 예정이다. 6.0에서는 AudioClip 같은 UnityEngine.Object를 managed component에 넣는 게 흔했다.
- 대신 unmanaged struct에 `UnityObjectRef<T>`를 넣는다. Baker에서는 `Clip = audioClip`처럼 암시적 변환으로 넣고, main thread에서 `.Value`로 실제 객체를 꺼낸다. 배열은 `IBufferElementData`로 바꾼다.
- 생산자 시스템은 `RequireForUpdate<PlaySoundRequest>` 대신 `SystemAPI.TryGetSingletonBuffer`를 쓴다. 사운드 싱글톤이 없어도 발사/폭발 로직은 계속 돌아야 하기 때문이다.

### 2026-09-24 — ECS에서 스프라이트 시트 이펙트 만들기 (MaterialProperty + DOTS Instancing 셰이더)

- `SpriteRenderer`는 Entities Graphics가 **companion GameObject**로 bake한다(`SpriteRendererCompanionBaker`). 엔티티마다 숨은 GameObject가 생기고, sprite 교체는 managed 코드라 Burst를 쓸 수 없다. 많이 생성되는 이펙트에는 맞지 않다.
- 순수 ECS 방식: Quad `MeshRenderer` + 커스텀 셰이더로 bake하면 BatchRendererGroup으로 그려진다. 프레임 번호는 `[MaterialProperty("_Frame")]`을 붙인 `IComponentData`에 넣는다. Burst Job에서 값만 바꾸면 Entities Graphics가 GPU로 올린다(MaterialPropertyBlock을 대신함).
- 셰이더를 **Shader Graph**로 만들 때: 엔티티별로 받을 프로퍼티(`_Frame`)는 Graph Inspector > Node Settings > **Override Property Declaration**을 켜고 `Hybrid Per Instance`로 둔다. 이렇게 해야 `UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)` 블록에 들어간다. 기본값(Per Material)으로 두면 `[MaterialProperty]` 컴포넌트 값이 **에러 없이 무시된다**.
- 2D Renderer + Entities Graphics에서 쓸 Shader Graph target은 **Universal > Unlit**이다. 2D Renderer는 `Universal2D`, `SRPDefaultUnlit` LightMode만 그린다. Sprite Unlit/Lit target은 `Universal2D` 패스는 있지만 DOTS.hlsl(DOTS_INSTANCING_ON)을 넣지 않아서 BRG로 그릴 수 없다. URP Unlit target은 LightMode가 없어서(SRPDefaultUnlit) 2D Renderer가 그리고, DOTS Instancing도 포함한다. 단, `Keep Lighting Variants`를 켜면 LightMode가 UniversalForward로 바뀌어 2D Renderer에서 안 보인다.
- 문서상 URP는 Forward+만 공식 지원한다. 2D Renderer는 위 조건에서 동작을 확인했다.
- 스폰 타이밍: 이펙트를 `EndSimulation` ECB로 만들면 이번 프레임 TransformSystemGroup이 이미 끝났으므로 첫 렌더링이 프리팹의 LocalToWorld(원점)로 나간다. `BeginSimulation` ECB로 만들어서 다음 프레임 Transform 계산 뒤에 그려지게 했다.

### 2026-09-24 — Unity Physics의 Kinematic body는 PhysicsVelocity로 움직인다 (PhysX와 다름)

- Kinematic Rigidbody를 bake하면 `PhysicsVelocity` + `PhysicsMass`(inverse mass/inertia = 0) + `PhysicsGravityFactor = 0`이 붙는다. solver가 속도를 적분해서 움직이고, 충돌해도 밀려나지 않는다.
- PhysX에서는 kinematic의 velocity가 무시되고 `MovePosition`을 써야 했지만, Unity Physics에서는 **kinematic에 velocity를 넣는 것이 정석**이다.
- Rigidbody가 없는 collider는 static body가 된다. static body를 `LocalTransform`으로 매 프레임 옮기면 static BVH가 매 step 다시 만들어진다.
- velocity로 움직이면 이동이 fixed step 안에서 일어나서 물리 판정과 타이밍이 맞는다. `LocalTransform`을 직접 수정하면 teleport로 처리된다.
- 회전 잠금: `PhysicsMass.InverseInertia = 0`으로 둔다. 각가속도 = I⁻¹·torque라서 inverse가 0이면 회전 관성이 무한대인 것과 같다. `InverseMass = 0`(kinematic)의 회전 버전이다. baking이 Rigidbody Freeze Rotation을 반영하지 않으므로 직접 설정한다.
- 단, kinematic끼리, kinematic과 static 사이에는 **충돌 응답이 없다**(trigger 이벤트는 발생). 벽에 막히거나 서로 밀어내야 하는 캐릭터는 dynamic + gravity off + velocity 구동으로 두고, 총알처럼 판정만 필요한 것만 kinematic으로 한다.

### 2026-09-24 — Unity Physics에서 Collider Layer Overrides가 적용되는 방식

`ColliderBakingSystem.ProduceCollisionFilter`와 `CollisionFilter.IsCollisionEnabled`로 확인한 내용.

- Baking 결과: `BelongsTo = 1 << layer`, `CollidesWith = (Layer Collision Matrix) | includeLayers(Collider+Rigidbody) & ~excludeLayers`. **`layerOverridePriority`는 Baking에서 무시됨.**
- 충돌 판정은 **양방향 AND**: `(A.BelongsTo & B.CollidesWith) != 0 && (B.BelongsTo & A.CollidesWith) != 0`. 그래서 한쪽(Bullet)에만 Include Layers를 넣고 상대(Enemy)의 matrix/override에 Bullet이 빠져 있으면 Trigger 이벤트가 생기지 않음. PhysX처럼 priority로 한쪽 설정이 이기는 방식이 아님.
- 해결: Layer Collision Matrix에서 두 layer를 체크하거나, **양쪽 Collider 모두**에 Include Layers를 설정함.

### 2026-09-23 — IJobEntity 내부 동작 (6.0 대비 변경 없음)

소스 제너레이터 코드(`JobEntityGenerator/`)로 확인한 내용.

- `partial struct MyJob : IJobEntity`는 소스 제너레이터가 **`IJobChunk`로 바꿔서** 구현함. 생성된 `Execute(in ArchetypeChunk chunk, ...)`가 chunk마다 component 배열 포인터를 한 번 얻은 뒤, `for` 루프로 사용자가 작성한 `Execute(ref A, in B)`를 entity마다 호출함.
- Query는 `Execute` 파라미터(`ref`는 RW, `in`은 RO)와 `[WithAll]`/`[WithNone]` 등으로 만들어지고, System의 `OnCreateForCompiler`에서 한 번만 생성됨. `job.ScheduleParallel(dep)` 호출은 System 쪽에서 `JobChunkExtensions.ScheduleParallelByRef(ref job, query, dep)`로 바뀜.
- 병렬 작업 단위는 **chunk**라서, 같은 chunk는 한 worker만 처리함 → 같은 component에 `ref`로 써도 안전함.
- Query에 enableable component가 있을 때만 enabled mask를 검사하는 코드가 생성됨(bit range 방식이나 64bit mask 방식). 없으면 단순한 `for` 루프만 생성됨.

### 2026-09-23 — ECS용 2D Physics 현황 (6.0 대비: Entities 통합은 여전히 없음, 대신 PhysicsCore2D가 새로 생김)

- `com.unity.physics`(Unity Physics)는 여전히 3D 전용. Entities와 통합된 공식 2D Physics 패키지(Baker, `PhysicsWorldSingleton` 같은 것)는 아직 없음. 예전 `com.unity.2d.entities`(Project Tiny, 0.22-preview)는 폐기된 패키지임.
- 새로 생긴 것: 6.3에 Box2D v3 기반 **LowLevelPhysics2D**가 추가됐고, 6.5에서 **PhysicsCore2D**로 이름이 바뀜 (namespace `UnityEngine.LowLevelPhysics2D` → `Unity.U2D.Physics`). 객체를 struct handle로 다루고 multithread를 지원해서 ISystem/Job 안에서 직접 호출할 수 있음.
- 그래서 선택지는: (a) 예전처럼 3D Unity Physics를 Z축과 회전을 고정해서 쓰기 (Baker와 Query가 다 있어서 제일 편함), (b) PhysicsCore2D world/body를 직접 만들고 Entity와 동기화하는 시스템을 직접 작성하기, (c) 단순한 top-down 충돌이면 물리 엔진 없이 직접 AABB나 원 판정하기.
- 주의: GameObject용 `Rigidbody2D`/`Collider2D`는 Baking되지 않음 (Baker가 없음).

### 2026-09-23 — 기본 ECB System 목록과 `CreateCommandBuffer(WorldUnmanaged)` 내부 (6.0 대비 변경 없음)

- 기본 ECB System은 9개: Begin/End × Initialization, FixedStepSimulation, VariableRateSimulation, Simulation + `BeginPresentation`. `EndPresentation`은 없음. 각 그룹에서 `OrderFirst`/`OrderLast`로 배치됨.
- `Singleton.CreateCommandBuffer(state.WorldUnmanaged)` 내부 동작 (`EntityCommandBufferSystem.cs`):
  - ECB System의 전용 allocator로 ECB를 만들고, 그 System의 `PendingBuffers` 목록에 등록함. playback이 끝나면 Dispose하고 allocator를 Rewind함 → 직접 `Dispose`하지 않아도 됨.
  - `world` 인자는 `world.ExecutingSystem`을 읽어 **어느 System이 만든 ECB인지**(`OriginSystemHandle`) 기록하는 데 쓰임 → playback 에러 메시지와 디버깅용.
- `WorldUnmanaged`: managed `World` 클래스의 unmanaged(struct) 버전. `ISystem`/Burst 안에서는 `World`를 쓸 수 없어서 대신 사용함.

### 2026-09-23 — `ValueRO`로 `LocalTransform.Up()` 호출 시 "impure method" 경고 (6.0 대비 변경 없음)

- `ValueRO`는 `ref readonly T`를 반환하고, `LocalTransform`은 `readonly struct`가 아니며 `Up()`/`Right()`에도 `readonly`가 붙어 있지 않음 → C# 컴파일러가 호출 전에 **방어적 복사(defensive copy)**를 하고, IDE(Rider 등)가 경고를 띄움.
- 실제 문제는 없음: `Up()`은 값을 바꾸지 않아서 결과가 같고, 복사도 32바이트뿐이라 Burst에서는 사실상 공짜.
- 경고를 없애려면: `LocalTransform t = localTrm.ValueRO;`로 한 번 명시적으로 복사한 뒤 `t.Up()`을 호출하거나, `math.mul(rot, math.up())`처럼 직접 계산함.
- `ValueRW`(`ref T`)에서는 복사가 없어서 경고도 뜨지 않음. 하지만 읽기만 하려고 `RefRW`를 요청하는 건 change version 때문에 손해임.

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
