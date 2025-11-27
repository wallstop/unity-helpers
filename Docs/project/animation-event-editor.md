# Animation Event Editor Architecture

## Overview

`Editor/AnimationEventEditor.cs` drives the Animation Event Editor window. Recent work split the window into a thin coordinator that wires IMGUI controls to a dedicated view‑model (`AnimationEventEditorViewModel`) and several focused helpers. This document explains how the components fit together so future changes avoid re‑introducing the old monolith.

## Components

| Component                             | Responsibility                                                                                                                        |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `AnimationEventEditorViewModel`       | Holds clip state, baseline events, reference curves, dirty detection, clip filtering, move/reset helpers, and frame-rate bookkeeping. |
| `AnimationEventClipSelector`          | Renders the animator clip picker + search field and always includes the current clip in results.                                      |
| `AnimationEventKeyboardShortcuts`     | Centralizes Delete / Ctrl+D / Up / Down handling so the window simply calls the helper during `OnGUI`.                                |
| `AnimationEventTimeFieldRenderer`     | Draws the time/frame inputs (frame-index vs. precise time) and pushes undo operations back to the editor.                             |
| `AnimationEventFunctionFieldRenderer` | Renders function name + search inputs (for non-explicit mode) and busts lookup caches when search terms change.                       |
| `AnimationEventMethodSelector`        | Filters type/method lookups, caches results, auto-selects matches using `functionName`, and validates selections.                     |
| `AnimationEventParameterRenderer`     | Draws the appropriate parameter editor (int, float, string, enum with override, object) based on the selected method signature.       |
| `AnimationEventSpritePreviewRenderer` | Shows the sprite preview, handles Read/Write fixes, and reuses textures via `_spriteTextureCache`.                                    |

## Control Flow

1. `OnGUI` delegates clip selection, keyboard shortcuts, and row rendering to helpers.
2. For each `AnimationEventItem`, the editor:
   - Retrieves the lookup dictionary (`Lookup` or helper-filtered cache in non-explicit mode).
   - Ensures the helper selections (`AnimationEventMethodSelector.EnsureSelection/ValidateSelection`).
   - Draws time/function fields via their renderers.
   - Renders type/method dropdowns and parameters through helper calls.
   - Displays sprite preview via `AnimationEventSpritePreviewRenderer`.
3. Save/reset buttons simply call view-model APIs (`SaveAnimation`, `RefreshAnimationEvents`, `HasPendingChanges`, etc.).

## Testing

Editor tests live in `Tests/Editor/Tools/AnimationEventEditorViewModelTests.cs` and cover:

- Clip filtering/tokenization
- Dirty detection for add/remove/frame-rate changes
- Swap/reset helpers
- Reference-curve ordering and sprite lookup behavior

Smoke coverage (`AnimationEventEditorSmokeTests`) creates the window twice to mimic editor-domain reloads.

## Maintenance Tips

- Add new UI behaviors in helper classes so `AnimationEventEditor` stays focused on layout + wiring.
- Prefer updating the view-model when new state is introduced; expose it through small helper methods invoked by the window.
- Keep tests in sync by unit-testing view-model logic and adding smoke tests only for editor-window regressions.
