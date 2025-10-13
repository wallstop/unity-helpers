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
| 1,000,000 entries | 1 (0.508s)          | 3 (0.304s)            | 4 (0.241s) | 1 (0.737s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 32        | 15      |
| Half (~span/4) (r=24.75)    | 133                 | 162                   | 245       | 167     |
| Quarter (~span/8) (r=12.38) | 924                 | 1,215                 | 1,668     | 1,516   |
| Tiny (~span/1000) (r=1)     | 23,192              | 23,394                | 137,947   | 75,463  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 34                  | 39                    | 210       | 20      |
| Half (size≈49.50x49.50x49.50)    | 39                  | 45                    | 1,295     | 277     |
| Quarter (size≈24.75x24.75x24.75) | 39                  | 47                    | 3,977     | 2,508   |
| Unit (size=1)                    | 39                  | 47                    | 180,603   | 76,876  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,838               | 10,192                | 2,297     | 306     |
| 100 neighbors                 | 64,970              | 74,311                | 10,846    | 3,390   |
| 10 neighbors                  | 407,029             | 420,234               | 15,963    | 7,817   |
| 1 neighbor                    | 524,410             | 460,319               | 19,781    | 8,325   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 46 (0.022s)           | 63 (0.016s) | 43 (0.023s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 334                 | 483                   | 772       | 212     |
| Half (~span/4) (r=24.75)    | 1,010               | 1,436                 | 2,027     | 851     |
| Quarter (~span/8) (r=12.38) | 2,549               | 3,971                 | 5,974     | 3,427   |
| Tiny (~span/1000) (r=1)     | 26,431              | 29,461                | 176,120   | 100,009 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 633                 | 763                   | 2,698     | 352     |
| Half (size≈49.50x49.50x4.5)     | 731                 | 896                   | 9,274     | 3,479   |
| Quarter (size≈24.75x24.75x2.25) | 744                 | 922                   | 47,175    | 24,173  |
| Unit (size=1)                   | 747                 | 932                   | 242,902   | 102,272 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,663               | 11,970                | 1,625     | 272     |
| 100 neighbors                 | 38,566              | 43,933                | 9,229     | 2,270   |
| 10 neighbors                  | 441,954             | 327,003               | 18,991    | 7,712   |
| 1 neighbor                    | 424,183             | 297,081               | 29,888    | 12,033  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 447 (0.002s)        | 272 (0.004s)          | 601 (0.002s) | 427 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,748               | 4,578                 | 9,034     | 2,064   |
| Half (~span/4) (r=24.75)    | 5,907               | 6,440                 | 8,855     | 4,269   |
| Quarter (~span/8) (r=12.38) | 5,868               | 6,456                 | 11,150    | 7,507   |
| Tiny (~span/1000) (r=1)     | 40,827              | 38,708                | 220,189   | 157,694 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,416               | 6,427                 | 26,247    | 3,579   |
| Half (size≈49.50x4.5x4.5)      | 7,236               | 7,279                 | 45,827    | 37,451  |
| Quarter (size≈24.75x2.25x2.25) | 7,394               | 7,453                 | 167,260   | 120,971 |
| Unit (size=1)                  | 7,491               | 7,509                 | 321,908   | 163,490 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,685               | 10,626                | 636       | 186     |
| 100 neighbors                 | 51,212              | 68,755                | 5,907     | 2,255   |
| 10 neighbors                  | 417,879             | 449,838               | 26,902    | 12,870  |
| 1 neighbor                    | 660,089             | 564,985               | 44,974    | 21,929  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,551 (0.000s)      | 3,597 (0.000s)        | 3,963 (0.000s) | 3,951 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 11,889              | 13,977                | 24,840    | 21,558  |
| Half (~span/4) (r=2.25)    | 52,048              | 61,813                | 123,986   | 147,568 |
| Quarter (~span/8) (r=1.13) | 62,056              | 63,090                | 338,282   | 222,311 |
| Tiny (~span/1000) (r=1)    | 62,061              | 60,803                | 338,028   | 220,654 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 58,850              | 63,749                | 320,982   | 35,389  |
| Half (size≈4.5x4.5x4.5)       | 64,139              | 70,415                | 196,205   | 168,950 |
| Quarter (size≈2.25x2.25x2.25) | 64,869              | 72,077                | 496,709   | 236,781 |
| Unit (size=1)                 | 64,865              | 73,115                | 497,719   | 237,572 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,543              | 14,687                | 3,259     | 622     |
| 100 neighbors                 | 70,021              | 66,063                | 15,576    | 4,154   |
| 10 neighbors                  | 423,735             | 416,232               | 74,338    | 33,030  |
| 1 neighbor                    | 691,738             | 632,114               | 84,636    | 44,251  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 36,363 (0.000s)     | 32,467 (0.000s)       | 22,522 (0.000s) | 17,985 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 125,428             | 126,400               | 292,761   | 190,037 |
| Half (~span/4) (r=2.25)    | 145,842             | 147,953               | 315,761   | 306,447 |
| Quarter (~span/8) (r=1.13) | 146,012             | 149,154               | 390,179   | 416,622 |
| Tiny (~span/1000) (r=1)    | 146,038             | 149,179               | 392,548   | 419,108 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 529,034             | 526,832               | 1,832,248 | 326,622 |
| Half (size≈4.5x2x1)     | 558,166             | 565,767               | 513,823   | 462,243 |
| Quarter (size≈2.25x1x1) | 572,641             | 569,761               | 767,483   | 673,328 |
| Unit (size=1)           | 572,991             | 570,221               | 767,909   | 734,376 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 92,047              | 88,429                | 66,597    | 65,937  |
| 10 neighbors                  | 582,351             | 493,565               | 95,528    | 101,563 |
| 1 neighbor                    | 805,526             | 608,704               | 170,178   | 224,276 |

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
