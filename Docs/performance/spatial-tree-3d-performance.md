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
| 1,000,000 entries | 3 (0.291s)          | 4 (0.200s)            | 2 (0.363s) | 3 (0.311s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 19                  | 23                    | 31        | 15      |
| Half (~span/4) (r=24.75)    | 141                 | 175                   | 229       | 144     |
| Quarter (~span/8) (r=12.38) | 1,016               | 1,359                 | 1,655     | 1,296   |
| Tiny (~span/1000) (r=1)     | 23,524              | 24,114                | 138,638   | 76,268  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 32                  | 36                    | 191       | 19      |
| Half (size≈49.50x49.50x49.50)    | 44                  | 50                    | 1,197     | 270     |
| Quarter (size≈24.75x24.75x24.75) | 45                  | 53                    | 3,619     | 2,502   |
| Unit (size=1)                    | 41                  | 53                    | 173,244   | 78,843  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,173               | 10,692                | 2,334     | 301     |
| 100 neighbors                 | 69,218              | 79,541                | 11,042    | 3,319   |
| 10 neighbors                  | 433,653             | 452,732               | 16,314    | 7,590   |
| 1 neighbor                    | 663,529             | 585,901               | 20,120    | 8,395   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D    |
| --------------- | ------------------- | --------------------- | ----------- | ---------- |
| 100,000 entries | 20 (0.048s)         | 95 (0.011s)           | 16 (0.061s) | 7 (0.128s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 387                 | 586                   | 780       | 179     |
| Half (~span/4) (r=24.75)    | 1,154               | 1,726                 | 2,072     | 854     |
| Quarter (~span/8) (r=12.38) | 2,872               | 4,686                 | 6,057     | 3,451   |
| Tiny (~span/1000) (r=1)     | 28,067              | 31,742                | 181,001   | 101,873 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 618                 | 707                   | 2,672     | 349     |
| Half (size≈49.50x49.50x4.5)     | 717                 | 843                   | 8,774     | 3,458   |
| Quarter (size≈24.75x24.75x2.25) | 728                 | 872                   | 44,334    | 24,060  |
| Unit (size=1)                   | 741                 | 886                   | 231,265   | 103,771 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,988               | 12,632                | 1,665     | 271     |
| 100 neighbors                 | 41,624              | 47,826                | 9,354     | 2,253   |
| 10 neighbors                  | 502,938             | 363,228               | 19,096    | 7,432   |
| 1 neighbor                    | 528,049             | 317,486               | 30,490    | 11,968  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 351 (0.003s)        | 759 (0.001s)          | 606 (0.002s) | 435 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 5,171               | 5,071                 | 9,546     | 1,922   |
| Half (~span/4) (r=24.75)    | 6,322               | 7,027                 | 8,977     | 4,020   |
| Quarter (~span/8) (r=12.38) | 6,349               | 7,426                 | 11,277    | 7,421   |
| Tiny (~span/1000) (r=1)     | 42,829              | 40,541                | 223,240   | 159,836 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,211               | 6,231                 | 26,434    | 3,532   |
| Half (size≈49.50x4.5x4.5)      | 6,962               | 7,063                 | 42,700    | 37,534  |
| Quarter (size≈24.75x2.25x2.25) | 7,079               | 7,190                 | 157,510   | 123,799 |
| Unit (size=1)                  | 7,046               | 7,231                 | 308,034   | 168,592 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,798               | 11,308                | 640       | 186     |
| 100 neighbors                 | 55,502              | 75,169                | 6,045     | 2,227   |
| 10 neighbors                  | 519,124             | 444,720               | 27,753    | 12,612  |
| 1 neighbor                    | 794,399             | 745,982               | 46,608    | 21,177  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,332 (0.000s)      | 7,077 (0.000s)        | 4,210 (0.000s) | 1,411 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,244              | 15,879                | 24,840    | 21,484  |
| Half (~span/4) (r=2.25)    | 55,835              | 66,291                | 125,266   | 149,799 |
| Quarter (~span/8) (r=1.13) | 65,190              | 68,205                | 349,787   | 231,455 |
| Tiny (~span/1000) (r=1)    | 65,227              | 68,049                | 349,907   | 230,146 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 57,004              | 61,674                | 326,799   | 35,420  |
| Half (size≈4.5x4.5x4.5)       | 61,387              | 67,302                | 184,452   | 180,057 |
| Quarter (size≈2.25x2.25x2.25) | 62,022              | 68,735                | 479,618   | 241,421 |
| Unit (size=1)                 | 62,019              | 69,920                | 476,235   | 242,040 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 17,027              | 15,539                | 3,243     | 615     |
| 100 neighbors                 | 75,623              | 70,230                | 15,795    | 4,067   |
| 10 neighbors                  | 475,755             | 453,743               | 76,265    | 30,651  |
| 1 neighbor                    | 749,223             | 765,587               | 85,886    | 40,317  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 37,735 (0.000s)     | 30,674 (0.000s)       | 24,390 (0.000s) | 15,948 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 141,329             | 140,277               | 305,014   | 182,053 |
| Half (~span/4) (r=2.25)    | 167,137             | 167,246               | 327,593   | 297,890 |
| Quarter (~span/8) (r=1.13) | 168,617             | 168,672               | 406,485   | 439,503 |
| Tiny (~span/1000) (r=1)    | 168,826             | 168,674               | 407,611   | 439,535 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 534,577             | 526,887               | 1,971,005 | 340,848 |
| Half (size≈4.5x2x1)     | 558,865             | 554,542               | 488,075   | 484,264 |
| Quarter (size≈2.25x1x1) | 571,907             | 562,126               | 737,894   | 797,751 |
| Unit (size=1)           | 563,952             | 563,128               | 737,765   | 798,813 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 97,832              | 95,332                | 69,263    | 69,426  |
| 10 neighbors                  | 765,455             | 612,920               | 109,557   | 91,602  |
| 1 neighbor                    | 1,049,762           | 740,538               | 182,111   | 192,772 |

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
