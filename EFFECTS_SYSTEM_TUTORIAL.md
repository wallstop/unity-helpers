# Effects System Tutorial - Build Your First Buff in 5 Minutes

## What You'll Build

By the end of this tutorial, you'll have a complete working buff system with:

- A "Haste" buff that increases speed by 50%
- Visual particle effects that spawn/despawn with the buff
- A "Stunned" debuff that prevents player movement
- Tags you can query in gameplay code

**Time required:** 5-10 minutes

---

## Why Use the Effects System?

**The Old Way:**

```csharp
// 50-100 lines per effect type
public class HasteEffect : MonoBehaviour {
    float duration;
    float speedMultiplier;
    GameObject particles;

    void Update() {
        duration -= Time.deltaTime;
        if (duration <= 0) RemoveSelf();
    }

    void RemoveSelf() {
        // Remove speed modifier...
        // Destroy particles...
        // Handle stacking...
        // 40 more lines...
    }
}
```

**The New Way:**

```csharp
// Zero lines - everything configured in editor
player.ApplyEffect(hasteEffect);  // Done!
```

**Result:** Designers create hundreds of effects without programmer involvement.

---

## Step 1: Create Your First AttributesComponent (2 minutes)

This component will hold the stats that effects can modify.

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Tags;

public class PlayerStats : AttributesComponent
{
    // Define attributes that effects can modify
    public Attribute Speed = 5f;
    public Attribute Health = 100f;
    public Attribute Damage = 10f;

    void Start()
    {
        // Optional: Log when attributes change
        Speed.OnValueChanged += (oldVal, newVal) =>
            Debug.Log($"Speed changed: {oldVal} → {newVal}");
    }
}
```

**What's an Attribute?**

- Holds a base value (e.g., Speed = 5)
- Tracks modifications from multiple sources
- Calculates final value automatically (Add → Multiply → Override)
- Raises events when value changes

---

## Step 2: Add Stats to Your Player (30 seconds)

1. Open your Player prefab/GameObject
2. Add Component → `PlayerStats`
3. Set values in Inspector:
   - Speed: `5`
   - Health: `100`
   - Damage: `10`

That's it! Your player now has modifiable attributes.

---

## Step 3: Create a Haste Effect (2 minutes)

### 3.1 Create the ScriptableObject

1. In Project window: `Right-click` → `Create` → `Wallstop Studios` → `Unity Helpers` → `Attribute Effect`
2. Name it: `HasteEffect`

### 3.2 Configure the Effect

Select `HasteEffect` and set these values in Inspector:

**Modifications:**

- Click **"+"** to add a modification
- Attribute Name: `Speed` (must match field name exactly)
- Action: `Multiplication`
- Value: `1.5` (150% of base speed)

**Duration:**

- Modifier Duration Type: `Duration`
- Duration: `5` (seconds)
- Can Reapply: ✅ (checking this resets timer when reapplied)

**Tags:**

- Effect Tags: Add tag `"Haste"`
- Grant Tags: Add tag `"Haste"` (allows gameplay queries)

### 3.3 Add Visual Effects (Optional)

**Cosmetic Effects:**

- Size: `1`
- Element 0:
  - Prefab: Drag a particle system prefab (or create one)
  - Requires Instancing: ✅ (creates new instance per application)

---

## Step 4: Apply the Effect (30 seconds)

Add this code to test your effect:

```csharp
using UnityEngine;
using WallstopStudios.UnityHelpers.Tags;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private AttributeEffect hasteEffect;
    private PlayerStats stats;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        // Apply haste when pressing H
        if (Input.GetKeyDown(KeyCode.H))
        {
            this.ApplyEffect(hasteEffect);
            Debug.Log($"Speed is now: {stats.Speed.Value}");
        }

        // Move with current speed
        float h = Input.GetAxis("Horizontal");
        transform.position += Vector3.right * h * stats.Speed.Value * Time.deltaTime;
    }
}
```

**Test it:**

1. Assign `HasteEffect` to the Inspector field
2. Press Play
3. Press `H` to apply haste
4. Notice: Speed increases to 7.5, particle effect spawns
5. After 5 seconds: Speed returns to 5, particles disappear

---

## Step 5: Create a Stun Debuff (2 minutes)

Let's make a more complex effect that prevents movement.

### 5.1 Create the Effect

1. `Right-click` → `Create` → `Wallstop Studios` → `Unity Helpers` → `Attribute Effect`
2. Name it: `StunEffect`

### 5.2 Configure Stun

**Modifications:**

- Attribute Name: `Speed`
- Action: `Override`
- Value: `0` (completely override speed to 0)

**Duration:**

- Modifier Duration Type: `Duration`
- Duration: `3`
- Can Reapply: ✅

**Tags:**

- Effect Tags: `"Stun"`, `"Debuff"`
- Grant Tags: `"Stunned"`, `"CC"` (crowd control)

### 5.3 Query Tags in Gameplay

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeField] private AttributeEffect hasteEffect;
    [SerializeField] private AttributeEffect stunEffect;
    private PlayerStats stats;

    void Update()
    {
        // Apply effects
        if (Input.GetKeyDown(KeyCode.H)) this.ApplyEffect(hasteEffect);
        if (Input.GetKeyDown(KeyCode.S)) this.ApplyEffect(stunEffect);

        // Check if player is stunned before allowing movement
        if (this.HasTag("Stunned"))
        {
            Debug.Log("Player is stunned! Cannot move.");
            return;
        }

        // Normal movement
        float h = Input.GetAxis("Horizontal");
        transform.position += Vector3.right * h * stats.Speed.Value * Time.deltaTime;
    }
}
```

**Test it:**

1. Press `S` to stun yourself
2. Try to move - you can't!
3. After 3 seconds, movement returns

---

## Step 6: Advanced Features (5 minutes)

### Stacking Effects

Effects stack independently by default:

```csharp
// Apply haste 3 times
this.ApplyEffect(hasteEffect);  // Speed = 7.5
this.ApplyEffect(hasteEffect);  // Speed = 11.25 (1.5 × 1.5 × 5)
this.ApplyEffect(hasteEffect);  // Speed = 16.875 (1.5 × 1.5 × 1.5 × 5)

// Each stack has its own duration and can be removed independently
```

### Manual Removal

```csharp
// Apply and save handle
EffectHandle? handle = this.ApplyEffect(hasteEffect);

// Remove specific stack early
if (handle.HasValue)
{
    this.RemoveEffect(handle.Value);
}

// Remove all haste effects
this.RemoveAllEffectsWithTag("Haste");
```

### Multiple Modifications Per Effect

One effect can modify multiple attributes:

**Create "Berserker Rage" effect:**

- Modification 1: Speed × 1.3
- Modification 2: Damage × 2.0
- Modification 3: Health × 0.8 (trade-off!)
- Duration: 10 seconds
- Tags: `"Berserker"`, `"Buff"`

### Infinite Duration Effects

For permanent buffs (e.g., equipment):

```csharp
// In Inspector:
// - Modifier Duration Type: Infinite
// - (Duration field is ignored)

// Apply permanent buff
EffectHandle? handle = this.ApplyEffect(permanentStrengthBonus);

// Later, remove when equipment is unequipped
if (handle.HasValue)
    this.RemoveEffect(handle.Value);
```

---

## Common Patterns

### Damage Over Time (DOT)

```csharp
// Create "Poison" effect:
// - Modification: Health + (-2) per second
// - Duration: 10 seconds
// - Tags: "Poison", "DoT", "Debuff"

// Apply to enemy
enemy.ApplyEffect(poisonEffect);

// In PlayerStats, handle negative health:
void Update()
{
    if (Health.Value <= 0)
    {
        Die();
    }
}
```

### Cooldown Reduction

```csharp
// Create "Haste" effect (for abilities):
// - Modification: CooldownRate × 1.5 (50% faster cooldowns)

public class AbilitySystem : AttributesComponent
{
    public Attribute CooldownRate = 1f;
    private float cooldown;

    public void UseAbility()
    {
        // Cooldown respects rate
        cooldown = baseCooldown / CooldownRate.Value;
    }
}
```

### Conditional Effects

```csharp
// Only apply effect if conditions met
void TryApplyBuff(AttributeEffect effect)
{
    // Check if player already has max buffs
    if (this.GetTagCount("Buff") >= 5)
    {
        Debug.Log("Too many buffs active!");
        return;
    }

    // Check if effect is already active
    if (this.HasTag("Haste") && effect == hasteEffect)
    {
        Debug.Log("Haste already active!");
        return;
    }

    this.ApplyEffect(effect);
}
```

---

## Troubleshooting

### "Attribute 'Speed' not found"

- **Cause:** Attribute name in effect doesn't match field name in AttributesComponent
- **Fix:** Names must match exactly (case-sensitive): `Speed` not `speed`
- **Tip:** Use Attribute Metadata Cache generator for dropdown validation

### Effect doesn't apply

- **Check:** Does target GameObject have an `AttributesComponent`?
- **Check:** Is `EffectHandler` component added? (Usually added automatically)
- **Check:** Are there any errors in console?

### Particles don't spawn

- **Check:** Cosmetic Effects → Prefab is assigned
- **Check:** Prefab has a `CosmeticEffectData` component
- **Check:** Requires Instancing is checked if using per-application instances

### Value isn't changing

- **Check:** Attribute name matches exactly
- **Check:** Modification value is non-zero
- **Check:** Action type is correct (Multiplication needs > 0, Addition can be negative)
- **Debug:** Log `attribute.Value` before and after applying effect

---

## Next Steps

You now have a complete buff/debuff system! Here are some ideas to expand:

### Create More Effects

- **Shield:** Health × 2.0, visual shield sprite
- **Slow:** Speed × 0.5, "Slowed" tag
- **Critical:** Damage × 1.5, "Critical" tag
- **Invisibility:** Just tags ("Invisible"), no stat changes
- **Burn:** Health + (-5) per second, fire particles

### Build Systems Around Tags

```csharp
// AI ignores invisible players
if (!target.HasTag("Invisible"))
{
    ChasePlayer(target);
}

// UI shows status icons
if (player.HasTag("Poisoned"))
    ShowPoisonIcon();

// Abilities check prerequisites
if (player.HasTag("Stunned") || player.HasTag("Silenced"))
    return;  // Can't cast

// Interactions respect state
if (player.HasTag("Invulnerable"))
    damage = 0;
```

### Designer Workflows

1. Create effect library (30+ common effects)
2. Designers mix/match on items, abilities, enemies
3. Programmers never touch effect code again!

---

## 📚 Related Documentation

**Core Guides:**

- [Effects System Full Guide](EFFECTS_SYSTEM.md) - Complete API reference and advanced patterns
- [Getting Started](GETTING_STARTED.md) - Your first 5 minutes with Unity Helpers
- [Main README](README.md) - Complete feature overview

**Related Features:**

- [Relational Components](RELATIONAL_COMPONENTS.md) - Auto-wire components (pairs well with effects)
- [Serialization](SERIALIZATION.md) - Save/load effects and attributes

**Need help?** [Open an issue](https://github.com/wallstop/unity-helpers/issues)

---

**Made with ❤️ by Wallstop Studios**

_Effects System tutorial complete! Your designers can now create gameplay effects without code._
