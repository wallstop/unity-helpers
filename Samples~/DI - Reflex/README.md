# Reflex Integration – Unity Helpers

This sample shows how to bridge the **Unity Helpers relational component system** with the
[Reflex](https://package.openupm.com/com.gustavopsantos.reflex) dependency injection container.
It mirrors the workflow from the VContainer / Zenject samples, but focuses on Reflex' `SceneScope`
and installer pipeline.

The sample lives under `Samples~/DI - Reflex` and includes the following runtime scripts:

- `ReflexSampleInstaller` – registers a tiny palette service inside the Reflex container so you can
  see dependency injection working alongside relational attributes.
- `ReflexRelationalConsumer` – a MonoBehaviour that receives both Reflex dependencies (`[Inject]`)
  and relational fields (`[SiblingComponent]`, `[ChildComponent]`) with no boilerplate.
- `ReflexSpawner` – demonstrates the `Container` extension methods from
  `WallstopStudios.UnityHelpers.Integrations.Reflex` for instantiating prefabs, hydrating hierarchies,
  and wiring relational fields automatically.
- `ReflexPaletteService` – a very small service used by the consumer to tint scene visuals and prove
  the DI pathway is active.

> ℹ️ The assembly definition limits compilation to projects where the Reflex package is present. If
> you import the sample but do not have `com.gustavopsantos.reflex` installed, Unity will simply skip
> building the sample scripts.

---

## Quick Start

1. **Install the packages**
   - Add `com.wallstop-studios.unity-helpers` to your Unity 2021.3+ project.
   - Install Reflex (`com.gustavopsantos.reflex`) from the Unity Package Manager or OpenUPM.

2. **Import the sample**
   - Open *Window ▸ Package Manager*.
   - Select **Unity Helpers**.
   - Locate **DI – Reflex** under Samples and click **Import**.

3. **Create a scene scope**
   - Add an empty GameObject (e.g. `SceneScope`).
   - Attach Reflex' `SceneScope` component (ships with Reflex).
   - On the **same object** add:
     - `RelationalComponentsInstaller` (from Unity Helpers) – enables scene-wide relational scans
       and hooks additive scenes if desired.
     - `ReflexSampleInstaller` – registers the palette service used inside the sample scripts.
   - Optionally tweak the booleans on `RelationalComponentsInstaller`:
     - `Assign Scene On Initialize` hydrates the active scene right after the container builds.
     - `Include Inactive Objects` scans disabled hierarchies.
     - `Listen For Additive Scenes` keeps wiring scenes loaded at runtime.
     - `Use Single Pass Scan` enables the faster metadata-driven walk.

4. **Drop in the sample components**
   - Place `ReflexRelationalConsumer` on a sprite-bearing GameObject; add a `ParticleSystem` child
     if you want to see the `[ChildComponent]` array populate.
   - Add `ReflexSpawner` somewhere convenient, assign the consumer prefab / hierarchy prefab /
     default parent, and hook up UI buttons or keyboard shortcuts to its public methods.

You now have Reflex injecting services *and* Unity Helpers wiring hierarchy references with zero
manual `GetComponent` calls.

---

## Reflex Container Helpers in Action

All helpers live under `WallstopStudios.UnityHelpers.Integrations.Reflex`. The sample `ReflexSpawner`
shows the most common patterns:

```csharp
using Reflex.Core;
using Reflex.Extensions;
using UnityEngine;
using WallstopStudios.UnityHelpers.Integrations.Reflex;

public sealed class ReflexSpawner : MonoBehaviour
{
    [SerializeField] private ReflexRelationalConsumer _componentPrefab;
    [SerializeField] private GameObject _hierarchyPrefab;

    private Container _container;

    private void Awake()
    {
        // SceneScope ensures the scene has a container; fetch it once.
        _container = gameObject.scene.GetSceneContainer();
    }

    public ReflexRelationalConsumer SpawnComponent(Transform parent)
    {
        return _container.InstantiateComponentWithRelations(_componentPrefab, parent);
    }

    public GameObject SpawnHierarchy(Transform parent)
    {
        return _container.InstantiateGameObjectWithRelations(
            _hierarchyPrefab,
            parent,
            includeInactiveChildren: true
        );
    }

    public void HydrateExisting(GameObject root)
    {
        _container.InjectGameObjectWithRelations(root, includeInactiveChildren: true);
    }
}
```

Key takeaways:

- `InstantiateComponentWithRelations` / `InstantiateGameObjectWithRelations` combine
  instantiation, Reflex injection, and relational assignment in one call.
- `InjectGameObjectWithRelations` upgrades an existing hierarchy that was built outside of Reflex
  (e.g., scene editing tools).
- `AssignRelationalComponents` / `AssignRelationalHierarchy` are available if you only need the
  relational portion.

---

## Understanding the Relational Consumer

`ReflexRelationalConsumer` highlights how the two systems work together:

- `[Inject] private ReflexPaletteService _paletteService;` comes from Reflex.
- `[SiblingComponent] private SpriteRenderer _spriteRenderer;` is provided by Unity Helpers after
  relational scanning.
- `[ChildComponent] private ParticleSystem[] _childParticles;` demonstrates array hydration.

Because `RelationalComponentsInstaller` triggers the scan immediately after Reflex builds the scene
container, all of those fields are ready by `Awake`. Runtime objects spawned via the container get
the same treatment when you call the helpers above.

---

## Customising the Integration

- Need different palette colours? Edit the fields on `ReflexSampleInstaller` or create your own
  installer that registers project-specific services.
- Want to control the scene scan globally? Bind your own `RelationalSceneAssignmentOptions`
  *before* `RelationalComponentsInstaller` runs (for example, on a parent GameObject).
- Using additive scenes? Leave `Listen For Additive Scenes` enabled so the bootstrapper hydrates
  everything that loads later.

---

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| Fields marked with `[SiblingComponent]` stay null | Ensure `RelationalComponentsInstaller` is on the same object (or a child) as `SceneScope` and that *Assign Scene On Initialize* is ticked. |
| `[Inject]` fields are missing | Verify the script assembly is inside the Reflex define (the sample asmdef already handles this) and that the service is bound in one of your installers. |
| Runtime prefabs are missing relational fields | Instantiate them via `container.InstantiateComponentWithRelations` or `container.InstantiateGameObjectWithRelations`. Regular `Instantiate` + manual injection will skip the relational pass. |
| Additively loaded scenes are not wired | Enable the additive listener toggle or call `RelationalReflexSceneBootstrapper.AssignScene` manually after loading. |

---

## Further Reading

- `Docs/RELATIONAL_COMPONENTS.md` – Comprehensive attribute reference and DI integration notes.
- `Docs/GETTING_STARTED.md` – Overview of the Unity Helpers package.
- [Reflex documentation](https://github.com/elraccoone/Reflex) – Official guides and API surface.
- `Runtime/Integrations/Reflex/*.cs` – Source for the helper methods used in this sample.

Happy injecting! ✨
