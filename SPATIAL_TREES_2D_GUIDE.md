# 2D Spatial Trees — Concepts and Usage

This practical guide complements performance and semantics pages with diagrams and actionable selection advice.

## Structures

### QuadTree2D

- Partition: Recursively splits space into four quadrants.
- Use for: Broad-phase proximity, view culling, general spatial bucketing.
- Pros: Simple structure; predictable performance; incremental updates straightforward.
- Cons: Data hotspots deepen local trees; nearest neighbors slower than KDTree.

Diagram: ![QuadTree2D](Docs/Images/quadtree_2d.svg)

### KDTree2D

- Partition: Alternating axis-aligned splits (x/y), often median-balanced.
- Use for: Nearest neighbor, k-NN, range queries on points.
- Pros: Strong NN performance; balanced variant gives consistent query time.
- Cons: Costly to maintain under heavy churn; unbalanced variant can degrade.

Diagram: ![KDTree2D](Docs/Images/kdtree_2d.svg)

### RTree2D

- Partition: Groups items by minimum bounding rectangles (MBRs) with hierarchical MBRs.
- Use for: Items with size (AABBs): sprites, tiles, colliders; bounds intersection.
- Pros: Great for large bounds queries; matches bounds semantics.
- Cons: Overlapping MBRs can increase node visits; not optimal for point NN.

Diagram: ![RTree2D](Docs/Images/rtree_2d.svg)

## Choosing a Structure

- Many moving points, rebuild or frequent updates: QuadTree2D
- Nearest neighbors on static points: KDTree2D (Balanced)
- Fast builds with good-enough queries: KDTree2D (Unbalanced)
- Objects with area; bounds queries primary: RTree2D

## Query Semantics

- Points vs Bounds: QuadTree2D and KDTree2D are point-based; RTree2D is bounds-based.
- Boundary inclusion: normalize half-open vs closed intervals. Add epsilons for edge cases.
- Numeric stability: prefer consistent ordering for colinear and boundary points.

