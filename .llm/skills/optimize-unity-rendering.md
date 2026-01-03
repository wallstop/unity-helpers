# Skill: Unity Rendering Optimization

<!-- trigger: material, shader, render, draw call | Materials, shaders, batching | Performance -->

**Trigger**: When working with materials, shaders, renderers, or any rendering-related operations. This skill focuses on avoiding material cloning, optimizing shader property access, and reducing draw call overhead.

---

## When to Use This Skill

- Modifying material properties at runtime
- Working with shader parameters
- Optimizing draw calls and batching
- Changing object colors or visual properties dynamically
- Managing material instances

---

## Material Access Pitfalls

### The Hidden Clone

Accessing `renderer.material` creates a material instance (clone):

```csharp
// ❌ BAD: Creates a material clone!
renderer.material.color = Color.red;

// This is equivalent to:
Material clone = new Material(renderer.sharedMaterial);
renderer.material = clone;
clone.color = Color.red;
// Clone persists until scene unload = memory leak!
```

### Repeated Access = Multiple Clones

```csharp
// ❌ TERRIBLE: Creates multiple clones
void Update()
{
    renderer.material.color = Color.red;      // Clone 1
    renderer.material.SetFloat("_Gloss", 1);  // Clone 2
    // Each .material access creates a new clone!
}
```

---

## Reading Material Properties

### Use sharedMaterial for Reading

```csharp
// ✅ GOOD: Read from shared material (no allocation)
Color currentColor = renderer.sharedMaterial.color;
float gloss = renderer.sharedMaterial.GetFloat("_Gloss");
```

### Warning: Shared Material Affects All Instances

```csharp
// ⚠️ DANGER: Modifying sharedMaterial affects all renderers using it!
renderer.sharedMaterial.color = Color.red; // ALL objects turn red!
```

---

## Writing Material Properties: MaterialPropertyBlock

### The Pattern

`MaterialPropertyBlock` modifies per-renderer properties without cloning materials:

```csharp
private MaterialPropertyBlock _propertyBlock;
private Renderer _renderer;

// Cache property IDs (see next section)
private static readonly int ColorProperty = Shader.PropertyToID("_Color");
private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");

void Awake()
{
    _propertyBlock = new MaterialPropertyBlock();
    _renderer = GetComponent<Renderer>();
}

void SetColor(Color color)
{
    // Get current block (preserves other properties)
    _renderer.GetPropertyBlock(_propertyBlock);

    // Set properties
    _propertyBlock.SetColor(ColorProperty, color);

    // Apply block
    _renderer.SetPropertyBlock(_propertyBlock);
}
```

### Benefits

- **No material cloning** — Zero allocations after initial setup
- **Per-renderer overrides** — Same material, different properties per object
- **Batching preserved** — Objects can still batch (in most cases)
- **Clean memory** — No material instances to leak

### Available Property Types

```csharp
// All standard shader property types supported
_propertyBlock.SetColor(id, color);
_propertyBlock.SetFloat(id, value);
_propertyBlock.SetInteger(id, value);
_propertyBlock.SetVector(id, vector);
_propertyBlock.SetMatrix(id, matrix);
_propertyBlock.SetTexture(id, texture);
_propertyBlock.SetBuffer(id, buffer);
```

---

## Shader Property ID Caching

### The Problem

String-based property access performs a hash lookup every time:

```csharp
// ❌ BAD: String lookup every call
material.SetFloat("_Glossiness", 0.5f);
material.SetColor("_Color", Color.red);
material.SetTexture("_MainTex", texture);
```

### The Solution

Cache property IDs as static readonly fields:

```csharp
// ✅ GOOD: ID computed once at class load
private static readonly int GlossinessProperty = Shader.PropertyToID("_Glossiness");
private static readonly int ColorProperty = Shader.PropertyToID("_Color");
private static readonly int MainTexProperty = Shader.PropertyToID("_MainTex");

void SetProperties()
{
    material.SetFloat(GlossinessProperty, 0.5f);
    material.SetColor(ColorProperty, Color.red);
    material.SetTexture(MainTexProperty, texture);
}
```

### Common Property IDs

```csharp
// Define in a shared static class for reuse
public static class ShaderProperties
{
    // Standard Shader
    public static readonly int Color = Shader.PropertyToID("_Color");
    public static readonly int MainTex = Shader.PropertyToID("_MainTex");
    public static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
    public static readonly int Metallic = Shader.PropertyToID("_Metallic");
    public static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
    public static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    // URP/HDRP
    public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    public static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
}
```

---

## When to Actually Clone Materials

Sometimes you need true material instances:

### Legitimate Use Cases

1. **Dramatically different properties** — Object looks completely different
2. **Complex animated materials** — Many properties changing together
3. **One-time setup** — Create instance once, never again

### Proper Cloning Pattern

```csharp
private Material _materialInstance;

void Awake()
{
    // Create instance once
    _materialInstance = new Material(renderer.sharedMaterial);
    renderer.material = _materialInstance;
}

void OnDestroy()
{
    // Clean up to prevent memory leak!
    if (_materialInstance != null)
    {
        Destroy(_materialInstance);
    }
}

void UpdateMaterial()
{
    // Use cached instance (no allocation)
    _materialInstance.color = Color.red;
}
```

---

## Multiple Materials on Single Renderer

### Reading Multiple Materials

```csharp
// ❌ BAD: sharedMaterials creates array copy
Material[] mats = renderer.sharedMaterials; // Allocates!

// ✅ GOOD: Use non-allocating version
private readonly List<Material> _materialList = new List<Material>();

void ReadMaterials()
{
    renderer.GetSharedMaterials(_materialList); // No allocation
    foreach (Material mat in _materialList)
    {
        ProcessMaterial(mat);
    }
}
```

### Modifying Specific Material Slots

```csharp
// ❌ BAD: Creates array and clones
renderer.materials[0].color = Color.red; // Clone + array!

// ✅ GOOD: MaterialPropertyBlock with material index
_renderer.GetPropertyBlock(_propertyBlock, materialIndex: 0);
_propertyBlock.SetColor(ColorProperty, Color.red);
_renderer.SetPropertyBlock(_propertyBlock, materialIndex: 0);
```

---

## Sprite Renderer Optimization

### Color Changes

```csharp
// ✅ SpriteRenderer.color is efficient (no material clone)
spriteRenderer.color = Color.red;
```

### Material Property Changes

```csharp
// For shader properties, still use MaterialPropertyBlock
private MaterialPropertyBlock _spritePropertyBlock;

void SetSpriteGlow(float intensity)
{
    spriteRenderer.GetPropertyBlock(_spritePropertyBlock);
    _spritePropertyBlock.SetFloat(GlowIntensity, intensity);
    spriteRenderer.SetPropertyBlock(_spritePropertyBlock);
}
```

---

## UI Image Optimization

### Material Changes in UI

```csharp
// ❌ BAD: Creates material instance
image.material.SetColor("_Color", newColor);

// ✅ GOOD: Use Image.color for tinting
image.color = newColor;

// ✅ GOOD: For complex effects, use shared material reference
public Material sharedEffectMaterial; // Assigned in Inspector
image.material = sharedEffectMaterial; // No clone if assigning directly
```

---

## Draw Call Optimization

### Enable GPU Instancing

For materials used by many objects:

1. In Material Inspector: Enable **GPU Instancing**
2. Shader must support instancing (`#pragma multi_compile_instancing`)

```csharp
// Enable via code
material.enableInstancing = true;
```

### Static Batching

For non-moving objects:

1. Mark objects as **Static** in Inspector
2. Or use `StaticBatchingUtility.Combine()`:

```csharp
void Start()
{
    StaticBatchingUtility.Combine(parentGameObject);
}
```

### Dynamic Batching

For small moving objects (< 300 vertices):

- Enabled in **Player Settings > Other Settings > Dynamic Batching**
- Objects must share material
- Limited vertex count per object

### SRP Batcher (URP/HDRP)

Modern render pipelines use SRP Batcher:

- Shader must be SRP Batcher compatible
- Check in Frame Debugger: "SRP Batch"
- Materials can differ; shader variant must match

---

## Texture Optimization

### Texture Import Settings

| Setting          | Recommendation                       |
| ---------------- | ------------------------------------ |
| Read/Write       | OFF (unless runtime access needed)   |
| Generate Mipmaps | ON for 3D, OFF for UI                |
| Max Size         | Smallest acceptable quality          |
| Compression      | Platform default (ASTC for mobile)   |
| Sprite Atlas     | Group related sprites                |

### Runtime Texture Access

```csharp
// ❌ BAD: Creates copy if Read/Write disabled
Color[] pixels = texture.GetPixels(); // May fail or copy

// ✅ GOOD: Use RenderTexture for GPU-side operations
RenderTexture rt = RenderTexture.GetTemporary(width, height);
Graphics.Blit(sourceTexture, rt, processingMaterial);
// Use rt for rendering
RenderTexture.ReleaseTemporary(rt);
```

---

## Quick Reference: Rendering Anti-Patterns

| ❌ Anti-Pattern                    | ✅ Solution                        |
| ---------------------------------- | ---------------------------------- |
| `renderer.material.color = x`      | `MaterialPropertyBlock`            |
| `renderer.material` in Update      | Cache material instance in Awake   |
| `material.SetFloat("_Name", x)`    | Cache property ID with `PropertyToID` |
| `renderer.sharedMaterials` access  | `GetSharedMaterials(list)`         |
| Modifying `sharedMaterial`         | Use `MaterialPropertyBlock` or clone |
| No GPU Instancing on repeated objects | Enable GPU Instancing           |
| Static objects without batching    | Mark as Static or manual batch     |
| Large uncompressed textures        | Use platform compression           |

---

## Complete Example: Efficient Material System

```csharp
public class EfficientMaterialController : MonoBehaviour
{
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");
    private static readonly int EmissionProperty = Shader.PropertyToID("_EmissionColor");
    private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");

    private MaterialPropertyBlock _propertyBlock;
    private Renderer _renderer;

    void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _renderer = GetComponent<Renderer>();
    }

    public void SetHighlighted(bool highlighted)
    {
        _renderer.GetPropertyBlock(_propertyBlock);

        if (highlighted)
        {
            _propertyBlock.SetColor(EmissionProperty, Color.yellow);
            _propertyBlock.SetFloat(OutlineWidthProperty, 0.02f);
        }
        else
        {
            _propertyBlock.SetColor(EmissionProperty, Color.black);
            _propertyBlock.SetFloat(OutlineWidthProperty, 0f);
        }

        _renderer.SetPropertyBlock(_propertyBlock);
    }

    public void SetTeamColor(Color teamColor)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(ColorProperty, teamColor);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
```

---

## Related Skills

- [unity-performance-patterns](./unity-performance-patterns.md) — General Unity optimization
- [high-performance-csharp](./high-performance-csharp.md) — Zero-allocation patterns
- [mobile-xr-optimization](./mobile-xr-optimization.md) — Mobile rendering constraints
- [memory-allocation-traps](./memory-allocation-traps.md) — Hidden allocation sources
