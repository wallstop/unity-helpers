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
| 1,000,000 entries | 2 (0.482s)          | 3 (0.327s)            | 4 (0.249s) | 2 (0.355s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 32        | 14      |
| Half (~span/4) (r=24.75)    | 122                 | 137                   | 190       | 126     |
| Quarter (~span/8) (r=12.38) | 935                 | 1,214                 | 1,661     | 1,394   |
| Tiny (~span/1000) (r=1)     | 23,475              | 23,643                | 137,942   | 61,290  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 32                  | 36                    | 155       | 22      |
| Half (size≈49.50x49.50x49.50)    | 38                  | 44                    | 1,266     | 230     |
| Quarter (size≈24.75x24.75x24.75) | 38                  | 45                    | 4,014     | 2,508   |
| Unit (size=1)                    | 39                  | 46                    | 178,283   | 77,655  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,930               | 10,142                | 2,274     | 305     |
| 100 neighbors                 | 65,339              | 74,219                | 10,789    | 3,368   |
| 10 neighbors                  | 407,944             | 418,104               | 15,750    | 7,776   |
| 1 neighbor                    | 548,844             | 479,717               | 19,606    | 8,462   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 14 (0.069s)         | 47 (0.021s)           | 64 (0.015s) | 10 (0.099s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 301                 | 457                   | 758       | 165     |
| Half (~span/4) (r=24.75)    | 1,004               | 1,443                 | 2,025     | 798     |
| Quarter (~span/8) (r=12.38) | 2,556               | 3,985                 | 5,985     | 3,199   |
| Tiny (~span/1000) (r=1)     | 26,614              | 29,395                | 176,682   | 82,564  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 612                 | 733                   | 2,629     | 331     |
| Half (size≈49.50x49.50x4.5)     | 738                 | 894                   | 9,353     | 3,498   |
| Quarter (size≈24.75x24.75x2.25) | 752                 | 912                   | 47,164    | 24,211  |
| Unit (size=1)                   | 758                 | 917                   | 242,422   | 103,124 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,704               | 11,954                | 1,605     | 272     |
| 100 neighbors                 | 36,631              | 43,254                | 9,189     | 2,263   |
| 10 neighbors                  | 357,128             | 315,656               | 19,110    | 7,686   |
| 1 neighbor                    | 466,360             | 333,430               | 29,715    | 12,204  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 434 (0.002s)        | 454 (0.002s)          | 592 (0.002s) | 433 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,727               | 4,601                 | 8,936     | 2,000   |
| Half (~span/4) (r=24.75)    | 5,877               | 6,481                 | 8,834     | 4,019   |
| Quarter (~span/8) (r=12.38) | 5,865               | 6,923                 | 11,068    | 6,925   |
| Tiny (~span/1000) (r=1)     | 40,895              | 38,736                | 221,423   | 132,546 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,441               | 6,445                 | 26,197    | 3,602   |
| Half (size≈49.50x4.5x4.5)      | 7,375               | 7,418                 | 45,803    | 37,714  |
| Quarter (size≈24.75x2.25x2.25) | 7,517               | 7,588                 | 166,485   | 122,366 |
| Unit (size=1)                  | 7,610               | 7,636                 | 317,153   | 164,795 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,789               | 10,637                | 625       | 185     |
| 100 neighbors                 | 51,083              | 69,844                | 5,860     | 2,256   |
| 10 neighbors                  | 463,740             | 422,128               | 26,061    | 13,193  |
| 1 neighbor                    | 607,938             | 630,252               | 45,660    | 22,246  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,575 (0.000s)      | 4,977 (0.000s)        | 4,187 (0.000s) | 4,042 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 11,988              | 14,137                | 24,884    | 20,333  |
| Half (~span/4) (r=2.25)    | 52,268              | 62,507                | 124,554   | 128,621 |
| Quarter (~span/8) (r=1.13) | 62,648              | 64,098                | 342,588   | 196,683 |
| Tiny (~span/1000) (r=1)    | 62,653              | 64,488                | 342,574   | 196,522 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 59,379              | 64,521                | 322,514   | 35,998  |
| Half (size≈4.5x4.5x4.5)       | 65,338              | 72,004                | 197,610   | 178,058 |
| Quarter (size≈2.25x2.25x2.25) | 66,045              | 73,532                | 499,910   | 222,871 |
| Unit (size=1)                 | 66,065              | 74,597                | 500,149   | 237,989 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,517              | 14,702                | 3,285     | 621     |
| 100 neighbors                 | 70,131              | 66,004                | 15,721    | 4,148   |
| 10 neighbors                  | 469,591             | 390,744               | 75,134    | 33,304  |
| 1 neighbor                    | 682,037             | 653,100               | 85,578    | 44,528  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 33,333 (0.000s)     | 32,679 (0.000s)       | 27,700 (0.000s) | 20,703 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 126,257             | 127,169               | 297,352   | 187,284 |
| Half (~span/4) (r=2.25)    | 147,151             | 149,308               | 318,992   | 291,714 |
| Quarter (~span/8) (r=1.13) | 147,508             | 150,698               | 393,687   | 384,360 |
| Tiny (~span/1000) (r=1)    | 147,512             | 150,803               | 396,144   | 353,871 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 537,312             | 539,172               | 1,774,769 | 330,855 |
| Half (size≈4.5x2x1)     | 567,901             | 576,541               | 487,364   | 462,490 |
| Quarter (size≈2.25x1x1) | 583,926             | 581,021               | 765,988   | 732,907 |
| Unit (size=1)           | 583,801             | 574,660               | 766,104   | 734,376 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 91,835              | 84,026                | 69,710    | 65,852  |
| 10 neighbors                  | 612,704             | 560,243               | 105,922   | 100,006 |
| 1 neighbor                    | 880,984             | 640,366               | 175,426   | 210,918 |

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
