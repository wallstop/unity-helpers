---
---

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
| 1,000,000 entries | 3 (0.273s)          | 6 (0.162s)            | 3 (0.255s) | 2 (0.444s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 19                  | 23                    | 31        | 13      |
| Half (~span/4) (r=24.75)    | 142                 | 172                   | 212       | 109     |
| Quarter (~span/8) (r=12.38) | 992                 | 1,283                 | 1,592     | 1,355   |
| Tiny (~span/1000) (r=1)     | 23,204              | 22,739                | 135,578   | 74,223  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 31                  | 35                    | 188       | 20      |
| Half (size≈49.50x49.50x49.50)    | 40                  | 48                    | 1,257     | 254     |
| Quarter (size≈24.75x24.75x24.75) | 42                  | 51                    | 3,361     | 2,387   |
| Unit (size=1)                    | 43                  | 49                    | 166,559   | 76,043  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,769               | 9,870                 | 2,244     | 294     |
| 100 neighbors                 | 66,813              | 75,516                | 10,624    | 3,039   |
| 10 neighbors                  | 411,400             | 401,849               | 15,556    | 6,477   |
| 1 neighbor                    | 520,686             | 495,981               | 19,444    | 6,964   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 48 (0.020s)         | 27 (0.036s)           | 61 (0.016s) | 40 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 353                 | 574                   | 751       | 164     |
| Half (~span/4) (r=24.75)    | 1,069               | 1,664                 | 1,911     | 732     |
| Quarter (~span/8) (r=12.38) | 2,690               | 4,470                 | 5,793     | 3,062   |
| Tiny (~span/1000) (r=1)     | 26,554              | 29,131                | 173,109   | 98,259  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 542                 | 706                   | 2,971     | 312     |
| Half (size≈49.50x49.50x4.5)     | 686                 | 802                   | 8,653     | 3,303   |
| Quarter (size≈24.75x24.75x2.25) | 700                 | 812                   | 41,583    | 23,509  |
| Unit (size=1)                   | 704                 | 830                   | 219,434   | 100,191 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,764               | 12,107                | 1,539     | 261     |
| 100 neighbors                 | 40,273              | 44,197                | 9,076     | 2,024   |
| 10 neighbors                  | 464,220             | 308,231               | 18,534    | 6,323   |
| 1 neighbor                    | 327,866             | 346,915               | 29,545    | 9,969   |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 583 (0.002s)        | 766 (0.001s)          | 598 (0.002s) | 442 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,900               | 4,863                 | 8,944     | 1,812   |
| Half (~span/4) (r=24.75)    | 6,069               | 6,805                 | 8,714     | 3,771   |
| Quarter (~span/8) (r=12.38) | 6,206               | 7,215                 | 10,806    | 6,699   |
| Tiny (~span/1000) (r=1)     | 41,862              | 39,326                | 216,802   | 155,989 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 5,948               | 5,905                 | 29,702    | 3,455   |
| Half (size≈49.50x4.5x4.5)      | 6,697               | 6,812                 | 40,891    | 36,632  |
| Quarter (size≈24.75x2.25x2.25) | 6,846               | 6,940                 | 150,814   | 119,091 |
| Unit (size=1)                  | 6,934               | 6,996                 | 290,459   | 160,442 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,032              | 10,850                | 624       | 179     |
| 100 neighbors                 | 53,651              | 72,172                | 5,924     | 2,047   |
| 10 neighbors                  | 421,142             | 401,563               | 27,038    | 10,642  |
| 1 neighbor                    | 692,012             | 655,753               | 44,957    | 18,189  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D        |
| ------------- | ------------------- | --------------------- | ------------ | -------------- |
| 1,000 entries | 3,102 (0.000s)      | 3,487 (0.000s)        | 561 (0.002s) | 4,115 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,227              | 15,598                | 22,620    | 19,722  |
| Half (~span/4) (r=2.25)    | 54,999              | 64,386                | 120,538   | 137,404 |
| Quarter (~span/8) (r=1.13) | 64,083              | 66,383                | 331,625   | 221,778 |
| Tiny (~span/1000) (r=1)    | 62,501              | 62,588                | 331,396   | 220,368 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 56,167              | 60,611                | 308,451   | 34,526  |
| Half (size≈4.5x4.5x4.5)       | 60,499              | 66,713                | 177,818   | 174,781 |
| Quarter (size≈2.25x2.25x2.25) | 60,990              | 68,211                | 459,583   | 231,669 |
| Unit (size=1)                 | 60,841              | 69,140                | 459,533   | 229,829 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,583              | 15,104                | 3,209     | 601     |
| 100 neighbors                 | 67,663              | 69,708                | 15,499    | 3,877   |
| 10 neighbors                  | 474,345             | 425,012               | 73,900    | 27,066  |
| 1 neighbor                    | 704,067             | 703,187               | 83,897    | 34,717  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 38,167 (0.000s)     | 37,453 (0.000s)       | 23,640 (0.000s) | 20,790 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 134,470             | 138,075               | 290,527   | 170,607 |
| Half (~span/4) (r=2.25)    | 162,408             | 163,979               | 309,715   | 284,532 |
| Quarter (~span/8) (r=1.13) | 163,520             | 164,973               | 384,706   | 410,361 |
| Tiny (~span/1000) (r=1)    | 162,723             | 165,329               | 385,821   | 407,196 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 513,720             | 509,046               | 1,716,771 | 323,753 |
| Half (size≈4.5x2x1)     | 532,670             | 540,182               | 428,013   | 454,299 |
| Quarter (size≈2.25x1x1) | 534,737             | 544,166               | 702,190   | 705,538 |
| Unit (size=1)           | 525,382             | 543,896               | 703,827   | 683,906 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 87,177              | 91,691                | 68,064    | 67,651  |
| 10 neighbors                  | 654,031             | 482,474               | 106,281   | 86,265  |
| 1 neighbor                    | 867,390             | 708,030               | 172,794   | 175,012 |

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
