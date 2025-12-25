# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KdTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](../features/spatial/spatial-tree-semantics.md)

This document contains performance benchmarks for the 3D spatial tree implementations in Unity Helpers.

## Available 3D Spatial Trees

- **OctTree3D** - Easiest to use, good all-around performance for 3D
- **KdTree3D** - Balanced and unbalanced variants available
- **RTree3D** - Optimized for 3D bounding box queries
- **SpatialHash3D** - Efficient for uniformly distributed moving objects (stable)

## Performance Benchmarks

<!-- SPATIAL_TREE_3D_BENCHMARKS_START -->

### Datasets

<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

| Construction      | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D  | RTree3D    |
| ----------------- | ------------------- | --------------------- | ---------- | ---------- |
| 1,000,000 entries | 3 (0.269s)          | 6 (0.155s)            | 3 (0.308s) | 2 (0.418s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 19                  | 23                    | 32        | 15      |
| Half (~span/4) (r=24.75)    | 149                 | 180                   | 210       | 135     |
| Quarter (~span/8) (r=12.38) | 1,030               | 1,332                 | 1,638     | 1,445   |
| Tiny (~span/1000) (r=1)     | 23,774              | 23,816                | 137,762   | 75,685  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 34                  | 35                    | 193       | 22      |
| Half (size≈49.50x49.50x49.50)    | 43                  | 52                    | 1,245     | 266     |
| Quarter (size≈24.75x24.75x24.75) | 45                  | 55                    | 3,452     | 2,511   |
| Unit (size=1)                    | 46                  | 54                    | 169,635   | 77,689  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,107               | 10,710                | 2,252     | 297     |
| 100 neighbors                 | 69,072              | 78,289                | 10,709    | 3,312   |
| 10 neighbors                  | 437,378             | 452,647               | 15,711    | 7,622   |
| 1 neighbor                    | 593,842             | 562,726               | 19,345    | 8,251   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D    |
| --------------- | ------------------- | --------------------- | ----------- | ---------- |
| 100,000 entries | 48 (0.021s)         | 95 (0.011s)           | 12 (0.082s) | 6 (0.151s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 373                 | 580                   | 783       | 196     |
| Half (~span/4) (r=24.75)    | 1,082               | 1,702                 | 1,960     | 808     |
| Quarter (~span/8) (r=12.38) | 2,772               | 4,650                 | 5,817     | 3,322   |
| Tiny (~span/1000) (r=1)     | 27,349              | 31,613                | 176,282   | 100,663 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 589                 | 698                   | 2,833     | 321     |
| Half (size≈49.50x49.50x4.5)     | 679                 | 834                   | 8,844     | 3,367   |
| Quarter (size≈24.75x24.75x2.25) | 683                 | 857                   | 42,985    | 24,354  |
| Unit (size=1)                   | 713                 | 865                   | 228,448   | 103,917 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,823               | 12,470                | 1,630     | 267     |
| 100 neighbors                 | 41,253              | 47,129                | 9,202     | 2,195   |
| 10 neighbors                  | 502,433             | 360,087               | 18,642    | 7,363   |
| 1 neighbor                    | 527,709             | 273,432               | 29,339    | 11,833  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 600 (0.002s)        | 754 (0.001s)          | 597 (0.002s) | 430 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 5,131               | 5,019                 | 9,051     | 2,124   |
| Half (~span/4) (r=24.75)    | 6,304               | 6,946                 | 8,769     | 4,114   |
| Quarter (~span/8) (r=12.38) | 6,234               | 7,312                 | 10,759    | 7,316   |
| Tiny (~span/1000) (r=1)     | 42,470              | 39,916                | 220,226   | 160,335 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,060               | 6,021                 | 29,196    | 3,507   |
| Half (size≈49.50x4.5x4.5)      | 6,824               | 6,850                 | 42,320    | 37,311  |
| Quarter (size≈24.75x2.25x2.25) | 6,918               | 6,880                 | 155,416   | 122,336 |
| Unit (size=1)                  | 6,966               | 7,050                 | 302,516   | 165,954 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,996               | 11,038                | 629       | 182     |
| 100 neighbors                 | 54,641              | 73,602                | 5,934     | 2,187   |
| 10 neighbors                  | 490,904             | 437,377               | 26,862    | 12,465  |
| 1 neighbor                    | 772,986             | 731,539               | 45,041    | 21,385  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,068 (0.000s)      | 7,457 (0.000s)        | 4,240 (0.000s) | 4,074 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,145              | 15,776                | 24,463    | 21,078  |
| Half (~span/4) (r=2.25)    | 55,684              | 66,328                | 123,432   | 150,527 |
| Quarter (~span/8) (r=1.13) | 65,065              | 67,697                | 341,565   | 229,826 |
| Tiny (~span/1000) (r=1)    | 65,000              | 67,595                | 342,480   | 230,735 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 56,630              | 61,188                | 315,610   | 35,638  |
| Half (size≈4.5x4.5x4.5)       | 61,001              | 67,303                | 182,362   | 182,315 |
| Quarter (size≈2.25x2.25x2.25) | 61,622              | 68,707                | 475,119   | 242,496 |
| Unit (size=1)                 | 61,757              | 69,701                | 478,002   | 242,680 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,679              | 15,223                | 3,187     | 608     |
| 100 neighbors                 | 72,893              | 70,478                | 15,611    | 3,971   |
| 10 neighbors                  | 513,618             | 438,764               | 74,696    | 30,475  |
| 1 neighbor                    | 755,405             | 763,826               | 85,801    | 40,266  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 41,152 (0.000s)     | 38,314 (0.000s)       | 26,525 (0.000s) | 22,075 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 139,004             | 140,546               | 297,016   | 195,452 |
| Half (~span/4) (r=2.25)    | 165,229             | 164,780               | 317,558   | 310,191 |
| Quarter (~span/8) (r=1.13) | 165,651             | 168,237               | 397,217   | 437,336 |
| Tiny (~span/1000) (r=1)    | 164,679             | 168,136               | 399,584   | 441,812 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 525,234             | 519,711               | 1,900,096 | 342,234 |
| Half (size≈4.5x2x1)     | 549,787             | 551,979               | 480,595   | 486,938 |
| Quarter (size≈2.25x1x1) | 566,594             | 561,211               | 735,512   | 797,552 |
| Unit (size=1)           | 566,224             | 557,144               | 734,310   | 797,254 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 96,672              | 90,915                | 68,630    | 69,167  |
| 10 neighbors                  | 766,056             | 603,986               | 107,741   | 89,263  |
| 1 neighbor                    | 1,058,115           | 734,862               | 178,690   | 190,589 |

<!-- tabs:end -->
<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**OctTree3D**:

- Best for: General-purpose 3D spatial queries
- Strengths: Balanced performance, easy to use, good spatial locality
- Use cases: 3D collision detection, visibility culling, spatial audio

**KdTree3D (Balanced)**:

- Best for: Nearest-neighbor queries in 3D space
- Strengths: Fast point queries, good for smaller datasets
- Use cases: Pathfinding, AI spatial awareness, particle systems

**KdTree3D (Unbalanced)**:

- Best for: When you need fast construction and will rebuild frequently
- Strengths: Fastest construction, similar query performance to balanced
- Use cases: Dynamic environments, frequently changing spatial data

**RTree3D**:

- Best for: 3D bounding box queries, especially with volumetric data
- Strengths: Excellent for large bounding volumes, handles overlapping objects
- Use cases: Physics engines, frustum culling, volumetric effects

### Important Notes

- All spatial trees assume **immutable** positional data
- If positions change, you must reconstruct the tree
- Spatial queries are O(log n) vs O(n) for linear search
- 3D trees have higher construction costs than 2D variants due to additional dimension
- Construction cost is amortized over many queries
