# Hulls (Convex vs Concave)

## TL;DR — When To Use Which

- Convex hull: fastest, safe outer bound; great for coarse collisions and visibility.
- Concave hull: follows shape detail; tunable fidelity vs stability via k/alpha parameters.

This guide explains convex and concave hulls, when to use each, and how they differ.

## Convex Hull

- The smallest convex polygon that contains all points.
- Algorithms: Monotone Chain (a.k.a. Andrew’s), Graham Scan, Jarvis March.
- Characteristics
  - Always convex; no inward dents.
  - Stable and deterministic for fixed input.
  - Often used for coarse collision proxies, shape bounds, and visibility.

Illustration:

![Convex Hull](../../images/spatial/convex-hull.svg)

## Concave Hull

- A polygon that can indent to follow the shape of points more closely.
- Algorithms: k-nearest-neighbor based, alpha-shapes, ball-pivoting variants.
- Characteristics
  - Can capture shape detail; may exclude sparse outliers.
  - Parameterized by k (neighbors) or alpha (radius) controlling “concavity”.
  - May create holes or self-intersections if not constrained; validate output.

Illustration:

![Concave Hull](../../images/spatial/concave-hull.svg)

## Choosing Between Them

- Use convex hull when you need a fast, safe, and simple bound with predictable performance.
- Use concave hull when shape fidelity matters (e.g., silhouette, path enclosure) and you accept a tunable trade-off between detail and stability.

## Tips

- Preprocess: remove duplicate points and optionally simplify clusters.
- Postprocess: enforce clockwise/CCW winding and run self-intersection checks for concave hulls.
- Numerical stability: add small epsilons for colinear checks; include or exclude boundary points consistently.

## API Reference (Grid vs Gridless)

All hull helpers now offer both grid-aware (`Grid` + `FastVector3Int`) and gridless variants so you can work directly with `Vector2`/`FastVector3Int` data:

- Convex hull
  - `points.BuildConvexHull(includeColinearPoints: false)` for pure `Vector2`.
  - `fastPoints.BuildConvexHull(includeColinearPoints: false)` for `FastVector3Int` without a `Grid`.
  - `fastPoints.BuildConvexHull(grid, includeColinearPoints: false)` when you need `Grid.CellToWorld` conversions.
  - Algorithm selection via `ConvexHullAlgorithm` is available for both gridful and gridless overloads.
- Concave hull
  - `vectorPoints.BuildConcaveHull(options)` / `BuildConcaveHullKnn` / `BuildConcaveHullEdgeSplit` for `Vector2`.
  - `fastPoints.BuildConcaveHull(options)` plus the `Knn`/`EdgeSplit` helpers for `FastVector3Int` without requiring a `Grid`.
  - `fastPoints.BuildConcaveHull(grid, options)` remains available when your data lives in grid space.

Because the new overloads reuse the pooled implementations under the hood, behaviour (winding, pruning, GC profile) matches the grid versions—pick whichever signature best matches your data source.
