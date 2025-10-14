# Changelog

## 2.0.0

- Deprecate BinaryFormatter with `[Obsolete]`, keep functional for trusted/legacy scenarios.
- Make GameObject JSON converter output structured JSON with `name`, `type`, and `instanceId`.
- Fix stray `UnityEditor` imports in Runtime to ensure clean player builds.

## 1.x

- See commit history for incremental features (random engines, spatial trees, serialization converters, editor tools).
