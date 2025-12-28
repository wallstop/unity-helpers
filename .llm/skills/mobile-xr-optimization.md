# Skill: Mobile and XR Optimization

**Trigger**: When developing for mobile platforms (iOS, Android), VR headsets, AR devices, or any scenario requiring consistent high framerates (90+ FPS).

---

## Platform Requirements

### Frame Rate Targets

| Platform          | Target FPS | Frame Budget |
| ----------------- | ---------- | ------------ |
| Mobile (standard) | 30-60 FPS  | 16.67-33.3ms |
| Mobile (high-end) | 60 FPS     | 16.67ms      |
| VR/AR (minimum)   | 90 FPS     | 11.1ms       |
| VR/AR (ideal)     | 120 FPS    | 8.3ms        |
| Quest 2/3         | 90-120 FPS | 8.3-11.1ms   |
| HoloLens 2        | 60 FPS     | 16.67ms      |

**Critical**: Frame timing consistency matters more than average FPS. A single dropped frame causes visible judder in VR.

---

## CPU Optimization for Mobile/XR

### Cache Everything

Mobile/XR devices have weaker CPUs — every lookup costs more:

```csharp
// ❌ BAD: Multiple lookups per frame
void Update()
{
    Camera.main.transform.position;     // FindGameObjectsWithTag!
    GetComponent<Rigidbody>().velocity; // Component lookup!
}

// ✅ GOOD: Cache in Awake
private Camera _mainCamera;
private Transform _mainCameraTransform;
private Rigidbody _rigidbody;

void Awake()
{
    _mainCamera = Camera.main;
    _mainCameraTransform = _mainCamera.transform;
    _rigidbody = GetComponent<Rigidbody>();
}

void Update()
{
    Vector3 camPos = _mainCameraTransform.position;
    Vector3 vel = _rigidbody.velocity;
}
```

### Centralized Update Manager

Reduce managed/native boundary crossings:

```csharp
// ❌ BAD: 1000 Update() calls = 1000 boundary crossings
public class Enemy : MonoBehaviour
{
    void Update() { UpdateAI(); }
}

// ✅ GOOD: Single Update() manages all
public class EnemyManager : MonoBehaviour
{
    private readonly List<Enemy> _enemies = new List<Enemy>(256);

    void Update()
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            _enemies[i].UpdateAI();
        }
    }
}
```

### Service Pattern for Expensive Operations

Compute expensive operations once per frame:

```csharp
public class InputService : MonoBehaviour
{
    public static InputService Instance { get; private set; }

    // Cached results
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private Ray _pointerRay;
    private RaycastHit[] _hitBuffer = new RaycastHit[16];
    private int _hitCount;

    void Awake() => Instance = this;

    void Update()
    {
        // Compute once per frame
        _moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        _jumpPressed = Input.GetButtonDown("Jump");
        _pointerRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        _hitCount = Physics.RaycastNonAlloc(_pointerRay, _hitBuffer, 100f);
    }

    // Other systems query cached results
    public Vector2 GetMoveInput() => _moveInput;
    public bool WasJumpPressed() => _jumpPressed;
    public bool TryGetPointerHit(out RaycastHit hit)
    {
        hit = _hitCount > 0 ? _hitBuffer[0] : default;
        return _hitCount > 0;
    }
}
```

---

## GPU Optimization for XR

### Single Pass Instanced Rendering

Renders both eyes in a single draw call per object:

```text
Project Settings → XR Plug-in Management → [Platform] → Rendering Mode → Single Pass Instanced
```

**Impact**: Can reduce draw calls by up to 50%.

### Dynamic Resolution Scaling

Reduce render resolution under load:

```csharp
using UnityEngine.XR;

void UpdateResolutionScale()
{
    float targetFrameTime = 1f / 90f;  // 90 FPS target
    float currentFrameTime = Time.unscaledDeltaTime;

    if (currentFrameTime > targetFrameTime * 1.1f)
    {
        // Reduce resolution when struggling
        XRSettings.renderViewportScale = Mathf.Max(0.7f, XRSettings.renderViewportScale - 0.05f);
    }
    else if (currentFrameTime < targetFrameTime * 0.9f)
    {
        // Increase resolution when headroom exists
        XRSettings.renderViewportScale = Mathf.Min(1.0f, XRSettings.renderViewportScale + 0.02f);
    }
}
```

### Depth Buffer Optimization

```csharp
// Use 16-bit depth for better performance
// Project Settings → Quality → Rendering → Depth Format → 16-bit

// Set appropriate far clip plane (50m works well with 16-bit)
_mainCamera.farClipPlane = 50f;
```

### Disable Expensive Effects

| Effect          | Impact      | Recommendation         |
| --------------- | ----------- | ---------------------- |
| Real-time GI    | Very High   | Use baked lighting     |
| MSAA            | High        | Use FXAA or disable    |
| Bloom           | High        | Disable or simplify    |
| HDR             | Medium-High | Disable for mobile     |
| Shadows         | High        | Disable or low quality |
| Post-processing | Very High   | Minimize or disable    |

---

## Memory Optimization for Mobile

### Mobile Memory Constraints

- Mobile devices have **shared memory** between GPU and CPU
- Texture memory competes with game memory
- Lower total RAM than desktop (4-8 GB typical)
- OS can kill apps using too much memory

### Texture Optimization

```csharp
// Use platform-native compression formats
// iOS: ASTC, PVRTC
// Android: ASTC, ETC2
// Avoid: Uncompressed, runtime transcoding

// Texture Import Settings:
// - Read/Write Enabled: OFF (unless needed)
// - Generate Mip Maps: ON for 3D, OFF for UI
// - Max Size: Appropriate for content (512-2048)
// - Compression: Platform default
```

### Audio Optimization

| Setting     | Recommendation                                        |
| ----------- | ----------------------------------------------------- |
| Load Type   | Streaming for music, Decompress On Load for short SFX |
| Compression | Vorbis for music, ADPCM for frequent SFX              |
| Sample Rate | 22050 Hz for most, 44100 Hz only for music            |
| Force Mono  | ON for non-spatial audio                              |

### Asset Loading

```csharp
// ❌ BAD: Resources folder loads everything at startup
var asset = Resources.Load<GameObject>("Prefabs/Enemy");

// ✅ GOOD: Addressables for on-demand loading
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Enemy");
await handle;
var asset = handle.Result;

// ✅ GOOD: Release when done
Addressables.Release(handle);
```

---

## Physics Optimization

### Collider Performance Hierarchy

```text
Sphere < Capsule < Box <<< Convex Mesh <<<< Concave Mesh
 Fast                                           Slow
```

**Rule**: Never use concave mesh colliders. Use compound primitives instead.

### Physics Settings

```csharp
// Reduce physics iterations for mobile
// Edit → Project Settings → Time
// Fixed Timestep: 0.02 (50 Hz) → 0.0333 (30 Hz) for mobile

// Configure layer collision matrix
// Disable collisions between layers that never interact

// Use simple colliders
// Replace mesh colliders with primitive approximations
```

### Raycast Pooling

```csharp
// Pool raycast results
private readonly RaycastHit[] _raycastBuffer = new RaycastHit[16];

void PerformRaycast(Ray ray)
{
    int count = Physics.RaycastNonAlloc(ray, _raycastBuffer, 100f);
    for (int i = 0; i < count; i++)
    {
        ProcessHit(_raycastBuffer[i]);
    }
}
```

---

## Battery Optimization

### Reduce CPU/GPU Work

```csharp
// Lower update frequency for non-critical systems
void Update()
{
    _frameCounter++;
    if (_frameCounter % 3 == 0)  // Update every 3rd frame
    {
        UpdateNonCriticalSystems();
    }
}

// Reduce physics frequency
Time.fixedDeltaTime = 0.0333f;  // 30 Hz instead of 50 Hz
```

### Thermal Throttling Awareness

Mobile devices throttle performance when hot:

```csharp
// Monitor for thermal issues
void Update()
{
    float temp = SystemInfo.batteryLevel;  // Not temperature, but indicator

    // Reduce quality if device is stressed
    if (Application.targetFrameRate > 30 && IsDeviceStressed())
    {
        QualitySettings.DecreaseLevel();
        Application.targetFrameRate = 30;
    }
}
```

---

## XR-Specific Patterns

### Avoid Off-Screen Render Targets

XR displays don't benefit from off-screen effects:

```csharp
// ❌ BAD: Render to texture then display
Camera.targetTexture = renderTexture;

// ✅ GOOD: Direct rendering to display
Camera.targetTexture = null;
```

### Foveated Rendering (Where Available)

Reduces pixel count in peripheral vision:

```csharp
#if UNITY_ANDROID
// Oculus Quest
OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.Medium;
#endif
```

### Reprojection Safety

Ensure critical elements render at full rate:

```csharp
// UI and crosshairs should be on layers rendered at full rate
// Particle effects can be on lower-priority layers
```

---

## Profiling on Device

### Remote Profiler Connection

1. Build with **Development Build** enabled
2. Enable **Autoconnect Profiler**
3. Connect device via USB
4. Open Unity Profiler
5. Select device from dropdown

### Key Metrics to Monitor

| Metric        | Mobile Target | XR Target |
| ------------- | ------------- | --------- |
| Frame Time    | < 16ms        | < 11ms    |
| GC Alloc      | 0 B/frame     | 0 B/frame |
| Draw Calls    | < 100         | < 50      |
| SetPass Calls | < 50          | < 30      |
| Triangles     | < 100K        | < 50K     |

### GPU vs CPU Bound

```csharp
// Test by reducing resolution
// If FPS improves → GPU bound
// If FPS same → CPU bound

// Quick test in editor
XRSettings.renderViewportScale = 0.5f;  // Reduce to 50%
// If smoother, optimize GPU
// If same, optimize CPU
```

---

## Quick Reference: Mobile/XR Checklist

### Must Do

- [ ] Cache all component references in Awake
- [ ] Cache `Camera.main` reference
- [ ] Use centralized update managers
- [ ] Zero per-frame allocations
- [ ] Use non-allocating physics APIs
- [ ] Enable texture compression (ASTC)
- [ ] Disable unnecessary post-processing
- [ ] Profile on target device

### XR-Specific

- [ ] Enable Single Pass Instanced Rendering
- [ ] Use 16-bit depth buffer
- [ ] Set appropriate far clip plane
- [ ] Consider foveated rendering
- [ ] Test with dynamic resolution scaling
- [ ] Disable real-time GI
- [ ] Minimize shadow complexity

### Mobile-Specific

- [ ] Reduce physics tick rate
- [ ] Use Addressables (not Resources)
- [ ] Optimize audio settings
- [ ] Consider 30 FPS for battery life
- [ ] Test thermal throttling behavior
- [ ] Minimize texture sizes

---

## Related Skills

- [high-performance-csharp](high-performance-csharp.md) — Zero-allocation patterns
- [unity-performance-patterns](unity-performance-patterns.md) — Unity optimizations
- [gc-architecture-unity](gc-architecture-unity.md) — Why allocations matter
- [performance-audit](performance-audit.md) — Audit checklist
