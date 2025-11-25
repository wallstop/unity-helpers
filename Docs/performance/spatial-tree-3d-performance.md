# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KDTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](../features/spatial/spatial-tree-semantics.md)

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
| 1,000,000 entries | 3 (0.271s)          | 4 (0.218s)            | 3 (0.253s) | 1 (0.792s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 19                  | 23                    | 30        | 14      |
| Half (~span/4) (r=24.75)    | 134                 | 159                   | 185       | 105     |
| Quarter (~span/8) (r=12.38) | 1,021               | 1,326                 | 1,627     | 1,465   |
| Tiny (~span/1000) (r=1)     | 23,678              | 23,550                | 136,139   | 70,299  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 27                  | 31                    | 143       | 16      |
| Half (size≈49.50x49.50x49.50)    | 40                  | 45                    | 1,183     | 146     |
| Quarter (size≈24.75x24.75x24.75) | 37                  | 43                    | 4,007     | 2,450   |
| Unit (size=1)                    | 36                  | 44                    | 170,840   | 75,861  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,047               | 10,481                | 2,131     | 301     |
| 100 neighbors                 | 61,356              | 75,756                | 9,624     | 3,259   |
| 10 neighbors                  | 411,501             | 414,102               | 14,047    | 7,156   |
| 1 neighbor                    | 568,311             | 474,117               | 17,513    | 7,602   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 46 (0.021s)         | 94 (0.011s)           | 63 (0.016s) | 33 (0.029s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 344                 | 566                   | 745       | 142     |
| Half (~span/4) (r=24.75)    | 1,096               | 1,680                 | 1,994     | 839     |
| Quarter (~span/8) (r=12.38) | 2,713               | 4,573                 | 5,954     | 3,368   |
| Tiny (~span/1000) (r=1)     | 27,423              | 30,371                | 173,723   | 94,894  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 343                 | 614                   | 2,566     | 336     |
| Half (size≈49.50x49.50x4.5)     | 615                 | 783                   | 9,164     | 3,422   |
| Quarter (size≈24.75x24.75x2.25) | 665                 | 805                   | 45,449    | 23,820  |
| Unit (size=1)                   | 719                 | 858                   | 238,810   | 103,073 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,980               | 12,523                | 1,546     | 267     |
| 100 neighbors                 | 41,297              | 46,310                | 8,425     | 2,131   |
| 10 neighbors                  | 421,473             | 294,748               | 17,002    | 6,986   |
| 1 neighbor                    | 487,838             | 344,532               | 27,245    | 10,915  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 590 (0.002s)        | 754 (0.001s)          | 597 (0.002s) | 451 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,849               | 4,867                 | 9,325     | 2,123   |
| Half (~span/4) (r=24.75)    | 6,093               | 6,814                 | 8,990     | 4,138   |
| Quarter (~span/8) (r=12.38) | 6,151               | 7,101                 | 11,007    | 7,168   |
| Tiny (~span/1000) (r=1)     | 41,496              | 39,594                | 215,163   | 148,838 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,090               | 6,127                 | 26,093    | 3,520   |
| Half (size≈49.50x4.5x4.5)      | 6,848               | 7,040                 | 44,493    | 36,676  |
| Quarter (size≈24.75x2.25x2.25) | 6,963               | 7,124                 | 164,225   | 118,346 |
| Unit (size=1)                  | 6,967               | 7,204                 | 318,979   | 161,712 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,190              | 11,162                | 611       | 180     |
| 100 neighbors                 | 52,637              | 74,028                | 5,463     | 2,135   |
| 10 neighbors                  | 440,113             | 465,325               | 24,392    | 11,817  |
| 1 neighbor                    | 630,679             | 598,911               | 41,133    | 20,084  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,580 (0.000s)      | 6,476 (0.000s)        | 4,118 (0.000s) | 3,982 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,158              | 15,570                | 24,040    | 20,948  |
| Half (~span/4) (r=2.25)    | 54,480              | 65,675                | 122,685   | 141,641 |
| Quarter (~span/8) (r=1.13) | 64,470              | 68,107                | 334,067   | 214,492 |
| Tiny (~span/1000) (r=1)    | 65,285              | 67,773                | 331,608   | 213,034 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 56,438              | 61,867                | 304,860   | 34,641  |
| Half (size≈4.5x4.5x4.5)       | 60,498              | 68,072                | 193,447   | 173,577 |
| Quarter (size≈2.25x2.25x2.25) | 61,264              | 69,417                | 497,258   | 229,933 |
| Unit (size=1)                 | 61,243              | 69,530                | 488,006   | 229,285 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 16,170              | 15,381                | 2,956     | 617     |
| 100 neighbors                 | 73,783              | 67,710                | 14,647    | 4,077   |
| 10 neighbors                  | 409,321             | 403,568               | 67,611    | 30,274  |
| 1 neighbor                    | 714,007             | 677,782               | 76,845    | 40,417  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 33,333 (0.000s)     | 19,342 (0.000s)       | 24,691 (0.000s) | 19,493 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 136,987             | 134,248               | 294,618   | 190,875 |
| Half (~span/4) (r=2.25)    | 162,229             | 152,948               | 310,368   | 297,747 |
| Quarter (~span/8) (r=1.13) | 154,789             | 164,513               | 381,813   | 402,359 |
| Tiny (~span/1000) (r=1)    | 165,232             | 166,572               | 384,841   | 373,231 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 510,929             | 522,792               | 1,629,137 | 316,758 |
| Half (size≈4.5x2x1)     | 532,127             | 552,034               | 513,011   | 447,969 |
| Quarter (size≈2.25x1x1) | 541,795             | 552,438               | 753,295   | 715,455 |
| Unit (size=1)           | 540,860             | 553,239               | 749,785   | 703,658 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 95,723              | 94,193                | 64,417    | 63,716  |
| 10 neighbors                  | 589,809             | 518,000               | 96,514    | 99,964  |
| 1 neighbor                    | 935,393             | 732,789               | 161,032   | 209,003 |

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
