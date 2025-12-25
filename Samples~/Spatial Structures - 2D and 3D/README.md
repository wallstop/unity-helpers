Spatial Structures - 2D and 3D

Demonstrates building and querying spatial structures for static point sets and grid-based indexing.

How to use

- Add `SpatialStructuresDemo` to any GameObject and press Play.
- Adjust `pointCount`, `areaSize`, and `queryRadius` in the inspector.
- Add `HullUsageDemo` to the same GameObject when you want to visualize concave hulls.
  - Assign a `Grid` component to the sample so the grid-aware example can convert tile coordinates back to world space.
  - Enter Play Mode to see cyan (gridless Vector2) and yellow (grid-aware Grid + FastVector3Int) hull loops drawn for a few seconds alongside log output describing the strategies used.
- Open the ready-to-go scene at `Scenes/HullUsageDemo.unity` if you just want to press Play (it already includes `Grid`, `HullUsageDemo`, `SpatialStructuresDemo`, camera, and light setup).

What it shows

2D Spatial Structures:

- `QuadTree2D<T>` - Recursive subdivision tree with `GetElementsInRange()` for radius queries
- `KdTree2D<T>` - k-d tree with `GetApproximateNearestNeighbors()` for fast ANN searches
- `SpatialHash2D<T>` - Grid-based hash with `Query()` for uniform distributions
- `RTree2D<T>` - R-tree with bounding box queries via `GetElementsInBounds()`

3D Spatial Structures:

- `OctTree3D<T>` - 3D octree subdivision with `GetElementsInRange()` and `GetElementsInBounds()`
- `KdTree3D<T>` - 3D k-d tree with `GetApproximateNearestNeighbors()`
- `SpatialHash3D<T>` - 3D grid hash with `Query()` and `QueryBox()`
- `RTree3D<T>` - 3D R-tree for bounding volume queries

Hull Algorithms:

- `HullUsageDemo` comparing gridless concave hull options (Vector2) with grid-aware usage (Grid + FastVector3Int) so you can pick the overload that matches your data

Example: 3D Spatial Query

```csharp
// Create an octree for 3D points
List<Vector3> points = GetWorldPositions();
Bounds worldBounds = CalculateBounds(points);
OctTree3D<Vector3> octree = new OctTree3D<Vector3>(points, p => p, worldBounds);

// Query for elements within radius
List<Vector3> nearby = new List<Vector3>();
octree.GetElementsInRange(playerPosition, detectionRadius, nearby);

// Query for elements in a bounding box
Bounds searchArea = new Bounds(center, size);
octree.GetElementsInBounds(searchArea, nearby);
```

Example: SpatialHash for Moving Objects

```csharp
// SpatialHash is ideal for frequently-updated collections
using (ISpatialHash3D<Enemy> hash = new SpatialHash3D<Enemy>(cellSize: 5f))
{
    // Insert all enemies
    foreach (Enemy enemy in enemies)
    {
        hash.Insert(enemy.Position, enemy);
    }

    // Query nearby enemies
    List<Enemy> threats = new List<Enemy>();
    hash.Query(playerPosition, alertRadius, threats);

    // Clear and rebuild each frame for moving objects
    hash.Clear();
}
```

Choosing the Right Structure

| Structure | Best For | Update Cost | Query Cost |
|-----------|----------|-------------|------------|
| QuadTree2D/OctTree3D | Static or rarely-updated point clouds | O(n) rebuild | O(log n) |
| KdTree2D/KdTree3D | Nearest-neighbor searches | O(n) rebuild | O(log n) approx |
| RTree2D/RTree3D | Bounding box queries, overlapping objects | O(log n) insert | O(log n) |
| SpatialHash2D/SpatialHash3D | Uniformly distributed, frequently updated | O(1) insert/remove | O(1) average |
