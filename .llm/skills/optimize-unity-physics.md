# Skill: Unity Physics Optimization

<!-- trigger: physics, collider, raycast, rigidbody | Physics colliders, raycasts, non-alloc | Performance -->

**Trigger**: When working with Unity physics systems including colliders, raycasts, rigidbodies, or any Physics/Physics2D API calls. This skill focuses on eliminating allocations and optimizing physics performance.

---

## When to Use This Skill

- Implementing collision detection or physics-based gameplay
- Using raycasts, overlap checks, or other physics queries
- Configuring colliders and rigidbodies
- Optimizing physics-heavy scenes
- Targeting mobile or XR platforms where physics cost is critical

---

## Physics Query Allocations

### The Problem

Unity's physics APIs often allocate arrays for results. In hot paths, this creates garbage:

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
```

### The Solution: NonAlloc APIs

Unity provides non-allocating versions that write to pre-allocated buffers:

```csharp
// ✅ GOOD: Pre-allocated buffer, zero allocations
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

## Non-Allocating Physics API Reference

### Physics (3D)

| Allocating API              | Non-Allocating Alternative      |
| --------------------------- | ------------------------------- |
| `Physics.RaycastAll`        | `Physics.RaycastNonAlloc`       |
| `Physics.OverlapSphere`     | `Physics.OverlapSphereNonAlloc` |
| `Physics.OverlapBox`        | `Physics.OverlapBoxNonAlloc`    |
| `Physics.OverlapCapsule`    | `Physics.OverlapCapsuleNonAlloc`|
| `Physics.SphereCastAll`     | `Physics.SphereCastNonAlloc`    |
| `Physics.BoxCastAll`        | `Physics.BoxCastNonAlloc`       |
| `Physics.CapsuleCastAll`    | `Physics.CapsuleCastNonAlloc`   |

### Physics2D

| Allocating API                 | Non-Allocating Alternative         |
| ------------------------------ | ---------------------------------- |
| `Physics2D.RaycastAll`         | `Physics2D.RaycastNonAlloc`        |
| `Physics2D.OverlapCircleAll`   | `Physics2D.OverlapCircleNonAlloc`  |
| `Physics2D.OverlapBoxAll`      | `Physics2D.OverlapBoxNonAlloc`     |
| `Physics2D.OverlapCapsuleAll`  | `Physics2D.OverlapCapsuleNonAlloc` |
| `Physics2D.OverlapAreaAll`     | `Physics2D.OverlapAreaNonAlloc`    |
| `Physics2D.CircleCastAll`      | `Physics2D.CircleCastNonAlloc`     |
| `Physics2D.BoxCastAll`         | `Physics2D.BoxCastNonAlloc`        |
| `Physics2D.GetRayIntersection` | `Physics2D.GetRayIntersectionNonAlloc` |

---

## Raycast Patterns

### Single Raycast (No Allocation by Default)

```csharp
// ✅ Single raycast doesn't allocate
if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
{
    ProcessHit(hit);
}
```

### Multiple Results (Use NonAlloc)

```csharp
private readonly RaycastHit[] _raycastBuffer = new RaycastHit[16];

void FindAllHits(Ray ray)
{
    int count = Physics.RaycastNonAlloc(ray, _raycastBuffer, 100f);

    for (int i = 0; i < count; i++)
    {
        ProcessHit(_raycastBuffer[i]);
    }
}
```

### Sorting Results by Distance

```csharp
private readonly RaycastHit[] _raycastBuffer = new RaycastHit[16];

void FindClosestHit(Ray ray)
{
    int count = Physics.RaycastNonAlloc(ray, _raycastBuffer, 100f);

    if (count == 0) return;

    // Sort by distance (NonAlloc doesn't guarantee order)
    System.Array.Sort(_raycastBuffer, 0, count,
        Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance)));

    ProcessHit(_raycastBuffer[0]);
}
```

---

## Overlap Check Patterns

### Sphere Check

```csharp
private readonly Collider[] _overlapBuffer = new Collider[32];

void FindEnemiesInRange(Vector3 position, float radius, LayerMask enemyLayer)
{
    int count = Physics.OverlapSphereNonAlloc(
        position, radius, _overlapBuffer, enemyLayer);

    for (int i = 0; i < count; i++)
    {
        if (_overlapBuffer[i].TryGetComponent<Enemy>(out var enemy))
        {
            enemy.Alert();
        }
    }
}
```

### Box Check with Rotation

```csharp
private readonly Collider[] _boxBuffer = new Collider[16];

void CheckZone(Vector3 center, Vector3 halfExtents, Quaternion rotation)
{
    int count = Physics.OverlapBoxNonAlloc(
        center, halfExtents, _boxBuffer, rotation);

    for (int i = 0; i < count; i++)
    {
        ProcessCollider(_boxBuffer[i]);
    }
}
```

---

## Collider Performance

### Performance Hierarchy (Best to Worst)

| Collider Type     | Performance     | Use Case                    |
| ----------------- | --------------- | --------------------------- |
| Sphere            | ★★★★★ (fastest) | Radial proximity detection  |
| Capsule           | ★★★★☆           | Character controllers       |
| Box               | ★★★☆☆           | Rectangular objects         |
| Mesh (Convex)     | ★★☆☆☆           | Complex but limited shapes  |
| Mesh (Non-Convex) | ★☆☆☆☆ (slowest) | AVOID - use compound        |

### Critical Rules

1. **Never use non-convex mesh colliders** — Replace with compound primitive colliders
2. **Prefer spheres** — A sphere approximation is often sufficient
3. **Limit vertices** — Convex mesh colliders should have < 255 vertices

### Compound Collider Pattern

```csharp
// Instead of one complex mesh collider, use multiple primitives:
// - One box for the main body
// - Spheres for rounded ends
// - Capsules for cylindrical parts

// This is configured in the Inspector hierarchy:
// Parent (Rigidbody)
// ├── Body (BoxCollider)
// ├── Head (SphereCollider)
// └── Arms (CapsuleCollider)
```

---

## Layer Collision Matrix

Configure which layers can collide in **Project Settings > Physics > Layer Collision Matrix**.

### Benefits

- Skips collision checks between non-interacting layers
- Reduces broad-phase overhead
- Critical for large scenes

### Example Configuration

```text
Layer Setup:
- Player (Layer 8)
- Enemy (Layer 9)
- PlayerBullet (Layer 10)
- EnemyBullet (Layer 11)
- Environment (Layer 12)

Collision Matrix:
- PlayerBullet collides with: Enemy, Environment
- EnemyBullet collides with: Player, Environment
- Player collides with: Enemy, Environment, EnemyBullet
- Enemy collides with: Player, Environment, PlayerBullet
```

### Scripted Layer Configuration

```csharp
// Ignore collision between two layers
Physics.IgnoreLayerCollision(playerBulletLayer, playerLayer);
Physics.IgnoreLayerCollision(enemyBulletLayer, enemyLayer);
```

---

## Rigidbody Best Practices

### Static Colliders

Objects that never move should have:
- Collider component
- **No Rigidbody** (Unity optimizes these as static)
- Do not move via Transform (causes physics recalculation)

### Kinematic Rigidbodies

For scripted movement (not physics-driven):

```csharp
// In Inspector or code:
_rigidbody.isKinematic = true;

// Move with MovePosition/MoveRotation for physics compatibility
void FixedUpdate()
{
    _rigidbody.MovePosition(targetPosition);
    _rigidbody.MoveRotation(targetRotation);
}
```

### Constraint Unused Axes

```csharp
// Freeze axes that aren't needed
// Reduces physics calculations

// 2D game - freeze Z position and X/Y rotation:
_rigidbody.constraints =
    RigidbodyConstraints.FreezePositionZ |
    RigidbodyConstraints.FreezeRotationX |
    RigidbodyConstraints.FreezeRotationY;
```

### Disable Gravity When Not Needed

```csharp
// Flying enemies, floating objects, etc.
_rigidbody.useGravity = false;
```

---

## Physics Settings Optimization

### Time Settings

```csharp
// Project Settings > Time
// Fixed Timestep: 0.02 (50 Hz) - default
// For mobile: Consider 0.0333 (30 Hz)

// In code:
Time.fixedDeltaTime = 0.0333f; // 30 Hz
```

### Solver Iterations

```csharp
// Reduce for better performance (less accuracy)
// Project Settings > Physics > Default Solver Iterations
// Default: 6, Mobile: 2-4

Physics.defaultSolverIterations = 4;
Physics.defaultSolverVelocityIterations = 1;
```

### Sleep Threshold

```csharp
// Increase to put objects to sleep faster
Physics.sleepThreshold = 0.01f; // Default 0.005
```

---

## Physics in Update vs FixedUpdate

### The Rule

- **Physics operations** → `FixedUpdate()`
- **Physics queries** → Either (but be consistent)
- **Input-driven physics** → Read input in `Update()`, apply in `FixedUpdate()`

### Correct Pattern

```csharp
private Vector3 _inputForce;

void Update()
{
    // Read input (happens every frame)
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");
    _inputForce = new Vector3(h, 0, v) * moveSpeed;
}

void FixedUpdate()
{
    // Apply physics (happens at fixed rate)
    _rigidbody.AddForce(_inputForce);
}
```

---

## Service Pattern for Physics Queries

When multiple systems need the same physics query results, compute once per frame:

```csharp
public class PhysicsQueryService : MonoBehaviour
{
    public static PhysicsQueryService Instance { get; private set; }

    private readonly RaycastHit[] _groundBuffer = new RaycastHit[8];
    private readonly Collider[] _nearbyBuffer = new Collider[32];

    // Cached results
    private bool _isGrounded;
    private RaycastHit _groundHit;
    private int _nearbyCount;

    void Awake() => Instance = this;

    void FixedUpdate()
    {
        // Ground check - computed once
        Ray groundRay = new Ray(transform.position, Vector3.down);
        int groundHits = Physics.RaycastNonAlloc(groundRay, _groundBuffer, 1.1f);
        _isGrounded = groundHits > 0;
        if (_isGrounded) _groundHit = _groundBuffer[0];

        // Nearby enemies - computed once
        _nearbyCount = Physics.OverlapSphereNonAlloc(
            transform.position, 10f, _nearbyBuffer, enemyLayer);
    }

    public bool IsGrounded => _isGrounded;
    public RaycastHit GroundHit => _groundHit;
    public ReadOnlySpan<Collider> NearbyEnemies => new ReadOnlySpan<Collider>(_nearbyBuffer, 0, _nearbyCount);
}
```

---

## Quick Reference: Physics Anti-Patterns

| ❌ Anti-Pattern                     | ✅ Solution                     |
| ----------------------------------- | ------------------------------- |
| `Physics.OverlapSphere` in loop     | `OverlapSphereNonAlloc`         |
| `Physics.RaycastAll` every frame    | `RaycastNonAlloc` + cache       |
| Non-convex mesh colliders           | Compound primitive colliders    |
| Moving static colliders             | Use kinematic Rigidbody         |
| Physics in `Update()`               | Use `FixedUpdate()`             |
| All layers colliding                | Configure Layer Collision Matrix|
| High solver iterations on mobile    | Reduce to 2-4                   |
| Physics queries in multiple scripts | Centralized query service       |

---

## Related Skills

- [unity-performance-patterns](./unity-performance-patterns.md) — General Unity optimization
- [high-performance-csharp](./high-performance-csharp.md) — Zero-allocation patterns
- [mobile-xr-optimization](./mobile-xr-optimization.md) — Mobile and XR physics tuning
- [refactor-to-zero-alloc](./refactor-to-zero-alloc.md) — Migration guide
