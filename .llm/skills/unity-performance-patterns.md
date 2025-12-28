# Skill: Unity Performance Patterns

**Trigger**: When writing Unity-specific code, accessing Unity APIs, or working with MonoBehaviours, GameObjects, Physics, or other Unity systems. This skill complements [high-performance-csharp](high-performance-csharp.md) with Unity-specific patterns.

---

## Unity's Garbage Collector

Unity uses the **Boehm-Demers-Weiser (BDW)** garbage collector, which differs from .NET's generational GC:

| Aspect            | Unity (BDW)                    | .NET CLR                   |
| ----------------- | ------------------------------ | -------------------------- |
| Algorithm         | Conservative, non-generational | Generational (Gen 0, 1, 2) |
| Memory Compaction | **No**                         | Yes                        |
| Stop-the-world    | Yes (full heap scan)           | Yes (per-generation)       |
| Fragmentation     | Prone to fragmentation         | Reduced via compaction     |

**Critical Insight**: Unity's GC must process the **entire object graph** on every collection. This makes allocation avoidance even more important in Unity than in standard .NET applications.

### GC Spike Prevention

- **Target**: 0 bytes allocated per frame (or as close as possible)
- At 60 FPS with 1KB/frame = **3.6 MB/minute** of garbage
- Use loading screens to manually trigger GC: `System.GC.Collect()`

---

## Component & Reference Caching

### Cache Component References

`GetComponent<T>()` involves internal lookups and should **never** be called in `Update()`.

```csharp
// ❌ NEVER: Expensive lookup every frame
void Update()
{
    Rigidbody rb = GetComponent<Rigidbody>();
    rb.AddForce(Vector3.up);
}

// ✅ ALWAYS: Cache in Awake()
private Rigidbody _rigidbody;
private Transform _transform;
private Camera _mainCamera;

void Awake()
{
    _rigidbody = GetComponent<Rigidbody>();
    _transform = transform;  // Cache transform property too
    _mainCamera = Camera.main;
}

void Update()
{
    _rigidbody.AddForce(Vector3.up);
    _transform.position = _mainCamera.transform.position;
}
```

### Cache Expensive Properties

Many Unity properties perform work each access:

```csharp
// ❌ BAD: Camera.main performs FindGameObjectWithTag internally
void Update()
{
    Vector3 camPos = Camera.main.transform.position;  // Lookup + property access
}

// ✅ GOOD: Cached reference
private Camera _mainCamera;
void Awake() { _mainCamera = Camera.main; }
void Update()
{
    Vector3 camPos = _mainCamera.transform.position;
}
```

---

## Never Use SendMessage

`SendMessage()` and `BroadcastMessage()` are **up to 1000x slower** than direct function calls due to reflection-based method lookup:

```csharp
// ❌ NEVER: Extremely slow, no compile-time safety
gameObject.SendMessage("OnDamage", damage);
gameObject.BroadcastMessage("OnHit");

// ✅ ALWAYS: Direct interface calls
var damageable = gameObject.GetComponent<IDamageable>();
if (damageable != null)
{
    damageable.OnDamage(damage);
}

// ✅ Or use events/delegates
public event Action<float> OnDamage;
OnDamage?.Invoke(damage);
```

---

## Unity API Allocation Traps

### Array-Valued Properties Create Copies

Many Unity properties return **new array copies** on each access:

```csharp
// ❌ TERRIBLE: Creates 4 array copies per iteration!
void Update()
{
    for (int i = 0; i < mesh.vertices.Length; i++)
    {
        float x = mesh.vertices[i].x;  // New array!
        float y = mesh.vertices[i].y;  // New array!
        float z = mesh.vertices[i].z;  // New array!
        DoSomething(x, y, z);
    }
}

// ✅ BETTER: Cache array locally
void Update()
{
    Vector3[] vertices = mesh.vertices;  // Single allocation
    for (int i = 0; i < vertices.Length; i++)
    {
        DoSomething(vertices[i].x, vertices[i].y, vertices[i].z);
    }
}

// ✅ BEST: Use non-allocating API
private List<Vector3> _vertices = new List<Vector3>();

void Update()
{
    mesh.GetVertices(_vertices);  // No allocation!
    for (int i = 0; i < _vertices.Count; i++)
    {
        DoSomething(_vertices[i].x, _vertices[i].y, _vertices[i].z);
    }
}
```

### Non-Allocating Unity API Alternatives

| Allocating API               | Non-Allocating Alternative                             |
| ---------------------------- | ------------------------------------------------------ |
| `mesh.vertices`              | `mesh.GetVertices(list)`                               |
| `mesh.normals`               | `mesh.GetNormals(list)`                                |
| `mesh.uv`                    | `mesh.GetUVs(channel, list)`                           |
| `mesh.triangles`             | `mesh.GetTriangles(list, submesh)`                     |
| `Physics.RaycastAll`         | `Physics.RaycastNonAlloc`                              |
| `Physics.OverlapSphere`      | `Physics.OverlapSphereNonAlloc`                        |
| `Physics.OverlapBox`         | `Physics.OverlapBoxNonAlloc`                           |
| `Physics2D.OverlapCircleAll` | `Physics2D.OverlapCircleNonAlloc`                      |
| `Input.touches`              | `Input.touchCount` + `Input.GetTouch(i)`               |
| `Animator.parameters`        | `Animator.parameterCount` + `Animator.GetParameter(i)` |
| `Renderer.sharedMaterials`   | `Renderer.GetSharedMaterials(list)`                    |
| `gameObject.tag`             | `gameObject.CompareTag("Tag")`                         |
| `gameObject.name`            | Cache in Awake if needed repeatedly                    |

### Physics Non-Alloc Pattern

```csharp
// ❌ BAD: Allocates new array every call
void FindTargets()
{
    Collider[] hits = Physics.OverlapSphere(transform.position, radius);
    foreach (Collider hit in hits)
    {
        ProcessTarget(hit);
    }
}

// ✅ GOOD: Pre-allocated buffer
private readonly Collider[] _hitBuffer = new Collider[32];

void FindTargets()
{
    int count = Physics.OverlapSphereNonAlloc(
        transform.position, radius, _hitBuffer);

    for (int i = 0; i < count; i++)
    {
        ProcessTarget(_hitBuffer[i]);
    }
}
```

---

## Tag & Layer Comparisons

### Avoid String Allocation

```csharp
// ❌ BAD: .tag allocates a new string
if (gameObject.tag == "Player") { }

// ❌ BAD: .name also allocates
if (gameObject.name == "Enemy") { }

// ✅ GOOD: CompareTag is allocation-free
if (gameObject.CompareTag("Player")) { }

// ✅ GOOD: Cache name if needed repeatedly
private string _cachedName;
void Awake() { _cachedName = gameObject.name; }
```

---

## Update Methods

### Remove Empty Callbacks

Even empty Unity callbacks have overhead due to managed/unmanaged code boundary crossing:

```csharp
// ❌ BAD: Remove these if not used
void Update() { }
void FixedUpdate() { }
void LateUpdate() { }
void OnGUI() { }
```

### Update Method Selection

| Method          | When Called                    | Use For                                    |
| --------------- | ------------------------------ | ------------------------------------------ |
| `Update()`      | Every frame (variable rate)    | Input, game logic, non-physics movement    |
| `FixedUpdate()` | Fixed timestep (default 50 Hz) | Physics operations, Rigidbody manipulation |
| `LateUpdate()`  | After all Update() calls       | Camera follow, post-processing positions   |

### Anti-Patterns

- **Physics in Update()**: Inconsistent behavior at different framerates
- **Input in FixedUpdate()**: May miss input events between fixed steps
- **Heavy logic every frame**: Consider spreading work across frames

---

## Centralized Update Manager Pattern

When you have many MonoBehaviours with `Update()`, the managed/native code boundary crossing adds significant overhead. Use a centralized manager instead:

```csharp
// ❌ BAD: 1000 MonoBehaviours each with Update = significant overhead
public class Enemy : MonoBehaviour
{
    void Update()
    {
        UpdateAI();
    }
}

// ✅ GOOD: Single Update call manages all entities
public class EnemyManager : MonoBehaviour
{
    private readonly List<Enemy> _enemies = new List<Enemy>(256);

    public void Register(Enemy enemy) => _enemies.Add(enemy);
    public void Unregister(Enemy enemy) => _enemies.Remove(enemy);

    void Update()
    {
        // Single native->managed boundary crossing
        for (int i = 0; i < _enemies.Count; i++)
        {
            _enemies[i].UpdateAI();
        }
    }
}

// Enemy becomes:
public class Enemy : MonoBehaviour
{
    void OnEnable() => EnemyManager.Instance.Register(this);
    void OnDisable() => EnemyManager.Instance.Unregister(this);

    public void UpdateAI()
    {
        // AI logic here
    }
}
```

**Benefits:**

- Reduces managed/native boundary crossings from N to 1
- Easier to profile (single entry point)
- Can implement prioritization, spatial partitioning, etc.

---

## Coroutine Optimization

### Cache WaitForSeconds

```csharp
// ❌ BAD: Allocates every iteration
IEnumerator BadCoroutine()
{
    while (true)
    {
        yield return new WaitForSeconds(1f);  // Allocation!
        DoWork();
    }
}

// ✅ GOOD: Cache and reuse
private readonly WaitForSeconds _waitOneSecond = new WaitForSeconds(1f);
private readonly WaitForEndOfFrame _waitEndOfFrame = new WaitForEndOfFrame();
private readonly WaitForFixedUpdate _waitFixedUpdate = new WaitForFixedUpdate();

IEnumerator GoodCoroutine()
{
    while (true)
    {
        yield return _waitOneSecond;  // No allocation!
        DoWork();
    }
}
```

### Yield Null Is Free

```csharp
// ✅ yield return null has no allocation
IEnumerator FrameByFrameCoroutine()
{
    while (condition)
    {
        yield return null;  // Free!
        ProcessNextStep();
    }
}
```

---

## GameObject Pooling

### Unity's ObjectPool (Unity 2021+)

```csharp
using UnityEngine.Pool;

public class BulletManager : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;

    private ObjectPool<GameObject> _bulletPool;

    void Awake()
    {
        _bulletPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(_bulletPrefab),
            actionOnGet: bullet => bullet.SetActive(true),
            actionOnRelease: bullet => bullet.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet),
            defaultCapacity: 50,
            maxSize: 200
        );
    }

    public GameObject SpawnBullet(Vector3 position)
    {
        GameObject bullet = _bulletPool.Get();
        bullet.transform.position = position;
        return bullet;
    }

    public void ReturnBullet(GameObject bullet)
    {
        _bulletPool.Release(bullet);
    }
}
```

### Custom Pool Pattern

```csharp
public class SimpleGameObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _initialSize = 20;

    private readonly Stack<GameObject> _pool = new Stack<GameObject>();

    void Awake()
    {
        for (int i = 0; i < _initialSize; i++)
        {
            GameObject obj = Instantiate(_prefab);
            obj.SetActive(false);
            _pool.Push(obj);
        }
    }

    public GameObject Get()
    {
        if (_pool.Count > 0)
        {
            GameObject obj = _pool.Pop();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(_prefab);
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        _pool.Push(obj);
    }
}
```

### What to Pool

- Projectiles (bullets, missiles)
- Particle effects
- Enemies that spawn/despawn frequently
- UI elements that appear/disappear
- Audio sources for sound effects
- Any frequently Instantiated/Destroyed object

---

## Material Access

### Avoid Material Cloning

```csharp
// ❌ BAD: .material creates a clone (memory leak until scene change)
renderer.material.color = Color.red;

// ✅ GOOD for READ: Use sharedMaterial
Color currentColor = renderer.sharedMaterial.color;

// ✅ GOOD for WRITE: Use MaterialPropertyBlock
private MaterialPropertyBlock _propertyBlock;
private static readonly int ColorProperty = Shader.PropertyToID("_Color");

void Awake()
{
    _propertyBlock = new MaterialPropertyBlock();
}

void SetColor(Color color)
{
    renderer.GetPropertyBlock(_propertyBlock);
    _propertyBlock.SetColor(ColorProperty, color);
    renderer.SetPropertyBlock(_propertyBlock);
}
```

### Cache Shader Property IDs

```csharp
// ❌ BAD: String lookup every time
material.SetFloat("_Glossiness", 0.5f);

// ✅ GOOD: Cached ID
private static readonly int GlossinessProperty = Shader.PropertyToID("_Glossiness");
material.SetFloat(GlossinessProperty, 0.5f);
```

---

## Physics Optimization

### Collider Performance (Best to Worst)

| Collider Type     | Performance     |
| ----------------- | --------------- |
| Sphere            | ★★★★★ (fastest) |
| Capsule           | ★★★★☆           |
| Box               | ★★★☆☆           |
| Mesh (Convex)     | ★★☆☆☆           |
| Mesh (Non-Convex) | ★☆☆☆☆ (slowest) |

**Rule**: Never use non-convex mesh colliders. Use compound primitive colliders instead.

### Physics Layer Matrix

Configure collision layers to avoid unnecessary collision checks:

```csharp
// In Project Settings > Physics > Layer Collision Matrix
// Disable collisions between layers that never interact

// Example: Player bullets only hit enemies
// Bullets layer only collides with Enemy layer
```

### Rigidbody Best Practices

```csharp
// ✅ Mark non-moving colliders as static in Inspector
// ✅ Use isKinematic for manually controlled objects
// ✅ Disable gravity if not needed
// ✅ Constrain unused axes (Freeze Position/Rotation)
```

---

## Debug.Log Performance

### Remove in Production

```csharp
// ❌ BAD: Debug.Log still executes in builds (string allocation)
Debug.Log($"Player position: {transform.position}");

// ✅ GOOD: Conditional compilation
#if UNITY_EDITOR
Debug.Log($"Player position: {transform.position}");
#endif

// ✅ GOOD: Use [Conditional] attribute for debug methods
[System.Diagnostics.Conditional("UNITY_EDITOR")]
private void LogDebug(string message)
{
    Debug.Log(message);
}
```

---

## String Operations in Unity

### Update Text Efficiently

```csharp
// ❌ BAD: Creates strings every frame
void Update()
{
    scoreText.text = "Score: " + score.ToString();  // 3 allocations!
}

// ✅ BETTER: Only update when changed
private int _lastScore = -1;

void Update()
{
    if (score != _lastScore)
    {
        scoreText.text = "Score: " + score.ToString();
        _lastScore = score;
    }
}

// ✅ BEST: Separate label from value
public TMP_Text scoreLabelText;  // "Score: "
public TMP_Text scoreValueText;  // Just the number

void Start()
{
    scoreLabelText.text = "Score: ";
}

void UpdateScore()
{
    scoreValueText.text = score.ToString();
}
```

---

## Async Operations

### Use Unity's Awaitable (Unity 2023+)

```csharp
// ✅ GOOD: Unity's Awaitable uses pooling internally
async Awaitable LoadDataAsync()
{
    await Awaitable.WaitForSecondsAsync(1f);
    await Awaitable.NextFrameAsync();
    // Process data...
}
```

### Async Scene Loading

```csharp
// ✅ GOOD: Load scenes asynchronously
public async Awaitable LoadSceneAsync(string sceneName)
{
    AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
    asyncLoad.allowSceneActivation = false;

    while (asyncLoad.progress < 0.9f)
    {
        UpdateLoadingBar(asyncLoad.progress);
        await Awaitable.NextFrameAsync();
    }

    asyncLoad.allowSceneActivation = true;
}
```

---

## Memory Cleanup

### Scene Transitions

```csharp
// Call during scene transitions to clean up
public void CleanupMemory()
{
    Resources.UnloadUnusedAssets();
    System.GC.Collect();
}
```

### Addressables Cleanup

```csharp
// When using Addressables
Addressables.Release(handle);
Addressables.ReleaseInstance(gameObject);
```

---

## Quick Reference: Unity Anti-Patterns

| ❌ Anti-Pattern                  | ✅ Solution                 |
| -------------------------------- | --------------------------- |
| `GetComponent<T>()` in Update    | Cache in Awake/Start        |
| `Camera.main` in Update          | Cache the reference         |
| `mesh.vertices` repeatedly       | Use `GetVertices(list)`     |
| `Physics.OverlapSphere`          | Use `OverlapSphereNonAlloc` |
| `gameObject.tag == "Tag"`        | Use `CompareTag("Tag")`     |
| `new WaitForSeconds()` in loop   | Cache yield instructions    |
| `renderer.material` for changes  | Use `MaterialPropertyBlock` |
| `Debug.Log` in builds            | Use conditional compilation |
| Empty Update/FixedUpdate         | Remove unused callbacks     |
| Non-convex mesh colliders        | Use compound primitives     |
| `Instantiate`/`Destroy` spam     | Use object pooling          |
| String concat for UI every frame | Update only on change       |
| `SendMessage`/`BroadcastMessage` | Direct interface calls      |
| `FindObjectOfType` in Update     | Cache in Awake/Start        |
| Many MonoBehaviours with Update  | Centralized update manager  |
| `Transform.position` repeatedly  | Cache in local variable     |

---

## XR/High-Performance Targets (90+ FPS)

For VR/AR/MR applications requiring 90 FPS or higher:

### Frame Budget

- **90 FPS** = 11.1ms per frame
- **120 FPS** = 8.3ms per frame
- Consistent frame timing is critical for user comfort

### XR-Specific Optimizations

| Area                | Recommendation                                   |
| ------------------- | ------------------------------------------------ |
| **Rendering**       | Enable Single Pass Instanced Rendering           |
| **Resolution**      | Use `XRSettings.renderViewportScale` (0.7-1.0)   |
| **Depth Buffer**    | Use 16-bit depth instead of 24-bit               |
| **Far Clip**        | Set to 50m (avoids z-fighting with 16-bit depth) |
| **Post-Processing** | Avoid bloom, MSAA, HDR (millions of ops)         |
| **GI**              | Disable real-time global illumination            |
| **Shadows**         | Disable or set Quality to "Low"                  |

### Service Pattern for Expensive Operations

Run expensive operations (raycasts, input) once per frame via singleton service:

```csharp
public class RaycastService : MonoBehaviour
{
    private RaycastHit[] _hitBuffer = new RaycastHit[32];
    private RaycastHit _cachedHit;
    private bool _didHit;

    void Update()
    {
        // Compute once per frame
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        _didHit = Physics.RaycastNonAlloc(ray, _hitBuffer) > 0;
        if (_didHit)
        {
            _cachedHit = _hitBuffer[0];
        }
    }

    public bool TryGetHit(out RaycastHit hit)
    {
        hit = _cachedHit;
        return _didHit;
    }
}
```

---

## Related Skills

- [high-performance-csharp](high-performance-csharp.md) — Core performance patterns (MANDATORY)
- [use-pooling](use-pooling.md) — Collection pooling patterns
- [refactor-to-zero-alloc](refactor-to-zero-alloc.md) — Migration guide
- [performance-audit](performance-audit.md) — Performance review checklist
- [gc-architecture-unity](gc-architecture-unity.md) — Unity GC architecture details
- [memory-allocation-traps](memory-allocation-traps.md) — Hidden allocation sources
- [mobile-xr-optimization](mobile-xr-optimization.md) — Mobile and XR patterns
