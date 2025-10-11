# Core Math & Extensions

## TL;DR — Why Use These

- Small helpers that fix everyday math and Unity annoyances: safe modulo, wrapped indices, robust equality, fast bounds math, color utilities, and more.
- Copy/paste examples and diagrams show intent; use as building blocks in hot paths.

This guide summarizes the math primitives and extension helpers in this package and shows how to apply them effectively, with examples, performance notes, and practical scenarios.

Contents
- [Numeric helpers](#numeric-helpers) — Positive modulo, wrapped arithmetic, approximate equality, clamping
- [Geometry](#geometry) — Lines, ranges, parabolas, point-in-polygon, polyline simplification
- [Unity extensions](#unity-extensions) — Rect/Bounds conversions, RectTransform bounds, camera bounds, bounds aggregation
- [Color utilities](#color-utilities) — Averaging (LAB/HSV/Weighted/Dominant), hex conversion
- [Collections](#collections) — IEnumerable helpers, buffering, infinite sequences
- [Strings](#strings) — Casing, encoding/decoding, distance
- [Direction helpers](#directions) — Enum conversions and operations
- [Best Practices](#best-practices)

<a id="numeric-helpers"></a>
## Numeric Helpers

- Positive modulo and wrap-around arithmetic
  - Use `PositiveMod` to ensure non-negative modulo results for indices and cyclic counters.
  - Use `WrappedAdd`/`WrappedIncrement` for ring buffer indexes and cursor navigation.

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Helper;

int i = -1;
i = i.PositiveMod(5); // 4
i = i.WrappedAdd(2, 5); // 1

float angle = -30f;
float normalized = angle.PositiveMod(360f); // 330f
```

Diagram (wrap-around on a ring of size 5):
```
Index:   0   1   2   3   4
           ↖           ↙
            \  +2 from 4  => 1

Start at 4, add 2 → 6 → 6 % 5 = 1
```

- Approximate equality
  - `float.Approximately(rhs, tolerance)` and `double.Approximately` add a magnitude-scaled fudge factor.

Example:
```csharp
bool close = 0.1f.Approximately(0.10001f, 0.0001f); // true
```

- Generic `Clamp`
  - `Clamp<T>(min, max)` works for any `IComparable<T>`.

<a id="geometry"></a>
## Geometry

- `Line2D`/`Line3D` operations
  - Length, direction, contains, intersections and closest point calculations.

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Math;
var a = new Line2D(new Vector2(0,0), new Vector2(2,0));
var b = new Line2D(new Vector2(1,-1), new Vector2(1,1));
bool hit = a.Intersects(b); // true
```

Diagram (segment intersection):
```
y↑           b.to (1,1)
 |             │
 |             │  b
 |   a ────────┼────────▶ x
 |         (1,0)×  ← intersection
 |             │
 |             │
 +─────────────┼────────
               b.from (1,-1)
```

- `Range<T>` inclusive/exclusive ranges
  - `Contains`, `Overlaps`, and factory methods for inclusive/exclusive endpoints.

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Math;
var r = Range<int>.Inclusive(0, 10);
bool inside = r.Contains(10); // true
```

- `Parabola`
  - Construct by height/length or coefficients; evaluate by x or normalized position.

Example:
```csharp
var p = new Parabola(maxHeight: 5f, length: 10f);
if (p.TryGetValueAtNormalized(0.5f, out float y)) { /* y == 5 */ }
```

Diagram (normalized parabola):
```
y↑          * vertex (0.5, 5)
 |        *   
 |      *     
 |    *       
 |  *         
 |*           *
 +────────*────────▶ x (t from 0..1)
 0        0.5       1
```

- Point-in-polygon
  - 2D and 3D (projected) tests; remarks on precision and assumptions.

- Polyline simplification (Douglas–Peucker)
  - `Simplify` (float epsilon) and `SimplifyPrecise` (double tolerance) reduce vertex count while preserving shape.

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Helper;
List<Vector2> simplified = LineHelper.Simplify(points, epsilon: 0.1f);
```

Diagram (original vs simplified):
```
Original:     *----*--*---*--*-----*
Simplified:   *-----------*--------*

Fewer vertices within epsilon of the original polyline.
```

Visual:
![Polyline Simplification](Docs/Images/polyline_simplify.svg)

Convex hull (monotone chain / Jarvis examples used by helpers):
```
Points:     ·  ·   ·
          ·      ·   ·
            ·  ·

Hull:     ┌───────────┐
          │           │
          └───────┬───┘
                  └─┐
```

Visual:
![Convex Hull](Docs/Images/convex_hull.svg)

Edge Cases Gallery

![Geometry Edge Cases](Docs/Images/geometry_edge_cases.svg)

<a id="unity-extensions"></a>
## Unity Extensions

- Rect/Bounds conversions, RectTransform world bounds
- Camera `OrthographicBounds`
- Bounds aggregation from collections

Example:
```csharp
Rect r = rectTransform.GetWorldRect();
Bounds view = Camera.main.OrthographicBounds();
```

Diagrams:
- RectTransform world rect (axis-aligned bounds of rotated UI):
```
   • corner         ┌───────────────┐
      ╲             │   AABB (r)    │
       ╲  rotated   │   ┌──────┐    │
        ╲ rectangle │  ╱│ UI  ╱│    │
         •          │ ╱ └────╱─┘    │
                    └───────────────┘
```

- Orthographic camera bounds (centered on camera):
```
            ┌──────── view (Bounds) ────────┐
            │           height=2*size      │
            │         ┌────────────────┐    │
   near ───▶│         │   camera FOV   │    │◀── far
            │         └────────────────┘    │
            └────────────────────────────────┘
```

<a id="color-utilities"></a>
## Color Utilities

- Averaging methods:
  - LAB: perceptually accurate
  - HSV: preserves vibrancy
  - Weighted: luminance-aware
  - Dominant: bucket-based mode

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
Color avg = sprite.GetAverageColor(ColorAveragingMethod.LAB);
string html = avg.ToHex();
```

Dominant color example (bucket-based):
```csharp
// Emphasize palette extraction (posterized sprites, UI swatches)
var dominant = pixels.GetAverageColor(ColorAveragingMethod.Dominant, alphaCutoff: 0.05f);
```

Diagram (dominant buckets):
```
RGB space buckets → counts
 [R][G][B] …  [R+Δ][G][B]  …  [R][G+Δ][B]  …
          ↑ pick max bucket centroid as dominant
```

<a id="collections"></a>
## Collections

- Readable helpers: `AsList`, `ToLinkedList`, `OrderBy(Func)`
- Utilities: `Infinite`, min-bounds from points, bounds aggregation

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
foreach (var v in someList.Infinite()) { /* cycles forever */ }
```

Bounds from points example:
```csharp
// Compute BoundsInt for occupied grid cells
BoundsInt? area = positions.GetBounds(inclusive: false);
if (area is BoundsInt b) { /* use b */ }
```

Bounds aggregation example:
```csharp
// Merge many Bounds (e.g., from Renderers)
Bounds? merged = renderers.Select(r => r.bounds).GetBounds();
```

<a id="strings"></a>
## Strings

- Casing conversions (Pascal, Camel, Snake, Kebab, Title)
- Encoding helpers: `GetBytes`, `GetString`
- Similarity: `LevenshteinDistance`

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
string s = "hello_world";
int distance = s.LevenshteinDistance("hello-world");
```

<a id="directions"></a>
## Directions

- Conversions between enum and vectors; splitting flag sets; combining

Example:
```csharp
using WallstopStudios.UnityHelpers.Core.Extension;
Vector2Int v = Direction.NorthWest.AsVector2Int(); // (-1, 1)
```

## Best Practices

- Use `PositiveMod` instead of `%` for indices and angles when negatives are possible.
- Prefer `SimplifyPrecise` for offline tooling; use `Simplify` during gameplay for speed.
- Choose color averaging method per goal: LAB for perceptual palette, Weighted for speed, Dominant for swatches.
- Favor IReadOnlyList/HashSet specializations to minimize allocations; pooled buffers are used where applicable.
- Run Unity-dependent extensions (e.g., `RectTransform`, `Camera`, `Grid`) on the main thread.

## Related Docs

- Random performance details — [Random Performance](RANDOM_PERFORMANCE.md)
- Serialization formats — [Serialization Guide](SERIALIZATION.md)
- Effects system — [Effects System](EFFECTS_SYSTEM.md)
- Relational Components — [Relational Components](RELATIONAL_COMPONENTS.md)
