# Skill: Use Relational Component Attributes

**Trigger**: When automatically finding and assigning components based on Unity GameObject hierarchy relationships (parent, sibling, or child components).

---

## Available Attributes

| Attribute            | Searches                                     | Special Options               |
| -------------------- | -------------------------------------------- | ----------------------------- |
| `[SiblingComponent]` | Same GameObject                              | –                             |
| `[ParentComponent]`  | Up the transform hierarchy                   | `OnlyAncestors`, `MaxDepth`   |
| `[ChildComponent]`   | Down the transform hierarchy (breadth-first) | `OnlyDescendants`, `MaxDepth` |

---

## Common Options (All Attributes)

| Option            | Type     | Default | Description                                            |
| ----------------- | -------- | ------- | ------------------------------------------------------ |
| `Optional`        | `bool`   | `false` | When `true`, no error logged if component not found    |
| `IncludeInactive` | `bool`   | `true`  | Include disabled components/inactive GameObjects       |
| `SkipIfAssigned`  | `bool`   | `false` | Skip assignment if field is already non-null/non-empty |
| `MaxCount`        | `int`    | `0`     | Limit results for collections (0 = unlimited)          |
| `TagFilter`       | `string` | `null`  | Only match GameObjects with this tag (exact match)     |
| `NameFilter`      | `string` | `null`  | Only match GameObjects whose name contains this string |
| `AllowInterfaces` | `bool`   | `true`  | Allow interface/base-type searches                     |

---

## Parent/Child-Specific Options

| Option            | Attribute           | Description                                           |
| ----------------- | ------------------- | ----------------------------------------------------- |
| `OnlyAncestors`   | `[ParentComponent]` | Exclude self, search parents only                     |
| `OnlyDescendants` | `[ChildComponent]`  | Exclude self, search children only                    |
| `MaxDepth`        | Both                | Limit hierarchy depth (1 = immediate parent/children) |

---

## Supported Collection Types

Fields can be:

- **Single**: `private Rigidbody2D rb;`
- **Array**: `private Collider2D[] colliders;`
- **List**: `private List<SpriteRenderer> renderers;`
- **HashSet**: `private HashSet<AudioSource> audioSources;`

---

## Sibling Components

Find components on the same GameObject:

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class Player : MonoBehaviour
{
    // Required – logs error if not found
    [SiblingComponent]
    private Animator animator;

    // Optional – no error if missing
    [SiblingComponent(Optional = true)]
    private Rigidbody2D rb;

    // Collect all matching
    [SiblingComponent]
    private List<Collider2D> allColliders;

    // Filter by tag and name
    [SiblingComponent(TagFilter = "Visual", NameFilter = "Sprite")]
    private Component[] visualComponents;

    private void Awake()
    {
        this.AssignSiblingComponents();
    }
}
```

---

## Parent Components

Find components up the hierarchy:

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public interface IHealth { int Current { get; } }

public class ChildComponent : MonoBehaviour
{
    // Immediate parent only (excludes self)
    [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
    private Transform directParent;

    // Search up to 3 levels for tagged component
    [ParentComponent(OnlyAncestors = true, MaxDepth = 3, TagFilter = "Player")]
    private Collider2D playerAncestorCollider;

    // Interface lookup up the chain
    [ParentComponent]
    private IHealth healthProvider;

    // Collect first 2 matches
    [ParentComponent(MaxCount = 2)]
    private Rigidbody2D[] firstTwoRigidbodies;

    private void Awake()
    {
        this.AssignParentComponents();
    }
}
```

---

## Child Components

Find components down the hierarchy (breadth-first):

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class EnemyRoot : MonoBehaviour
{
    // Immediate children only (excludes self)
    [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
    private Transform[] immediateChildren;

    // Find first descendant with tag
    [ChildComponent(OnlyDescendants = true, TagFilter = "Weapon")]
    private Collider2D weaponCollider;

    // Gather into HashSet (no duplicates)
    [ChildComponent(OnlyDescendants = true, MaxCount = 10)]
    private HashSet<Rigidbody2D> firstTenRigidbodies;

    // Include inactive children
    [ChildComponent(IncludeInactive = true)]
    private SpriteRenderer[] allSprites;

    private void Awake()
    {
        this.AssignChildComponents();
    }
}
```

---

## Calling Assignment Methods

### Individual Methods

```csharp
this.AssignSiblingComponents();  // [SiblingComponent] fields only
this.AssignParentComponents();   // [ParentComponent] fields only
this.AssignChildComponents();    // [ChildComponent] fields only
```

### All At Once

```csharp
// Assigns all relational attributes in one call
this.AssignRelationalComponents();
```

---

## DI Framework Integration

Relational attributes work alongside dependency injection frameworks. Use them for hierarchy-based component references while DI handles service injection:

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;
using VContainer;  // or Zenject, Reflex

public class Enemy : MonoBehaviour
{
    // Injected by DI framework
    [Inject] private IGameManager _gameManager;
    [Inject] private IPoolService _poolService;

    // Found via hierarchy
    [SiblingComponent] private Animator _animator;
    [ChildComponent(OnlyDescendants = true)] private Collider2D[] _hitboxes;
    [ParentComponent(OnlyAncestors = true, MaxDepth = 1)] private Transform _parentContainer;

    private void Awake()
    {
        // Called after DI injection
        this.AssignRelationalComponents();
    }
}
```

---

## Complete Example

```csharp
using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class CharacterController : MonoBehaviour
{
    // --- Sibling Components (same GameObject) ---
    [SiblingComponent]
    private Animator animator;

    [SiblingComponent]
    private Rigidbody2D rb;

    [SiblingComponent(Optional = true)]
    private AudioSource audioSource;

    // --- Parent Components (up the hierarchy) ---
    [ParentComponent(OnlyAncestors = true, MaxDepth = 1)]
    private Transform parentContainer;

    [ParentComponent(OnlyAncestors = true, TagFilter = "Manager")]
    private Component managerComponent;

    // --- Child Components (down the hierarchy) ---
    [ChildComponent(OnlyDescendants = true, MaxDepth = 1)]
    private SpriteRenderer[] immediateChildSprites;

    [ChildComponent(OnlyDescendants = true, NameFilter = "Hitbox")]
    private List<Collider2D> hitboxColliders;

    [ChildComponent(OnlyDescendants = true, TagFilter = "VFX", IncludeInactive = true)]
    private HashSet<ParticleSystem> vfxSystems;

    [ChildComponent(OnlyDescendants = true, MaxCount = 5)]
    private Transform[] firstFiveChildren;

    private void Awake()
    {
        // Wire up all relational fields
        this.AssignRelationalComponents();

        // Now use the components
        animator.Play("Idle");
        rb.gravityScale = 1f;

        foreach (Collider2D hitbox in hitboxColliders)
        {
            hitbox.enabled = true;
        }
    }
}
```

---

## Filter Behavior

- **TagFilter** and **NameFilter** can be combined – both must match (AND logic)
- When `IncludeInactive = false`, inactive components are filtered out _before_ tag/name filters
- **MaxCount** is applied last, after all other filters
- **NameFilter** is case-sensitive substring match
- **TagFilter** uses `GameObject.CompareTag()` for efficient exact matching

---

## Notes

- Fields are populated at **runtime**, not during Unity serialization
- Call assignment methods in `Awake()` or `OnEnable()` before dependent code runs
- Child search is **breadth-first** – closer descendants found before distant ones
- For performance-critical scenarios, consider using `RelationalComponentInitializer.Initialize()` during loading to pre-cache reflection data
