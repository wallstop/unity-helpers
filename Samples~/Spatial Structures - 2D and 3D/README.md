Spatial Structures – 2D and 3D

Demonstrates building and querying spatial structures for static point sets and grid-based indexing.

How to use

- Add `SpatialStructuresDemo` to any GameObject and press Play.
- Adjust `pointCount`, `areaSize`, and `queryRadius` in the inspector.
- Add `HullUsageDemo` to the same GameObject when you want to visualize concave hulls.
  - Assign a `Grid` component to the sample so the grid-aware example can convert tile coordinates back to world space.
  - Enter Play Mode to see cyan (gridless Vector2) and yellow (grid-aware Grid + FastVector3Int) hull loops drawn for a few seconds alongside log output describing the strategies used.
- Open the ready-to-go scene at `Scenes/HullUsageDemo.unity` if you just want to press Play (it already includes `Grid`, `HullUsageDemo`, `SpatialStructuresDemo`, camera, and light setup).
  ![Image placeholder: HullUsageDemo scene view with cyan and yellow hull loops drawn in Play Mode]
  ![GIF placeholder: Cyan/yellow hull loops animating in Play Mode, showing toggling between gridless and grid-aware examples]

What it shows

- `QuadTree2D<T>` created from points with a transformer and radius query.
- `KdTree2D<T>` approximate nearest neighbors.
- `SpatialHash2D<T>` bucketed insertion and radius query.
- `HullUsageDemo` comparing gridless concave hull options (Vector2) with grid-aware usage (Grid + FastVector3Int) so you can pick the overload that matches your data.
