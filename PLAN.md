# Unity Helpers – Palette + SerializableDictionary Fix Plan

## Current Issues

- Final validation: re-test palette + Serializable Set drawers inside grouped inspectors and Project Settings (Unity 2021/2022) to ensure the latest spacing/perf tweaks didn’t regress layout or persistence.
- Ensure palette diagnostics are quiet once serialization is validated; remove any temporary logging that’s no longer needed after verification.
- Documentation + release notes still need updates once validation is complete.
- Palette dictionary Sort button doesn’t hide after the visible entries are sorted inside Unity Helpers Settings.
- Dictionary value columns initially render squished/narrow until state changes force a redraw.
- The “New Entry” foldout toggle is ~2.5 px too far right in both inspectors and Project Settings.
- The “New Entry” label text in inspectors sits ~2.5 px too far right (Project Settings alignment is correct).
- The pending foldout “Value” column for complex types needs an extra ~2.5 px right shift to align with row values.

## Objectives

1. Ensure palette edits (existing and new entries) persist immediately via Project Settings.
2. Enable full editing support in the pending entry UI for palette dictionaries (color fields, complex structs).
3. Prevent auto-suggestion logic from overwriting user choices on commit.
4. Support complex serializable value types in the pending drawer (including foldout layout fixes).
5. Add regression tests and docs covering the above scenarios.

## Progress Status

- 2025-11-28: Added palette serialization diagnostics in `UnityHelpersSettings` and `SerializableDictionaryPropertyDrawer` to log `ApplyModifiedProperties` success/failure for palette dictionaries.
- 2025-11-28: Retuned the pending-entry UI to compute real key/value heights so palette “New Entry” drawers (including WEnum colors) have editable controls and no longer clip the Add/Reset buttons.
- 2025-11-28: Palette drawers now mark manual edits so `Ensure*CustomColorDefaults` skips the auto-suggestion pass for newly added/edited keys, preventing user-entered colors from being replaced.
- 2025-11-28: Added EditMode regression tests that cover palette manual edits, auto-suggestion behavior, and the dictionary drawer’s commit workflow to guard against future regressions.
- Remaining palette/UI fixes are still pending until we can validate the serialized output captured by the new diagnostics.
- 2025-11-29: Enabled struct-backed pending entries (wrapping value types via `PendingValueWrapper`, updating the New Entry foldout layout, and adding an EditMode regression test) so the toggle no longer overlaps the key column and complex structs are editable inline.
- 2025-11-29: Realigned the pending/list foldout toggles (normal inspector + grouped dictionaries) and added ColorData struct coverage so color pickers remain interactive, with EditMode tests guarding the layout offsets and struct editing path.
- 2025-11-29: Added a WGroup palette manual-edit regression test plus EditMode coverage that verifies pending headers and dictionary rows respect `GroupGUIWidthUtility` padding so grouped inspectors keep palette edits persistent and visually aligned.
- 2025-11-29: Shifted the “New Entry” foldout toggle spacing and taught the dictionary/set drawers to treat primitive values as inline fields, with EditMode tests ensuring ints render inputs instead of managed-reference wrappers.
- 2025-11-30: Normalized the pending-entry header alignment between inspectors and Project Settings, clamped indent bleed so complex struct values (e.g., `ColorData`) remain editable, and added regression tests covering the new offsets.
- 2025-11-30: Added dynamic foldout gutters for complex dictionary values (pending + rows) so `ColorData` entries expose foldouts consistently, with EditMode coverage tracking when gutters are applied.
- 2025-12-01: Experimented with reduced gutters + forced wrapper expansion to improve `ColorData` editing; alignment improved for headers, but nested fields in New Entry remain greyed out and row gutters still shift the value field too far, leaving the foldout icon overlapping the key. Further adjustments are required (see Pending Entry Layout Polish).
- 2025-12-02: Rebalanced dictionary row layout so complex value columns keep a fixed-width safe zone and widened the key/value gap when foldouts are present, preventing the foldout chevron from overlapping key fields and keeping color pickers full-width. Added an EditMode regression test that asserts the new gap + value width so we don’t regress when tweaking gutter math again.
- 2025-12-02: Follow-up investigations surfaced perf/layout regressions (see Current Issues): pending key typing lags, ColorData arrays aren’t persisted on commit, row value labels still reserve most of the horizontal width, and the pending UI needs tighter padding. These fixes are now prioritized alongside the remaining foldout header polish.
- 2025-12-02: Added array serialization support for pending/committed values (so `ColorData.otherColors` survive commits), shrank the pending/row padding + label widths, and replaced the per-row foldout string keys with value-type keys to reduce allocations and typing lag.
- 2025-12-02: New issues identified after user validation—pending foldout label alignment, pending value content offset, residual typing lag, list/reference collection serialization, and the key field clearing during commit. These items are now the top priority.
- 2025-12-02: Persisted per-row foldout states (so clicking the arrow now expands/collapses and forces a height rebuild), removed the `HideFlags.NotEditable` path from pending wrappers so complex values in “New Entry” are editable again, tightened the pending key/value label widths, and added diagnostics plus EditMode coverage for the foldout resize behavior.
- 2025-12-03: Rebalanced the pending foldout header (toggle offsets 20 px / 10 px for inspector vs. Project Settings, label padding 3 px, label content offset 4 px) and kept the pending value content left shift at 11 px so the chevron gap is tight while the header stays aligned inside grouped inspectors.
- 2025-12-03: Added a serialized-property equality fast path for pending duplicate detection and rewired dictionary row foldouts to render their child properties with a dynamic, clamped label width so typing in pending keys no longer clones every ColorData entry and row value inputs stay usable.
- 2025-12-03: Nudged the pending header toggles 2 px left (18 px / 8 px offsets) and reduced the value left shift to 8.5 px so the “New Entry” foldout aligns inside both inspectors and Project Settings, while row value columns gained a wider default (0.64 ratio for complex values), lighter indent, and padded child rectangles to prevent nested list overdraw.
- 2025-12-03: Tightened the “New Entry” label padding (1 px + 1.5 px content offset), reduced the pending left shift by 3 px whenever foldouts are present, and ensured row foldout states trigger a cache refresh on first render so complex value layouts start aligned before any user interaction.
- 2025-12-03: Further tightened the “New Entry” label spacing (0 px padding / 0.5 px content offset), shifted foldout values ~2.5–3 px to the right by reducing the left-shift only when foldouts exist and bumping the foldout gutter to 7 px, and primed row foldout states from the page cache so complex row values render correctly on the very first frame instead of after the first interaction.
- 2025-12-03: Shifted the New Entry toggle offsets (16 px / 6 px) to move the label left, eliminated the remaining label padding, added a dedicated foldout-value right shift (3 px) so expanded values stay inside the inspector gutter, and wrapped the page cache provider so row foldout priming happens before every height/draw pass.
- 2025-12-03: Palette “Sort” now uses the shared solid-button theme (custom indigo style) and hides itself whenever the currently visible page is already sorted, so users only see the control when the onscreen entries actually need reordering.
- 2025-12-03: Serializable set drawers now share the palette’s solid-button sort theme, hide the Sort control when the visible page is already ordered, auto-expand row foldouts on the first frame so complex values render at full height immediately, and gained EditMode coverage for the new per-page sort detection helper.
- 2025-12-04: Row foldout children now compute label widths per field (clamped for readability), nested array controls inherit depth-based indents so they stay inside the value column, and new EditMode regression tests cover child layout bounds plus dynamic label widths using `ColorData` and `LabelStressValue` entries.
- 2025-12-04: Pending duplicate detection caches per-value revisions so typing string keys no longer re-clones complex values, invalidates the cache whenever palette rows mutate, and adds EditMode coverage ensuring the cache refreshes when dictionary entries change.
- 2025-12-04: Re-centered the pending “New Entry” header (toggle offsets +4 px inspector/+4 px Project Settings), pulled the label 3 px closer to the chevron, and nudged the pending foldout value contents ~3 px left so complex fields stay aligned with the key column without negative padding.
- 2025-12-04: Pending `ColorListData` editors now sync list fields through the wrapper pipeline, with new EditMode coverage verifying inline list edits persist and that committing pending entries writes the updated list values.
- 2025-12-04: Pending complex-value wrappers no longer deep-compare against their serialized copies every frame (we track value revisions + sync tokens instead), eliminating the per-keystroke managed-reference cloning that made key typing laggy whenever the pending foldout was expanded.
- 2025-12-04: Palette commits invoked via the Project Settings drawer now trigger `UnityHelpersSettings.SaveSettings()` immediately (new internal event + EditMode regression test guard against regressions), ensuring Project Settings edits persist without reopening the window.
- 2025-12-04: Added ScriptableObject commit coverage so reference-type dictionary entries (pending + committed) retain their object references, closing the remaining “lists/reference collections persist just like arrays” gap called out during validation.
- 2025-12-05: Tracked pending key/value rects (absolute coordinates), updated the header/toggle layout tracking to honor grouped inspector offsets, and landed EditMode regression tests that verify GroupGUIWidthUtility padding keeps the pending fields and foldout gutters aligned across inspectors and Project Settings.
- 2025-12-05: Validated the UnityHelpersSettings palette dictionaries inside grouped inspectors—New Entry headers and key/value fields now have EditMode coverage that asserts the Project Settings offsets survive GroupGUIWidthUtility padding, and complex row child editors gained grouped-layout tests to ensure nested controls stay inside the value column when padding is applied.
- 2025-12-05: Instrumented the Serializable Set drawer to expose manual-entry and row layout metrics, then added EditMode coverage proving the “New Entry” header/value rows and complex element editors honor GroupGUIWidthUtility padding—keeping the palette/dictionary parity work consistent across the set drawer.
- 2025-12-05: Palette serialization diagnostics are now gated behind the `UNITY_HELPERS_PALETTE_DIAGNOSTICS` define, keeping editor logs quiet by default while still allowing engineers to opt-in when investigating serialization issues.
- 2025-12-05: Updated `docs/features/inspector/inspector-settings.md` with the refreshed palette editing workflow (pending foldouts, immediate persistence, diagnostics toggle) so users understand how to add/overwrite entries without reopening Project Settings.
- 2025-12-05: Realigned the pending “New Entry” header—foldout toggles moved 2.5 px left (inspector + settings), inspector labels shift left by 2.5 px, and complex value columns shift right by 2.5 px, keeping pending UI consistent across contexts while maintaining the foldout gutter adjustments.
- 2025-12-05: Added hash-based detection for dictionary ordering so once the Sort command runs (especially inside UnityHelpersSettings), the footer hides the Sort button until keys change again, preventing the button from lingering after a successful sort.
- 2025-12-05: Guarded dictionary row widths with a fallback equal to the list rect/indent budget so the initial render no longer collapses value columns; rows now reserve the expected min width even before any state change.

## Implementation Plan

1. **Pending Entry Layout Polish**
   - Validate the “New Entry” foldout offsets + gutters in grouped inspectors and Project Settings, ensuring the header, chevron, and value columns stay aligned alongside `GroupGUIWidthUtility` padding.
   - Double-check row/pending complex foldouts when nested arrays or lists are present inside grouped inspectors so child fields never overflow the value column.
   - 2025-12-05 Progress: Added EditMode tests covering UnityHelpersSettings palette dictionaries inside grouped inspectors plus grouped row-child layout coverage for `ColorData` entries (confirming the new offsets hold up under `GroupGUIWidthUtility` padding).
   - 2025-12-05 Progress: Serializable Set manual-entry sections and complex row editors now expose layout diagnostics with EditMode coverage verifying grouped inspectors apply padding consistently, keeping parity with the dictionary drawer.
   - 2025-12-05 Progress: UnityHelpersSettings dictionaries now hash their key order so the footer hides the Sort button immediately after a successful sort until the keys change again.
   - 2025-12-05 Progress: Pending header toggles/labels and complex value gutters received the latest 2.5 px adjustments so both inspectors and Project Settings share identical spacing, matching the user feedback.
2. **Regression Tests**
   - Add EditMode coverage for the grouped-inspector/Project Settings validation scenarios (e.g., `GroupGUIWidthUtility` contexts + Serializable Set parity) once the final layout tweaks are confirmed.
   - 2025-12-05 Progress: Grouped inspector regression tests now cover both palette dictionaries and Serializable Sets (manual entry + complex rows), ensuring layout tweaks stay locked.
3. **Documentation & Release Notes**
   - Update relevant docs (`docs/features/inspector-settings.md`, etc.) describing palette editing workflow.
   - 2025-12-05 Progress: Inspector settings documentation now walks through the New Entry workflow, immediate persistence, and the palette diagnostics define so teams know how to edit/overwrite entries safely.
   - Note fixes in CHANGELOG.

## Next Steps

1. Stabilize complex value editing/layout (pending complex values now edit correctly; remaining work: finish the foldout header/content offsets, restore row child labels + containment for nested arrays, ensure string key typing is instant, and revalidate list/reference persistence end-to-end).
2. Document the workflow and update CHANGELOG once fixes are validated in Unity 2022.3.51f1.
3. Validate the Serializable Set drawer parity updates (pending header alignment, manual-entry perf, row layout) across inspectors and document any follow-up tweaks.

## Future Prevention

- Add CI tests that instantiate `UnityHelpersSettings` in EditMode, run through palette modifications via serialized properties, and verify serialization on disk.
- Introduce logging or assertions when `ApplyModifiedProperties` fails during Project Settings edits.
- Document the palette auto-suggestion rules and ensure they cannot override explicit user choices.
