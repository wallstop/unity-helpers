# Unity Helpers – Palette + SerializableDictionary Fix Plan

## Current Issues

- Finalize documentation + CHANGELOG updates once the latest fixes are validated in Unity 2021/2022.

## Objectives

1. Add EditMode coverage for the palette sorting behavior in both Project Settings and grouped inspectors.
2. Document the refreshed workflow (palette sorting/diagnostics) and update the CHANGELOG after Unity 2021/2022 validation.
3. Validate Serializable Set drawer parity (pending header alignment, manual-entry perf, row layout) and close any remaining gaps.

## Implementation Plan

1. **String-Key Sorting Fix (DONE)**
   - ✅ Reproduced the UnityHelpersSettings ordering anomaly via palette Sort diagnostics.
   - ✅ Ensured the comparer output persists by reordering the serialized arrays ( `ApplyDictionarySliceOrder` ).
   - 🔜 Add/Edit EditMode coverage that asserts palette keys sort lexically (Project Settings + grouped inspectors).
2. **Documentation & Release Notes**
   - Capture the latest fixes in the CHANGELOG and confirm the updated workflow docs are linked from the release notes.
   - Double-check Unity 2021/2022 validation notes so teams know which editor versions were exercised.
3. **Serializable Set Validation**
   - Re-run grouped-inspector + Project Settings validation for Serializable Set drawers after the sorting fix to ensure parity is preserved.
   - Update or extend existing EditMode tests if the validation uncovers additional layout gaps.

## Next Steps

1. Land EditMode coverage that locks the palette dictionary ordering fix (Project Settings + grouped inspectors).
2. Document the workflow and update CHANGELOG once fixes are validated in Unity 2022.3.51f1.
3. Validate the Serializable Set drawer parity updates (pending header alignment, manual-entry perf, row layout) across inspectors and document any follow-up tweaks.

## Future Prevention

- Add CI tests that instantiate `UnityHelpersSettings` in EditMode, run through palette modifications via serialized properties, and verify serialization on disk.
- Introduce logging or assertions when `ApplyModifiedProperties` fails during Project Settings edits.
- Document the palette auto-suggestion rules and ensure they cannot override explicit user choices.
