# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KDTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](SPATIAL_TREE_SEMANTICS.md)

This document contains performance benchmarks for the 3D spatial tree implementations in Unity Helpers.

## Available 3D Spatial Trees

- **OctTree3D** - Easiest to use, good all-around performance for 3D
- **KDTree3D** - Balanced and unbalanced variants available
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
| 1,000,000 entries | 2 (0.402s)          | 3 (0.313s)            | 2 (0.430s) | 2 (0.373s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 31        | 15      |
| Half (~span/4) (r=24.75)    | 127                 | 153                   | 215       | 150     |
| Quarter (~span/8) (r=12.38) | 934                 | 1,227                 | 1,669     | 1,514   |
| Tiny (~span/1000) (r=1)     | 23,479              | 23,665                | 138,186   | 76,631  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 33                  | 35                    | 176       | 20      |
| Half (size≈49.50x49.50x49.50)    | 38                  | 41                    | 1,247     | 262     |
| Quarter (size≈24.75x24.75x24.75) | 38                  | 43                    | 3,959     | 2,520   |
| Unit (size=1)                    | 39                  | 43                    | 183,582   | 76,235  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,895               | 10,193                | 2,302     | 305     |
| 100 neighbors                 | 65,079              | 70,085                | 10,904    | 3,349   |
| 10 neighbors                  | 403,144             | 416,692               | 16,000    | 7,663   |
| 1 neighbor                    | 544,211             | 412,407               | 19,831    | 8,321   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 47 (0.021s)           | 64 (0.015s) | 43 (0.023s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 336                 | 485                   | 770       | 199     |
| Half (~span/4) (r=24.75)    | 1,019               | 1,445                 | 2,040     | 853     |
| Quarter (~span/8) (r=12.38) | 2,567               | 3,993                 | 5,989     | 3,443   |
| Tiny (~span/1000) (r=1)     | 26,745              | 29,804                | 176,265   | 101,035 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 572                 | 718                   | 2,636     | 324     |
| Half (size≈49.50x49.50x4.5)     | 662                 | 837                   | 9,370     | 3,498   |
| Quarter (size≈24.75x24.75x2.25) | 674                 | 861                   | 47,400    | 24,236  |
| Unit (size=1)                   | 676                 | 872                   | 246,015   | 101,313 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,708               | 11,996                | 1,629     | 271     |
| 100 neighbors                 | 38,469              | 42,192                | 9,227     | 2,237   |
| 10 neighbors                  | 420,942             | 218,972               | 19,122    | 7,481   |
| 1 neighbor                    | 459,844             | 327,816               | 29,943    | 11,842  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 378 (0.003s)        | 467 (0.002s)          | 585 (0.002s) | 442 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,775               | 4,609                 | 8,940     | 2,207   |
| Half (~span/4) (r=24.75)    | 5,940               | 6,484                 | 8,824     | 4,285   |
| Quarter (~span/8) (r=12.38) | 5,911               | 6,909                 | 11,123    | 7,540   |
| Tiny (~span/1000) (r=1)     | 41,201              | 39,120                | 218,463   | 160,263 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 5,944               | 5,974                 | 26,136    | 3,587   |
| Half (size≈49.50x4.5x4.5)      | 6,709               | 6,761                 | 46,124    | 37,467  |
| Quarter (size≈24.75x2.25x2.25) | 6,843               | 6,878                 | 167,117   | 120,365 |
| Unit (size=1)                  | 6,935               | 6,923                 | 313,373   | 161,932 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,797               | 10,570                | 636       | 185     |
| 100 neighbors                 | 48,860              | 69,836                | 5,909     | 2,233   |
| 10 neighbors                  | 463,086             | 416,036               | 26,962    | 12,808  |
| 1 neighbor                    | 622,715             | 624,381               | 45,121    | 21,542  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,828 (0.000s)      | 1,431 (0.001s)        | 4,177 (0.000s) | 4,058 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,017              | 13,682                | 24,546    | 21,261  |
| Half (~span/4) (r=2.25)    | 52,394              | 62,385                | 124,579   | 149,723 |
| Quarter (~span/8) (r=1.13) | 62,668              | 64,358                | 339,716   | 228,054 |
| Tiny (~span/1000) (r=1)    | 62,670              | 64,483                | 340,166   | 228,438 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 54,554              | 59,700                | 321,423   | 35,729  |
| Half (size≈4.5x4.5x4.5)       | 59,218              | 65,748                | 199,393   | 177,785 |
| Quarter (size≈2.25x2.25x2.25) | 59,744              | 67,219                | 501,086   | 234,106 |
| Unit (size=1)                 | 59,712              | 68,104                | 475,751   | 234,738 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,553              | 14,623                | 3,259     | 621     |
| 100 neighbors                 | 70,256              | 65,840                | 15,632    | 4,130   |
| 10 neighbors                  | 439,459             | 413,712               | 74,432    | 32,538  |
| 1 neighbor                    | 691,682             | 657,381               | 84,606    | 43,414  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 39,370 (0.000s)     | 33,003 (0.000s)       | 26,246 (0.000s) | 20,746 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 126,263             | 127,269               | 296,006   | 195,890 |
| Half (~span/4) (r=2.25)    | 147,197             | 149,386               | 318,618   | 311,821 |
| Quarter (~span/8) (r=1.13) | 147,516             | 150,720               | 393,623   | 420,109 |
| Tiny (~span/1000) (r=1)    | 147,471             | 150,688               | 395,500   | 405,202 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 501,997             | 505,246               | 1,807,410 | 329,640 |
| Half (size≈4.5x2x1)     | 498,779             | 534,318               | 494,122   | 456,745 |
| Quarter (size≈2.25x1x1) | 537,627             | 536,488               | 776,653   | 726,561 |
| Unit (size=1)           | 539,253             | 536,541               | 776,990   | 721,307 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 92,014              | 88,119                | 69,346    | 62,009  |
| 10 neighbors                  | 617,049             | 514,543               | 105,140   | 100,817 |
| 1 neighbor                    | 872,834             | 636,275               | 172,544   | 221,636 |

<!-- tabs:end -->
<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**OctTree3D**:

- Best for: General-purpose 3D spatial queries
- Strengths: Balanced performance, easy to use, good spatial locality
- Use cases: 3D collision detection, visibility culling, spatial audio

**KDTree3D (Balanced)**:

- Best for: Nearest-neighbor queries in 3D space
- Strengths: Fast point queries, good for smaller datasets
- Use cases: Pathfinding, AI spatial awareness, particle systems

**KDTree3D (Unbalanced)**:

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
