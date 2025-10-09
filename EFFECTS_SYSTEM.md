# Effects, Attributes, and Tags — Deep Dive

Data‑driven gameplay effects that modify stats, apply tags, and drive cosmetic presentation.

This guide explains the concepts, how they work together, authoring patterns, recipes, best practices, and FAQs.

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

1) You author an `AttributeEffect` with modifications, tags, cosmetics, and duration.
2) You apply it to a GameObject: `EffectHandle? handle = target.ApplyEffect(effect);`
3) `EffectHandler` will:
   - Create an `EffectHandle` (for Duration/Infinite) and track expiration
   - Apply tags via `TagHandler` (counted; multiple sources safe)
   - Apply cosmetic behaviours (`CosmeticEffectData`)
   - Forward `AttributeModification`s to all `AttributesComponent`s on the GameObject
4) On removal (manual or expiration), all of the above are cleanly reversed.

Instant effects modify base values permanently and return `null` instead of a handle.

## Authoring Guide

1) Define stats:
```csharp
public class CharacterStats : AttributesComponent
{
    public Attribute Health = 100f;
    public Attribute Speed = 5f;
    public Attribute Defense = 10f;
}
```

2) Create an `AttributeEffect` asset (Project view → Create → Wallstop Studios → Unity Helpers → Attribute Effect):
- modifications: e.g., `{ attribute: "Speed", action: Multiplication, value: 1.5f }`
- durationType: `Duration` with `duration = 5`
- resetDurationOnReapplication: true to refresh timer on reapply
- effectTags: e.g., `[ "Haste" ]`
- cosmeticEffects: prefab with `CosmeticEffectData` + `CosmeticEffectComponent` scripts

3) Apply/remove at runtime:
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

4) Query tags anywhere:
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

## Performance Notes

- Attribute field discovery is cached (and can be precomputed by the Attribute Metadata Cache generator).
- Tag queries provide overloads for lists to minimize allocations; prefer `IReadOnlyList<string>` overloads in hot paths.
- Cosmetics can be a significant cost; prefer shared presenters when possible.

---

Related:
- README section: “Effects, Attributes, and Tags”
- Attribute Metadata Cache (Editor Tools) for dropdowns and performance

