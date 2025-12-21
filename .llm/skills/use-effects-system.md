# Skill: Use Effects System

**Trigger**: When implementing buffs, debuffs, status effects, or stat modifications.

---

## Core Components

| Component             | Purpose                                                                 |
| --------------------- | ----------------------------------------------------------------------- |
| `AttributeEffect`     | ScriptableObject defining stat modifications, tags, cosmetics, duration |
| `AttributesComponent` | Base class exposing modifiable `Attribute` fields                       |
| `TagHandler`          | Reference-counted tag queries                                           |
| `EffectHandle`        | Unique ID for tracking/removing specific effect instances               |
| `CosmeticEffectData`  | VFX/SFX prefab data for effects                                         |

---

## Creating an Attributes Component

### Define Your Stats

```csharp
using WallstopStudios.UnityHelpers.Runtime.Tags;

public class PlayerAttributes : AttributesComponent
{
    [SerializeField]
    private Attribute maxHealth = new Attribute(100f);

    [SerializeField]
    private Attribute moveSpeed = new Attribute(5f);

    [SerializeField]
    private Attribute attackDamage = new Attribute(10f);

    [SerializeField]
    private Attribute defense = new Attribute(0f);

    public float MaxHealth => maxHealth.Value;
    public float MoveSpeed => moveSpeed.Value;
    public float AttackDamage => attackDamage.Value;
    public float Defense => defense.Value;
}
```

### Attribute Class

`Attribute` handles base value + modifiers:

```csharp
// Base value: 100
// With +20% modifier: 120
// With +50 flat modifier: 150
```

---

## Creating an AttributeEffect

### Via ScriptableObject Menu

1. Right-click in Project window
2. Create > Wallstop Studios > Unity Helpers > Attribute Effect

### Effect Configuration

```csharp
[CreateAssetMenu(fileName = "NewEffect", menuName = "Effects/Custom Effect")]
public class CustomEffect : AttributeEffect
{
    // Configured in inspector:
    // - Duration (0 = permanent until removed)
    // - Stat modifiers (additive, multiplicative)
    // - Tags to apply
    // - Cosmetic effects (VFX, SFX)
}
```

---

## Applying Effects

### Basic Application

```csharp
public class EffectApplier : MonoBehaviour
{
    [SerializeField]
    private AttributeEffect speedBoostEffect;

    [SerializeField]
    private AttributeEffect poisonEffect;

    public void ApplySpeedBoost(PlayerAttributes target)
    {
        target.ApplyEffect(speedBoostEffect);
    }

    public void ApplyPoison(PlayerAttributes target)
    {
        target.ApplyEffect(poisonEffect);
    }
}
```

### Tracking Effect Instances

```csharp
public class EffectManager : MonoBehaviour
{
    private Dictionary<AttributesComponent, List<EffectHandle>> activeEffects =
        new Dictionary<AttributesComponent, List<EffectHandle>>();

    public EffectHandle ApplyEffect(AttributesComponent target, AttributeEffect effect)
    {
        EffectHandle handle = target.ApplyEffect(effect);

        if (!activeEffects.TryGetValue(target, out List<EffectHandle> handles))
        {
            handles = new List<EffectHandle>();
            activeEffects[target] = handles;
        }

        handles.Add(handle);
        return handle;
    }

    public void RemoveEffect(AttributesComponent target, EffectHandle handle)
    {
        target.RemoveEffect(handle);

        if (activeEffects.TryGetValue(target, out List<EffectHandle> handles))
        {
            handles.Remove(handle);
        }
    }

    public void RemoveAllEffects(AttributesComponent target)
    {
        if (activeEffects.TryGetValue(target, out List<EffectHandle> handles))
        {
            foreach (EffectHandle handle in handles)
            {
                target.RemoveEffect(handle);
            }

            handles.Clear();
        }
    }
}
```

---

## Tag System

### Checking Tags

```csharp
public class CombatSystem : MonoBehaviour
{
    public void ProcessDamage(AttributesComponent target, float damage)
    {
        TagHandler tags = target.Tags;

        // Check for immunity
        if (tags.HasTag("Invulnerable"))
        {
            return;
        }

        // Check for damage modifiers
        if (tags.HasTag("Vulnerable"))
        {
            damage *= 1.5f;
        }

        if (tags.HasTag("Armored"))
        {
            damage *= 0.5f;
        }

        // Apply damage
        ApplyDamage(target, damage);
    }
}
```

### Tag Reference Counting

Tags are reference-counted—multiple effects can add the same tag:

```csharp
// Effect A adds "Burning" tag
target.ApplyEffect(burnEffectA);  // Tags: ["Burning" (count: 1)]

// Effect B also adds "Burning" tag
target.ApplyEffect(burnEffectB);  // Tags: ["Burning" (count: 2)]

// Remove Effect A
target.RemoveEffect(handleA);     // Tags: ["Burning" (count: 1)]

// "Burning" still present until Effect B removed
target.Tags.HasTag("Burning");    // true
```

---

## Cosmetic Effects

### CosmeticEffectData

```csharp
[System.Serializable]
public class CosmeticEffectData
{
    public GameObject vfxPrefab;      // Visual effect prefab
    public AudioClip sfxClip;         // Sound effect
    public float duration;            // How long to play
    public bool attachToTarget;       // Parent to target transform
}
```

### Configuring in AttributeEffect

In the inspector, configure:

- **On Apply VFX/SFX**: Played when effect is applied
- **On Remove VFX/SFX**: Played when effect is removed
- **Persistent VFX**: Stays active while effect is active

---

## Complete Example

### Effect ScriptableObjects

Create these in the editor:

**SpeedBoost.asset**:

- Duration: 10 seconds
- Move Speed: +50%
- Tags: ["SpeedBoosted"]
- On Apply VFX: SpeedBoostParticles

**Poison.asset**:

- Duration: 5 seconds
- Tags: ["Poisoned", "DamageOverTime"]
- Tick Damage: 5 per second

**Shield.asset**:

- Duration: 0 (permanent until removed)
- Defense: +50
- Tags: ["Shielded", "Armored"]

### Usage

```csharp
public class Player : MonoBehaviour
{
    [SerializeField]
    private PlayerAttributes attributes;

    [SerializeField]
    private AttributeEffect speedBoost;

    [SerializeField]
    private AttributeEffect shield;

    private EffectHandle shieldHandle;

    public void UseSpeedBoost()
    {
        attributes.ApplyEffect(speedBoost);
        // Auto-expires after duration
    }

    public void ActivateShield()
    {
        if (shieldHandle == null)
        {
            shieldHandle = attributes.ApplyEffect(shield);
        }
    }

    public void DeactivateShield()
    {
        if (shieldHandle != null)
        {
            attributes.RemoveEffect(shieldHandle);
            shieldHandle = null;
        }
    }

    public bool IsSpeedBoosted()
    {
        return attributes.Tags.HasTag("SpeedBoosted");
    }
}
```

---

## Best Practices

### Effect Stacking

```csharp
// Multiple applications of same effect
// Behavior depends on effect configuration:
// - Stack (each application is separate)
// - Refresh (resets duration)
// - Ignore (no effect if already applied)
```

### Cleanup on Destroy

```csharp
private void OnDestroy()
{
    // Effects are automatically cleaned up when AttributesComponent is destroyed
    // But you may want to trigger removal VFX/SFX manually
}
```

### Persistent vs Timed Effects

```csharp
// Timed effect (auto-expires)
[SerializeField]
private AttributeEffect timedBuff;  // Duration > 0

// Persistent effect (manual removal)
[SerializeField]
private AttributeEffect passiveAbility;  // Duration = 0

private EffectHandle passiveHandle;

void EnablePassive()
{
    passiveHandle = attributes.ApplyEffect(passiveAbility);
}

void DisablePassive()
{
    attributes.RemoveEffect(passiveHandle);
    passiveHandle = null;
}
```
