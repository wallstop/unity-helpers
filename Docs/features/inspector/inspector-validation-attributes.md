# Inspector Validation Attributes

**Protect your data with declarative validation and read-only presentation.**

Unity Helpers provides validation attributes that help maintain data integrity and prevent accidental modifications. These attributes work seamlessly with the Unity inspector and can validate fields at runtime.

---

## Table of Contents

- [WReadOnly](#wreadonly)
- [WNotNull](#wnotnull)
- [Best Practices](#best-practices)

---

## WReadOnly

Displays a field in the inspector as read-only, preventing accidental modifications while keeping the value visible.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class GameManager : MonoBehaviour
{
    [WReadOnly]
    public string sessionId = "abc-123-xyz";

    [WReadOnly]
    public float elapsedTime = 0f;

    [WReadOnly]
    [SerializeField]
    private int internalScore = 100;
}
```

**Behavior:**

- Field appears grayed out in the inspector
- Value is visible but cannot be edited through the inspector
- Useful for displaying computed values, debug info, or auto-generated IDs
- Works with any serializable field type

> **Visual Reference**
>
> ![WReadOnly attribute showing grayed-out fields in the inspector](../../images/inspector/validation/wreadonly-basic.png)
>
> _Fields marked with [WReadOnly] appear grayed out and cannot be edited_

### Common Use Cases

```csharp
public class Entity : MonoBehaviour
{
    // Auto-generated unique identifier
    [WReadOnly]
    public string entityId = System.Guid.NewGuid().ToString();

    // Computed property exposed for debugging
    [WReadOnly]
    [SerializeField]
    private float _currentHealth;

    // Reference that should only be set via code
    [WReadOnly]
    public Transform cachedTarget;

    // Frame counter for debugging
    [WReadOnly]
    public int framesSinceLastUpdate;
}
```

**Why Use WReadOnly:**

- **Prevent accidents**: Stop designers from accidentally modifying auto-generated values
- **Debug visibility**: Show internal state without allowing modification
- **Documentation**: Make it clear which fields are managed by code vs configured in editor
- **Data integrity**: Protect computed or cached values from manual overrides

> **Visual Reference**
>
> ![WReadOnly showing various field types as read-only](../../images/inspector/validation/wreadonly-use-cases.png)
>
> _Multiple field types (string, float, Transform, int) displayed as read-only_

---

## WNotNull

Validates that a field is not null at runtime. When `CheckForNulls()` is called on an object, any field marked with `[WNotNull]` that is null will throw an `ArgumentNullException`.

### Basic Usage

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class PlayerController : MonoBehaviour
{
    [WNotNull]
    public Rigidbody2D rb;

    [WNotNull]
    public SpriteRenderer spriteRenderer;

    [WNotNull]
    [SerializeField]
    private AudioSource audioSource;

    private void Awake()
    {
        // Validates all [WNotNull] fields are assigned
        // Throws ArgumentNullException if any are null
        this.CheckForNulls();
    }
}
```

**Behavior:**

- Call `this.CheckForNulls()` on any object to validate all `[WNotNull]` fields
- Throws `ArgumentNullException` with the field name if any marked field is null
- Works with both Unity `Object` types and plain C# objects
- Validation runs **only in the Unity Editor** (stripped in builds for performance)

> **Visual Reference**
>
> ![WNotNull fields in inspector with null references highlighted](../../images/inspector/validation/wnotnull-inspector.png)
>
> _Fields marked with [WNotNull] in the inspector - null fields will throw on CheckForNulls()_

### Validation Example

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

public class EnemySpawner : MonoBehaviour
{
    [WNotNull]
    public GameObject enemyPrefab;

    [WNotNull]
    public Transform spawnPoint;

    [WNotNull]
    public EnemyManager enemyManager;

    private void Start()
    {
        // If any [WNotNull] field is null, this throws with the field name
        // Example: ArgumentNullException("enemyPrefab")
        this.CheckForNulls();

        // Safe to use - we know these are assigned
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemyManager.RegisterEnemy(enemy);
    }
}
```

### Editor-Only Validation

The `CheckForNulls()` extension method is only active in the Unity Editor:

```csharp
// The validation code only runs in UNITY_EDITOR
// In builds, CheckForNulls() does nothing (stripped for performance)
this.CheckForNulls();
```

This means:

- **Development**: Full null checking with detailed exception messages
- **Production**: Zero runtime cost (validation code is stripped)

> **Visual Reference**
>
> ![Console showing ArgumentNullException from CheckForNulls](../../images/inspector/validation/wnotnull-exception.png)
>
> _Console output when CheckForNulls() detects a null field - shows exact field name_

### Combining with Other Attributes

```csharp
public class UIManager : MonoBehaviour
{
    // Required reference - validated at runtime
    [WNotNull]
    [SerializeField]
    private Canvas mainCanvas;

    // Optional reference - not validated
    [SerializeField]
    private AudioSource clickSound;

    // Read-only and required
    [WReadOnly]
    [WNotNull]
    public RectTransform cachedRect;

    // Group with validation
    [WGroup("UI Elements")]
    [WNotNull]
    public Button startButton;

    [WNotNull]
    [WGroupEnd("UI Elements")]
    public Button quitButton;
}
```

**Why Use WNotNull:**

- **Early failure**: Catch missing references at game start, not when first used
- **Clear errors**: Get the exact field name in the exception message
- **Documentation**: Make required references explicit in code
- **Zero runtime cost**: Validation stripped from builds

---

## Best Practices

### 1. Validate Early

Call `CheckForNulls()` in `Awake()` or `Start()` to catch missing references immediately:

```csharp
private void Awake()
{
    this.CheckForNulls();
}
```

### 2. Use WReadOnly for Computed Values

```csharp
[WReadOnly]
public float Speed => rb.velocity.magnitude;
```

### 3. Combine with Relational Components

```csharp
// Auto-wired but protected from manual changes
[WReadOnly]
[SiblingComponent]
public Collider2D col;
```

### 4. Document Intent

```csharp
// Required: Must be assigned in inspector
[WNotNull]
public AudioClip attackSound;

// Optional: May be null
public AudioClip hitSound;
```

### 5. Use with ScriptableObjects

```csharp
[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    [WNotNull]
    public GameObject playerPrefab;

    [WNotNull]
    public Material defaultMaterial;

    private void OnEnable()
    {
        this.CheckForNulls();
    }
}
```

---

## See Also

- **[Inspector Grouping Attributes](inspector-grouping-attributes.md)** - Organize related fields
- **[Inspector Conditional Display](inspector-conditional-display.md)** - Show/hide fields conditionally
- **[Relational Components](../relational-components/relational-components.md)** - Auto-wire component references
