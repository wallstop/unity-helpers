# Effects, Attributes, and Tags — Deep Dive

## TL;DR — What Problem This Solves

- **⭐ Build buff/debuff systems without writing custom code for every effect.**
- Data‑driven ScriptableObjects: designers create 100s of effects, programmers build system once.
- **Time saved: Weeks of boilerplate eliminated + designers empowered to iterate freely.**

### ⭐ The Designer Empowerment Killer Feature

**The Problem - Hardcoded Effects:**

```csharp
// Every buff needs its own custom MonoBehaviour:

public class HasteEffect : MonoBehaviour
{
    private float duration = 5f;
    private float originalSpeed;
    private PlayerStats player;

    void Start()
    {
        player = GetComponent<PlayerStats>();
        originalSpeed = player.speed;
        player.speed *= 1.5f;  // Apply speed boost
    }

    void Update()
    {
        duration -= Time.deltaTime;
        if (duration <= 0)
        {
            player.speed = originalSpeed;  // Restore
            Destroy(this);
        }
    }
}

// 20 effects × 50 lines each = 1000 lines of repetitive code
// Designers can't create effects without programmer
```

**The Solution - Data-Driven:**

```csharp
// Programmers build system once (Unity Helpers provides this):
// - AttributesComponent base class
// - EffectHandler manages application/removal
// - ScriptableObject authoring

// Designers create effects in Editor (NO CODE):
// 1. Right-click → Create → Attribute Effect
// 2. Name: "Haste"
// 3. Add modification: Speed × 1.5
// 4. Duration: 5 seconds
// 5. Done!

// Apply at runtime (one line):
target.ApplyEffect(hasteEffect);
```

**Designer Workflow:**

1. Create effect asset in 30 seconds (no code)
2. Test in-game immediately
3. Tweak values and iterate freely
4. Create variations (Haste II, Haste III) by duplicating assets

**Impact:**

- **Programmer time saved**: Weeks of boilerplate → system built once
- **Designer empowerment**: Block creating 100s of effects instantly
- **Iteration speed**: Change values without code changes/recompiles
- **Maintainability**: All effects in one system vs scattered scripts

Data‑driven gameplay effects that modify stats, apply tags, and drive cosmetic presentation.

This guide explains the concepts, how they work together, authoring patterns, recipes, best practices, and FAQs.

Visuals

![Effects Pipeline](Docs/Images/effects_pipeline.svg)

![Attribute Resolution](Docs/Images/attribute_resolution.svg)

## Concepts

- `Attribute` — A dynamic numeric value with a base and a calculated current value. Current value applies all active modifications.
- `AttributeModification` — Declarative change to an `Attribute`. Actions: Addition, Multiplication, Override. Applied in that order.
- `AttributeEffect` — ScriptableObject asset bundling modifications, tags, cosmetic data, and duration policy.
- `EffectHandle` — Opaque identifier for a specific application instance (for Duration/Infinite effects). Used to remove one stack.
- `AttributesComponent` — Base MonoBehaviour exposing modifiable `Attribute` fields (e.g., Health, Speed) on your character.
- `EffectHandler` — Component that applies/removes effects, tracks durations, forwards modifications to `AttributesComponent`, applies tags and cosmetics.
- `TagHandler` — Counts and queries string tags for gating gameplay (e.g., "Stunned"). Removes tags only when all sources are gone.
- `CosmeticEffectData` — Prefab‑like container with `CosmeticEffectComponent` behaviours; reused or instanced per effect application.

## How It Works

1. You author an `AttributeEffect` with modifications, tags, cosmetics, and duration.
2. You apply it to a GameObject: `EffectHandle? handle = target.ApplyEffect(effect);`
3. `EffectHandler` will:
   - Create an `EffectHandle` (for Duration/Infinite) and track expiration
   - Apply tags via `TagHandler` (counted; multiple sources safe)
   - Apply cosmetic behaviours (`CosmeticEffectData`)
   - Forward `AttributeModification`s to all `AttributesComponent`s on the GameObject
4. On removal (manual or expiration), all of the above are cleanly reversed.

Instant effects modify base values permanently and return `null` instead of a handle.

## Authoring Guide

1. Define stats:

```csharp
public class CharacterStats : AttributesComponent
{
    public Attribute Health = 100f;
    public Attribute Speed = 5f;
    public Attribute Defense = 10f;
}
```

1. Create an `AttributeEffect` asset (Project view → Create → Wallstop Studios → Unity Helpers → Attribute Effect):

- modifications: e.g., `{ attribute: "Speed", action: Multiplication, value: 1.5f }`
- durationType: `Duration` with `duration = 5`
- resetDurationOnReapplication: true to refresh timer on reapply
- effectTags: e.g., `[ "Haste" ]`
- cosmeticEffects: prefab with `CosmeticEffectData` + `CosmeticEffectComponent` scripts

1. Apply/remove at runtime:

```csharp
GameObject player = ...;
AttributeEffect haste = ...; // ScriptableObject reference
EffectHandle? handle = player.ApplyEffect(haste);
// ... later ...
if (handle.HasValue)
{
    player.RemoveEffect(handle.Value);
}
```

1. Query tags anywhere:

```csharp
if (player.HasTag("Stunned"))
{
    // Disable input, play animation, etc.
}
```

## Recipes

### 1) Buff with % Speed for 5s (refreshable)

- Effect: Multiplication `Speed *= 1.5f`, `Duration=5`, `resetDurationOnReapplication=true`, tag `Haste`.
- Apply to extend: reapply before expiry to reset the timer.

### 2) Poison: −5 Health instantly and "Poisoned" tag for 10s

- modifications: Addition `{ attribute: "Health", value: -5f }`
- durationType: Duration `10s`
- effectTags: `[ "Poisoned" ]`
- cosmetics: particles + UI icon

### 3) Equipment Aura: +10 Defense while equipped

- durationType: Infinite
- modifications: Addition `{ attribute: "Defense", value: 10f }`
- Apply on equip, store handle, remove on unequip.

### 4) One‑off Permanent Bonus

- durationType: Instant (returns null)
- modifications: Addition or Override on base value (no handle; cannot be removed).

### 5) Stacking Multiple Instances

- Apply the same effect multiple times → multiple `EffectHandle`s; remove one handle to remove one stack.
- Use tags to gate behaviour regardless of which instance applied it.

### 6) Shared vs Instanced Cosmetics

- In `CosmeticEffectData`, set a component’s `RequiresInstance = true` for per‑application instances (e.g., particles).
- Keep `RequiresInstance = false` for shared presenters (e.g., status icon overlay).

## Best Practices

- Use Addition for flat changes; Multiplication for percentage changes; Override sparingly (wins last).
- Use the Attribute Metadata Cache generator to power editor dropdowns for `attribute` names and avoid typos.
- Centralize tag strings as constants to prevent mistakes and improve refactor safety.
- Prefer shared cosmetics where feasible; instantiate only when state must be isolated per application.
- If reapplication should refresh timers, set `resetDurationOnReapplication = true` on the effect.

### Type-Safe Effect References with Enums

Instead of managing effects through inspector references or Resources.Load calls, consider using an enum-based registry for centralized, type-safe access to all your effects:

**The Pattern:**

```csharp
// 1. Define an enum for all your effects
public enum EffectType
{
    HastePotion,
    StrengthBuff,
    PoisonDebuff,
    ShieldBuff,
    FireDamageOverTime,
}

// 2. Create a centralized registry
public class EffectRegistry : ScriptableObject
{
    [System.Serializable]
    private class EffectEntry
    {
        public EffectType type;
        public AttributeEffect effect;
    }

    [SerializeField] private EffectEntry[] effects;
    private Dictionary<EffectType, AttributeEffect> effectLookup;

    private void OnEnable()
    {
        effectLookup = effects.ToDictionary(e => e.type, e => e.effect);
    }

    public AttributeEffect GetEffect(EffectType type)
    {
        return effectLookup.TryGetValue(type, out AttributeEffect effect)
            ? effect
            : null;
    }
}

// 3. Usage - type-safe and refactorable
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField] private EffectRegistry effectRegistry;

    public void DrinkHastePotion()
    {
        // Compiler ensures this effect exists
        AttributeEffect haste = effectRegistry.GetEffect(EffectType.HastePotion);
        this.ApplyEffect(haste);

        // Typos are caught at compile time
        // effectRegistry.GetEffect(EffectType.HastPotoin); // ❌ Won't compile
    }
}
```

**Using DisplayName for Editor-Friendly Names:**

```csharp
using System.ComponentModel;

public enum EffectType
{
    [Description("Haste Potion")]
    HastePotion,

    [Description("Strength Buff (10s)")]
    StrengthBuff,

    [Description("Poison DoT")]
    PoisonDebuff,

    [Description("Shield (+50 Defense)")]
    ShieldBuff,
}

// Custom PropertyDrawer can display Description in inspector
// Or use Unity's [InspectorName] attribute in Unity 2021.2+:
// [InspectorName("Haste Potion")] HastePotion,
```

**Cached Name Pattern for Performance:**

If you're doing frequent lookups or displaying effect names in UI, cache the enum-to-string mappings:

```csharp
public static class EffectTypeExtensions
{
    private static readonly Dictionary<EffectType, string> DisplayNames = new()
    {
        { EffectType.HastePotion, "Haste Potion" },
        { EffectType.StrengthBuff, "Strength Buff" },
        { EffectType.PoisonDebuff, "Poison" },
        { EffectType.ShieldBuff, "Shield" },
    };

    public static string GetDisplayName(this EffectType type)
    {
        return DisplayNames.TryGetValue(type, out string name)
            ? name
            : type.ToString();
    }
}

// Usage in UI
void UpdateEffectTooltip(EffectType effectType)
{
    tooltipText.text = effectType.GetDisplayName();
    // No allocations, no typos, refactor-safe
}
```

**Benefits:**

✅ **Type safety** - Compiler catches typos and missing effects
✅ **Refactoring** - Rename effects across entire codebase reliably
✅ **Autocomplete** - IDE suggests all available effects
✅ **Performance** - Dictionary lookup faster than Resources.Load
✅ **No magic strings** - Effect references are code symbols, not brittle strings

**Drawbacks:**

⚠️ **Centralization** - All effects must be registered in the enum and registry
⚠️ **Designer friction** - Programmers must add enum entries for new effects
⚠️ **Scalability** - With 100+ effects, enum becomes unwieldy (consider categories)
⚠️ **Asset decoupling** - Effects are tied to code enum, harder to add via mods/DLC

**When to Use:**

- ✅ Small to medium projects (< 50 effects)
- ✅ Programmer-driven effect creation
- ✅ Need strong refactoring safety
- ✅ Want compile-time validation

**When to Avoid:**

- ❌ Designer-driven workflows (they can't add enum entries)
- ❌ Modding/DLC systems (effects defined outside codebase)
- ❌ Very large effect catalogs (enums become bloated)
- ❌ Rapid prototyping (slows iteration)

---

**Integration with Unity Helpers' Built-in Enum Utilities:**

This package already includes high-performance `EnumDisplayNameAttribute` and `ToCachedName()` extensions (see `EnumExtensions.cs:437-478`). You can leverage these for optimal performance:

```csharp
using WallstopStudios.UnityHelpers.Core.Attributes;
using WallstopStudios.UnityHelpers.Core.Extension;

public enum EffectType
{
    [EnumDisplayName("Haste Potion")]
    HastePotion,

    [EnumDisplayName("Strength Buff (10s)")]
    StrengthBuff,

    [EnumDisplayName("Poison DoT")]
    PoisonDebuff,
}

// High-performance cached display name (zero allocation after first call)
void UpdateEffectTooltip(EffectType effectType)
{
    tooltipText.text = effectType.ToDisplayName(); // Uses EnumDisplayNameCache<T>
}

// Or use ToCachedName() for the enum's field name without attributes
void LogEffect(EffectType effectType)
{
    Debug.Log($"Applied: {effectType.ToCachedName()}"); // Uses EnumNameCache<T>
}
```

**Performance characteristics:**

- `ToDisplayName()`: O(1) lookup, zero allocations (array-based for enums ≤256 values)
- `ToCachedName()`: O(1) lookup, zero allocations, thread-safe with concurrent dictionary
- Both use aggressive inlining and avoid boxing

This eliminates the need to manually maintain a `DisplayNames` dictionary as shown in the earlier example—the package already provides optimized caching infrastructure.

## FAQ

Q: Why didn’t I get an `EffectHandle`?

- Instant effects modify the base value permanently and do not return a handle (`null`). Duration/Infinite do.

Q: Do modifications stack across multiple effects?

- Yes. Each `Attribute` applies all active modifications ordered by action: Addition → Multiplication → Override.

Q: How do I remove just one instance of an effect?

- Keep the `EffectHandle` returned from `ApplyEffect` and pass it to `RemoveEffect(handle)`.

Q: Two systems apply the same tag. Who owns removal?

- The tag is reference‑counted. Each application increments the count; removal decrements it. The tag is removed when the count reaches 0.

Q: When should I use tags vs checking stats?

- Use tags to represent categorical states (e.g., Stunned/Poisoned/Invulnerable) independent from numeric values. Check stats for numeric thresholds or calculations.

## Troubleshooting

- Attribute name doesn’t apply
  - Ensure the `attribute` field matches a public/private `Attribute` field name on an `AttributesComponent` subclass.
  - Regenerate the Attribute Metadata Cache to update editor dropdowns.

- Effect didn’t clean up cosmetics
  - Confirm `RequiresInstance` is set correctly and components either clean up themselves (`CleansUpSelf`) or are destroyed by `EffectHandler`.

- Duration didn’t refresh on reapply
  - Set `resetDurationOnReapplication = true` on the `AttributeEffect`.

## Advanced Scenarios: Beyond Buffs and Debuffs

While the Effects System excels at traditional buff/debuff mechanics, its true power lies in building **robust capability systems** that drive complex gameplay decisions across your entire codebase. This section explores advanced patterns that transform tags from "nice-to-have" into mission-critical architecture.

### Understanding the Capability Pattern

**The Problem with Flags:**

Many developers start with hardcoded boolean flags:

```csharp
// ❌ OLD WAY: Scattered boolean flags
public class PlayerController : MonoBehaviour
{
    public bool isInvulnerable;
    public bool canDash;
    public bool hasDoubleJump;
    public bool isInvisible;
    // 50+ booleans later...

    void TakeDamage(float damage)
    {
        if (isInvulnerable) return;
        // ...
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
            Dash();
    }
}

// Problems:
// 1. Every system needs direct references to check flags
// 2. Adding temporary effects requires custom timers
// 3. Multiple sources granting same capability = conflicts
// 4. No centralized place to see what capabilities exist
// 5. Difficult to debug "why can't I do X?"
```

**The Solution - Tag-Based Capabilities:**

```csharp
// ✅ NEW WAY: Tag-based capability system
public class PlayerController : MonoBehaviour
{
    void TakeDamage(float damage)
    {
        // Any system can grant "Invulnerable" tag
        if (this.HasTag("Invulnerable")) return;
        // ...
    }

    void Update()
    {
        // Check capability before allowing action
        if (Input.GetKeyDown(KeyCode.Space) && this.HasTag("CanDash"))
            Dash();
    }
}

// Benefits:
// 1. Decoupled - systems query tags, don't need direct references
// 2. Multiple sources work automatically (reference-counted)
// 3. Temporary effects are free - just apply/remove tag
// 4. Debuggable - inspect TagHandler to see all active tags
// 5. Designer-friendly - add capabilities via ScriptableObjects
```

### When to Use This Pattern

✅ **Perfect for:**

- **State management** - "Stunned", "Invisible", "Invulnerable", "Flying"
- **Capability gating** - "CanDash", "CanDoubleJump", "CanCastSpells"
- **System coordination** - "InCombat", "InCutscene", "InDialogue"
- **Permission systems** - "HasQuestItem", "UnlockedArea", "CompletedTutorial"
- **AI behavior** - "Aggressive", "Fleeing", "Alerted", "Patrolling"
- **Complex gameplay** - "Burning", "Wet", "Electrified" (element interactions)

❌ **Not ideal for:**

- **Simple one-off checks** - If you only check in one place, a boolean is fine
- **Continuous numeric values** - Use Attributes for health, speed, damage
- **Performance-critical inner loops** - Cache tag checks outside hot paths

### Pattern 1: Invulnerability System

**The Problem:** Many different sources need to grant invulnerability (power-ups, cutscenes, dash moves, debug mode). Without tags, you need complex logic to track all sources.

**The Solution:**

```csharp
// === Setup (done once by programmer) ===

// 1. Create invulnerability effects as ScriptableObjects
// DashInvulnerability.asset:
//   - durationType: Duration (0.3 seconds)
//   - effectTags: ["Invulnerable", "Dashing"]
//   - cosmeticEffects: flash sprite white

// PowerStarInvulnerability.asset:
//   - durationType: Duration (10 seconds)
//   - effectTags: ["Invulnerable", "PowerStar"]
//   - cosmeticEffects: rainbow sparkles + music

// DebugInvulnerability.asset:
//   - durationType: Infinite
//   - effectTags: ["Invulnerable", "Debug"]
//   - cosmeticEffects: debug overlay

// === Usage (everywhere in codebase) ===

// Combat system
public class CombatSystem : MonoBehaviour
{
    public void TakeDamage(GameObject target, float damage)
    {
        // One simple check - doesn't care WHY they're invulnerable
        if (target.HasTag("Invulnerable"))
        {
            Debug.Log("Target is invulnerable!");
            return;
        }

        // Apply damage...
    }
}

// Player dash ability
public class DashAbility : MonoBehaviour
{
    [SerializeField] private AttributeEffect dashInvulnerability;

    public void Dash()
    {
        // Grant 0.3s of invulnerability during dash
        this.ApplyEffect(dashInvulnerability);
        // Automatically removed after 0.3s
    }
}

// Debug menu
public class DebugMenu : MonoBehaviour
{
    [SerializeField] private AttributeEffect debugInvulnerability;
    private EffectHandle? debugHandle;

    public void ToggleInvulnerability()
    {
        if (debugHandle.HasValue)
        {
            player.RemoveEffect(debugHandle.Value);
            debugHandle = null;
        }
        else
        {
            debugHandle = player.ApplyEffect(debugInvulnerability);
        }
    }
}

// Cutscene controller
public class CutsceneController : MonoBehaviour
{
    [SerializeField] private AttributeEffect cutsceneInvulnerability;
    private EffectHandle? cutsceneHandle;

    void StartCutscene()
    {
        // Prevent player from taking damage during cutscenes
        cutsceneHandle = player.ApplyEffect(cutsceneInvulnerability);
    }

    void EndCutscene()
    {
        if (cutsceneHandle.HasValue)
            player.RemoveEffect(cutsceneHandle.Value);
    }
}

// AI system
public class EnemyAI : MonoBehaviour
{
    void ChooseTarget()
    {
        // Don't waste time attacking invulnerable targets
        List<GameObject> validTargets = allTargets
            .Where(t => !t.HasTag("Invulnerable"))
            .ToList();

        // Attack closest valid target...
    }
}
```

**Why This Works:**

- ✅ **Multiple sources** - Dash, power-ups, debug mode all grant invulnerability independently
- ✅ **Reference-counted** - All sources must end before invulnerability is removed
- ✅ **Decoupled** - Combat system doesn't know about dash, debug, or cutscene systems
- ✅ **Designer-friendly** - Create new invulnerability sources without code changes
- ✅ **Debuggable** - Inspect TagHandler to see exactly why someone is invulnerable

**Common Pitfall to Avoid:**

```csharp
// ❌ DON'T: Check multiple specific tags
if (target.HasTag("DashInvulnerable") ||
    target.HasTag("PowerStarInvulnerable") ||
    target.HasTag("DebugInvulnerable"))
{
    // Now you need to update this everywhere you add a new invulnerability source!
}

// ✅ DO: Check one general capability tag
if (target.HasTag("Invulnerable"))
{
    // Works with all current and future invulnerability sources
}
```

### Pattern 2: Complex AI Decision-Making

**The Problem:** AI needs to make decisions based on complex state (player stealth, environmental conditions, buffs, etc.). Without a unified system, you end up with brittle if/else chains.

**The Solution:**

```csharp
// === Setup effects that grant capability tags ===

// Stealth.asset:
//   - effectTags: ["Invisible", "Stealthy"]
//   - modifications: (none - just tags)

// InWater.asset:
//   - effectTags: ["Wet", "Swimming"]
//   - modifications: Speed × 0.5

// OnFire.asset:
//   - effectTags: ["Burning", "OnFire"]
//   - modifications: Health + (-5 per second)

// === AI uses tags to make robust decisions ===

public class EnemyAI : MonoBehaviour
{
    public void UpdateAI()
    {
        GameObject player = FindPlayer();

        // 1. Visibility checks
        if (player.HasTag("Invisible"))
        {
            // Can't see invisible targets - use last known position
            PatrolToLastKnownPosition();
            return;
        }

        // 2. Threat assessment
        if (player.HasTag("Invulnerable") && player.HasTag("PowerStar"))
        {
            // Player is powered up - flee!
            Flee(player.transform.position);
            return;
        }

        // 3. Environmental awareness
        if (this.HasTag("Burning"))
        {
            // On fire - prioritize finding water
            GameObject water = FindNearestWater();
            if (water != null)
            {
                MoveTowards(water.transform.position);
                return;
            }
        }

        // 4. Tactical decisions
        if (player.HasTag("Stunned") || player.HasTag("Slowed"))
        {
            // Player is vulnerable - aggressive pursuit
            AggressiveAttack(player);
            return;
        }

        // 5. Element interactions
        if (this.HasTag("Wet") && player.HasTag("ElectricWeapon"))
        {
            // We're wet and player has electric weapon - dangerous!
            MaintainDistance(player, minDistance: 10f);
            return;
        }

        // Default behavior
        ChaseAndAttack(player);
    }

    // Helper: Check multiple conditions easily
    bool CanEngageInCombat()
    {
        // Can't fight if we're stunned, fleeing, or in a cutscene
        return !this.HasTag("Stunned") &&
               !this.HasTag("Fleeing") &&
               !this.HasTag("InCutscene");
    }
}
```

**Why This Works:**

- ✅ **Readable** - AI logic is self-documenting ("if player is invisible")
- ✅ **Extensible** - Add new capabilities without modifying AI code
- ✅ **Composable** - Combine multiple tags for complex conditions
- ✅ **Testable** - Apply tags in tests to verify AI behavior
- ✅ **Designer-friendly** - Designers can create new effects that AI automatically responds to

### Pattern 3: Permission and Unlock Systems

**The Problem:** Games have many gated systems (abilities, areas, features). Tracking unlocks with individual booleans becomes unwieldy.

**The Solution:**

```csharp
// === Setup unlock effects ===

// UnlockDoubleJump.asset:
//   - durationType: Infinite (permanent unlock)
//   - effectTags: ["CanDoubleJump", "HasUpgrade"]

// QuestKeyItem.asset:
//   - durationType: Infinite
//   - effectTags: ["HasKeyItem", "CanEnterDungeon"]

// TutorialComplete.asset:
//   - durationType: Infinite
//   - effectTags: ["TutorialComplete", "CanAccessMultiplayer"]

// === Usage throughout game systems ===

// Ability system
public class PlayerAbilities : MonoBehaviour
{
    void Update()
    {
        // Jump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                Jump();
            }
            // Double jump only works if unlocked
            else if (this.HasTag("CanDoubleJump") && !hasUsedDoubleJump)
            {
                Jump();
                hasUsedDoubleJump = true;
            }
        }

        // Dash
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (this.HasTag("CanDash"))
            {
                Dash();
            }
            else
            {
                ShowMessage("Unlock dash ability first!");
            }
        }
    }
}

// Level gate
public class DungeonGate : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        GameObject player = other.gameObject;

        if (player.HasTag("HasKeyItem"))
        {
            // Has the key - open gate
            OpenGate();
        }
        else
        {
            // Missing key - show hint
            ShowMessage("You need the Ancient Key to enter.");
        }
    }
}

// UI system
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button multiplayerButton;

    void Update()
    {
        // Disable multiplayer until tutorial is complete
        multiplayerButton.interactable = player.HasTag("TutorialComplete");
    }
}

// Save system
public class SaveSystem : MonoBehaviour
{
    public SaveData CreateSaveData(GameObject player)
    {
        // Save all permanent unlocks
        var saveData = new SaveData
        {
            unlockedAbilities = new List<string>()
        };

        // Check all capability tags
        if (player.HasTag("CanDoubleJump"))
            saveData.unlockedAbilities.Add("DoubleJump");

        if (player.HasTag("CanDash"))
            saveData.unlockedAbilities.Add("Dash");

        if (player.HasTag("HasKeyItem"))
            saveData.unlockedAbilities.Add("KeyItem");

        return saveData;
    }

    public void LoadSaveData(GameObject player, SaveData saveData)
    {
        // Reapply permanent unlocks
        foreach (string ability in saveData.unlockedAbilities)
        {
            AttributeEffect unlock = LoadUnlockEffect(ability);
            player.ApplyEffect(unlock);
        }
    }
}
```

**Why This Works:**

- ✅ **Persistent** - Infinite duration effects work like permanent flags
- ✅ **Serializable** - Easy to save/load by checking tags
- ✅ **Discoverable** - Inspect TagHandler to see all unlocks
- ✅ **No hardcoded strings** - Create unlock effects as assets
- ✅ **Prevents duplication** - Reference-counting handles multiple unlock sources

### Pattern 4: Elemental Interaction Systems

**The Problem:** Complex element systems (wet + electric = shock, burning + ice = extinguish) require tracking multiple states and their interactions.

**The Solution:**

```csharp
// === Setup element effects ===

// Wet.asset:
//   - durationType: Duration (10 seconds)
//   - effectTags: ["Wet", "ConductsElectricity"]
//   - cosmeticEffects: water drips

// Burning.asset:
//   - durationType: Duration (5 seconds)
//   - effectTags: ["Burning", "OnFire"]
//   - modifications: Health + (-5 per second)
//   - cosmeticEffects: fire particles

// Frozen.asset:
//   - durationType: Duration (3 seconds)
//   - effectTags: ["Frozen", "Immobilized"]
//   - modifications: Speed × 0

// Electrified.asset:
//   - durationType: Duration (4 seconds)
//   - effectTags: ["Electrified", "Stunned"]
//   - modifications: Speed × 0

// === Interaction system ===

public class ElementalInteractions : MonoBehaviour
{
    [SerializeField] private AttributeEffect wetEffect;
    [SerializeField] private AttributeEffect burningEffect;
    [SerializeField] private AttributeEffect frozenEffect;
    [SerializeField] private AttributeEffect electrifiedEffect;

    public void OnEnvironmentalEffect(GameObject target, string effectType)
    {
        switch (effectType)
        {
            case "Water":
                // Apply wet
                target.ApplyEffect(wetEffect);

                // Water puts out fire
                if (target.HasTag("Burning"))
                {
                    target.RemoveAllEffectsWithTag("Burning");
                    CreateSteamParticles(target.transform.position);
                }
                break;

            case "Fire":
                // Fire dries wet targets
                if (target.HasTag("Wet"))
                {
                    target.RemoveAllEffectsWithTag("Wet");
                    CreateSteamParticles(target.transform.position);
                }
                else
                {
                    // Set on fire if dry
                    target.ApplyEffect(burningEffect);
                }
                break;

            case "Ice":
                // Ice freezes wet targets (stronger effect)
                if (target.HasTag("Wet"))
                {
                    target.ApplyEffect(frozenEffect);
                    target.RemoveAllEffectsWithTag("Wet");
                }
                break;

            case "Electric":
                // Electric shocks wet targets
                if (target.HasTag("Wet"))
                {
                    // Extra damage and stun
                    target.ApplyEffect(electrifiedEffect);
                    target.TakeDamage(20f); // Bonus damage
                    CreateElectricParticles(target.transform.position);
                }
                break;
        }
    }

    public float CalculateElementalDamageMultiplier(GameObject attacker, GameObject target)
    {
        float multiplier = 1f;

        // Fire does extra damage to frozen targets (they thaw)
        if (attacker.HasTag("FireWeapon") && target.HasTag("Frozen"))
            multiplier *= 1.5f;

        // Electric does massive damage to wet targets
        if (attacker.HasTag("ElectricWeapon") && target.HasTag("Wet"))
            multiplier *= 2.0f;

        // Ice does extra damage to burning targets (extinguish)
        if (attacker.HasTag("IceWeapon") && target.HasTag("Burning"))
            multiplier *= 1.5f;

        return multiplier;
    }
}
```

**Why This Works:**

- ✅ **Composable** - Elements interact naturally through tags
- ✅ **Discoverable** - All active elements visible in TagHandler
- ✅ **Designer-friendly** - Create new elements without code changes
- ✅ **Debuggable** - See exact element state at any moment
- ✅ **Extensible** - Add new elements and interactions easily

### Pattern 5: State Machine Replacement

**The Problem:** Traditional state machines become complex with many states and transitions. Tags can represent state more flexibly.

**Traditional Approach:**

```csharp
// ❌ OLD WAY: Rigid state machine
public enum PlayerState
{
    Idle,
    Walking,
    Running,
    Jumping,
    Attacking,
    Stunned,
    // What if player is jumping AND attacking?
    // What if player is attacking AND stunned?
    // Need combinatorial explosion of states!
}

private PlayerState currentState;

void Update()
{
    switch (currentState)
    {
        case PlayerState.Stunned:
            // Can't do anything when stunned
            return;

        case PlayerState.Attacking:
            // Can't move while attacking
            // But what if we want to allow movement during some attacks?
            break;

        // 50 more cases...
    }
}
```

**Tag-Based Approach:**

```csharp
// ✅ NEW WAY: Flexible tag-based state
void Update()
{
    // States can overlap naturally
    bool isGrounded = CheckGrounded();
    bool isMoving = Input.GetAxis("Horizontal") != 0;

    // Check capabilities, not rigid states
    if (this.HasTag("Stunned") || this.HasTag("Frozen"))
    {
        // Can't act while crowd-controlled
        return;
    }

    // Movement
    if (isMoving && !this.HasTag("Immobilized"))
    {
        Move();

        // Can attack while moving (if not attacking already)
        if (Input.GetButtonDown("Fire1") && !this.HasTag("Attacking"))
        {
            Attack();
        }
    }

    // Jumping
    if (Input.GetButtonDown("Jump") && isGrounded)
    {
        if (this.HasTag("CanJump") && !this.HasTag("Jumping"))
        {
            Jump();
        }
    }

    // Special abilities
    if (Input.GetButtonDown("Dash"))
    {
        if (this.HasTag("CanDash") && !this.HasTag("Dashing"))
        {
            Dash();
        }
    }
}

// Actions apply tags to themselves
void Attack()
{
    // Apply "Attacking" tag for duration of attack
    this.ApplyEffect(attackingEffect); // 0.5s duration
    // Play animation...
}

void Dash()
{
    // Apply multiple tags during dash
    this.ApplyEffect(dashingEffect);
    // Effect grants: ["Dashing", "Invulnerable", "FastMovement"]
    // All removed automatically after duration
}
```

**Why This Works:**

- ✅ **Composable** - Multiple states can be active simultaneously
- ✅ **Flexible** - Easy to add conditional behavior based on tags
- ✅ **No spaghetti** - Avoid complex state transition logic
- ✅ **Self-documenting** - Tag names describe what's happening
- ✅ **Designer-friendly** - Add new states via ScriptableObjects

### Pattern 6: Debugging and Cheat Codes

**The Problem:** Debug tools and cheat codes need to temporarily grant capabilities without affecting production code.

**The Solution:**

```csharp
public class DebugConsole : MonoBehaviour
{
    [SerializeField] private AttributeEffect godModeEffect;
    [SerializeField] private AttributeEffect noclipEffect;
    [SerializeField] private AttributeEffect unlockAllEffect;

    private Dictionary<string, EffectHandle?> activeDebugEffects = new();

    void Update()
    {
        // God mode (invulnerable + infinite resources)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugEffect("GodMode", godModeEffect);
        }

        // Noclip (fly through walls)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ToggleDebugEffect("Noclip", noclipEffect);
        }

        // Unlock all abilities
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ApplyDebugEffect("UnlockAll", unlockAllEffect);
        }
    }

    void ToggleDebugEffect(string name, AttributeEffect effect)
    {
        if (activeDebugEffects.TryGetValue(name, out EffectHandle? handle) && handle.HasValue)
        {
            player.RemoveEffect(handle.Value);
            activeDebugEffects.Remove(name);
            Debug.Log($"Debug: {name} OFF");
        }
        else
        {
            EffectHandle? newHandle = player.ApplyEffect(effect);
            activeDebugEffects[name] = newHandle;
            Debug.Log($"Debug: {name} ON");
        }
    }

    void ApplyDebugEffect(string name, AttributeEffect effect)
    {
        player.ApplyEffect(effect);
        Debug.Log($"Debug: Applied {name}");
    }
}

// GodMode.asset:
//   - durationType: Infinite
//   - effectTags: ["Invulnerable", "InfiniteResources", "Debug"]
//   - modifications: Health × 999, Stamina × 999

// Noclip.asset:
//   - durationType: Infinite
//   - effectTags: ["Noclip", "Flying", "Debug"]
//   - cosmeticEffects: ghost transparency

// UnlockAll.asset:
//   - durationType: Infinite
//   - effectTags: ["CanDoubleJump", "CanDash", "CanWallJump", "Debug"]
```

**Why This Works:**

- ✅ **Non-invasive** - Debug code doesn't pollute production code
- ✅ **Discoverable** - Inspect TagHandler to see active debug effects
- ✅ **Reusable** - Same effects can be used by actual gameplay
- ✅ **Safe** - Easy to ensure debug effects don't ship (check for "Debug" tag)

### Comparison to Other Approaches

| Approach               | Pros                                    | Cons                                                  |
| ---------------------- | --------------------------------------- | ----------------------------------------------------- |
| **Boolean Flags**      | Simple, fast                            | Not composable, hard to debug, scattered              |
| **Enums**              | Type-safe, clear options                | Rigid, can't combine states                           |
| **Bitflags**           | Combinable, fast                        | Limited to 64 states, not designer-friendly           |
| **State Machines**     | Structured, predictable                 | Complex with many states, rigid transitions           |
| **Tag System (this!)** | Flexible, composable, designer-friendly | Slightly slower than booleans, strings less type-safe |

**When to Use Tags vs Attributes:**

| Use Case                 | Solution                      | Example                                 |
| ------------------------ | ----------------------------- | --------------------------------------- |
| **Binary state**         | Tag                           | "Invulnerable", "CanDash"               |
| **Numeric value**        | Attribute                     | Health, Speed, Damage                   |
| **Temporary state**      | Tag with Duration             | "Stunned" for 3 seconds                 |
| **Stacking bonuses**     | Attribute with Multiplication | Speed × 1.5 from multiple haste effects |
| **Category membership**  | Tag                           | "Enemy", "Friendly", "Neutral"          |
| **Resource management**  | Attribute                     | Stamina, Mana                           |
| **Permission/unlock**    | Tag with Infinite duration    | "CanEnterDungeon", "TutorialComplete"   |
| **Complex interactions** | Multiple Tags                 | "Wet" + "Electrified" = shocked         |

### Best Practices for Capability Systems

1. **Namespace your tags** - Use prefixes to avoid conflicts

   ```csharp
   // ✅ Good: Clear categories
   "Status_Stunned"
   "Ability_CanDash"
   "Quest_HasKeyItem"
   "Element_Burning"

   // ❌ Bad: Ambiguous
   "Stunned"  // Status or ability?
   "Fire"     // On fire or has fire weapon?
   ```

2. **Create tag constants** - Avoid string typos

   ```csharp
   public static class GameplayTags
   {
       // States
       public const string Invulnerable = "Invulnerable";
       public const string Stunned = "Stunned";
       public const string Invisible = "Invisible";

       // Capabilities
       public const string CanDash = "CanDash";
       public const string CanDoubleJump = "CanDoubleJump";

       // Elements
       public const string Burning = "Burning";
       public const string Wet = "Wet";
       public const string Frozen = "Frozen";
   }

   // Usage
   if (player.HasTag(GameplayTags.Invulnerable))
   {
       // Compiler will catch typos!
   }
   ```

3. **Document tag meanings** - Keep a central registry

   ```csharp
   /// Tags Registry
   /// ===================================
   /// Invulnerable - Cannot take damage from any source
   /// Stunned - Cannot perform any actions (move, attack, cast)
   /// InCombat - Currently engaged in combat (prevents resting, saving)
   /// Invisible - Cannot be seen by AI or targeted
   /// CanDash - Has unlocked dash ability
   /// CanDoubleJump - Has unlocked double jump ability
   /// Wet - Conducts electricity, prevents fire, can be frozen
   /// Burning - Takes fire damage over time, can ignite others
   ```

4. **Use effect tags for internal organization**

   ```csharp
   // EffectTags vs GrantTags:
   // - EffectTags: Internal organization (removable via RemoveAllEffectsWithTag)
   // - GrantTags: Gameplay queries (checked via HasTag)

   // Example effect:
   // HastePotion.asset:
   //   - effectTags: ["Potion", "Buff", "Consumable"]  // For removal/organization
   //   - grantTags: ["Haste", "MovementBuff"]          // For gameplay queries
   ```

5. **Test tag combinations** - Verify interactions work correctly

   ```csharp
   [Test]
   public void TestInvulnerability_MultipleSourcesStack()
   {
       GameObject player = CreateTestPlayer();

       // Apply invulnerability from two sources
       EffectHandle? dash = player.ApplyEffect(dashInvulnerability);
       EffectHandle? powerup = player.ApplyEffect(powerupInvulnerability);

       Assert.IsTrue(player.HasTag("Invulnerable"));

       // Remove one source - should still be invulnerable
       player.RemoveEffect(dash.Value);
       Assert.IsTrue(player.HasTag("Invulnerable"));

       // Remove second source - now vulnerable
       player.RemoveEffect(powerup.Value);
       Assert.IsFalse(player.HasTag("Invulnerable"));
   }
   ```

## Performance Notes

- Attribute field discovery is cached (and can be precomputed by the Attribute Metadata Cache generator).
- Tag queries provide overloads for lists to minimize allocations; prefer `IReadOnlyList<string>` overloads in hot paths.
- Cosmetics can be a significant cost; prefer shared presenters when possible.

---

Related:

- README section: "Effects, Attributes, and Tags"
- Attribute Metadata Cache (Editor Tools) for dropdowns and performance
