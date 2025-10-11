# Spatial Tree Semantics

This page explains how the 2D and 3D spatial structures compare in terms of correctness and why some 3D variants may produce different results for identical inputs and queries.

## 2D: Consistent Results Across QuadTree2D and KdTree2D

- QuadTree2D and KdTree2D (balanced and unbalanced) index points and use equivalent per‑point checks for range and bounds queries. For the same input data and the same queries, they return the same results. Differences are limited to construction/query performance and memory layout.
- RTree2D differs by design: it indexes rectangles (AABBs). If your elements have size, intersection/containment semantics involve those sizes, so results will differ from point‑based trees.

## 3D: Why KdTree3D and OctTree3D Can Differ

While KdTree3D and OctTree3D are both point‑based and target equivalent use cases, algorithmic choices can yield different edge‑case behavior for identical inputs/queries.

Key reasons and scenarios:

- Split planes and child assignment
  - KdTree3D splits by alternating axes (x, y, z); balanced builds use median selection, unbalanced builds split at node‑center. Points lying exactly on a split plane are deterministically assigned but may end up in different leaves between balanced vs unbalanced trees.
  - OctTree3D partitions space into eight octants at each node. Points on plane boundaries are classified using octant rules; borderline points may be grouped differently than in KdTree3D.

- Bounds queries: half‑open vs closed edges
  - KdTree3D constructs an inclusive half‑open query box for per‑point checks and uses Unity `Bounds` for traversal. OctTree3D uses `BoundingBox3D` with inclusive‑max conversion and additional node‑level fast paths when a node is fully contained.
  - Minimum node size enforcement keeps node bounds non‑degenerate. Near boundary edges this can expand a node just enough to flip a fully‑contained check, changing whether the algorithm fast‑adds all points in a node or checks them individually.
  - Result impact: points exactly on max edges or at floating‑point limits can be included by one structure and excluded by the other in rare cases.

- Range (sphere) queries
  - Both trees use exact per‑point distance checks. However, their traversal pruning and “node fully contained in sphere” checks differ (sphere vs AABB overlap/containment and different numeric guards). For points close to the query radius, minor numeric differences can alter inclusion.

- Balanced vs unbalanced KdTree3D
  - Balanced uses median selection; unbalanced uses quick splits by node center. Both apply equivalent per‑point checks, but leaf grouping and bounding boxes differ. Near boundary edges, leaf‑level fast paths (e.g., when a node is fully contained) can diverge, leading to differences at exact boundaries.

## 3D: RTree3D Semantics

- RTree3D indexes 3D AABBs and aggregates bounding volumes upward. Queries (box/sphere) operate on those volumes rather than points. Expect results to differ from KdTree3D/OctTree3D in scenes where elements have size.

## Guidance

- Need consistent point semantics in 2D? Use QuadTree2D or KdTree2D interchangeably; choose based on performance.
- In 3D, prefer KdTree3D for nearest‑neighbor point queries and OctTree3D for general‑purpose spatial partitioning. Be mindful of edge cases on query boundaries; add small epsilons if needed.
- Use RTree2D/RTree3D for sized elements where bounds intersection is the primary concern.
- For many moving objects with broad‑phase neighbor checks, prefer SpatialHash3D (stable) or SpatialHash2D.

