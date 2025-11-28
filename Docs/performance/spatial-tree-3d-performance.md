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
| 1,000,000 entries | 3 (0.316s)          | 6 (0.155s)            | 4 (0.248s) | 1 (0.575s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 22                    | 31        | 13      |
| Half (~span/4) (r=24.75)    | 122                 | 168                   | 191       | 115     |
| Quarter (~span/8) (r=12.38) | 1,007               | 1,321                 | 1,634     | 1,368   |
| Tiny (~span/1000) (r=1)     | 23,482              | 23,365                | 138,007   | 75,466  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 32                  | 28                    | 151       | 18      |
| Half (size≈49.50x49.50x49.50)    | 43                  | 42                    | 1,068     | 206     |
| Quarter (size≈24.75x24.75x24.75) | 43                  | 45                    | 3,619     | 2,438   |
| Unit (size=1)                    | 44                  | 48                    | 164,659   | 73,957  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,237               | 10,754                | 2,197     | 299     |
| 100 neighbors                 | 69,563              | 78,207                | 10,328    | 3,240   |
| 10 neighbors                  | 443,230             | 438,246               | 15,415    | 7,290   |
| 1 neighbor                    | 624,543             | 572,119               | 19,430    | 7,925   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 47 (0.021s)         | 81 (0.012s)           | 63 (0.016s) | 41 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 354                 | 514                   | 758       | 178     |
| Half (~span/4) (r=24.75)    | 1,118               | 1,612                 | 1,973     | 826     |
| Quarter (~span/8) (r=12.38) | 2,801               | 4,502                 | 5,713     | 3,415   |
| Tiny (~span/1000) (r=1)     | 27,299              | 31,283                | 172,250   | 100,124 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 555                 | 694                   | 2,562     | 278     |
| Half (size≈49.50x49.50x4.5)     | 684                 | 829                   | 8,784     | 3,357   |
| Quarter (size≈24.75x24.75x2.25) | 700                 | 847                   | 43,640    | 23,211  |
| Unit (size=1)                   | 712                 | 854                   | 229,490   | 100,247 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 7,003               | 12,584                | 1,624     | 264     |
| 100 neighbors                 | 41,920              | 47,072                | 9,313     | 2,201   |
| 10 neighbors                  | 459,636             | 355,808               | 19,203    | 7,369   |
| 1 neighbor                    | 506,724             | 348,290               | 30,138    | 11,825  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 612 (0.002s)        | 762 (0.001s)          | 587 (0.002s) | 421 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 5,058               | 4,886                 | 8,948     | 1,829   |
| Half (~span/4) (r=24.75)    | 6,223               | 6,843                 | 8,754     | 3,647   |
| Quarter (~span/8) (r=12.38) | 6,161               | 7,331                 | 11,020    | 6,225   |
| Tiny (~span/1000) (r=1)     | 42,632              | 40,355                | 214,799   | 156,236 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,088               | 6,151                 | 27,258    | 3,406   |
| Half (size≈49.50x4.5x4.5)      | 6,846               | 7,000                 | 42,678    | 35,422  |
| Quarter (size≈24.75x2.25x2.25) | 6,966               | 7,137                 | 155,812   | 115,275 |
| Unit (size=1)                  | 6,992               | 7,184                 | 299,263   | 161,096 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,127              | 11,306                | 614       | 182     |
| 100 neighbors                 | 54,452              | 74,900                | 5,828     | 2,185   |
| 10 neighbors                  | 511,172             | 492,986               | 27,056    | 12,455  |
| 1 neighbor                    | 726,471             | 671,124               | 45,376    | 20,857  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,076 (0.000s)      | 7,077 (0.000s)        | 2,929 (0.000s) | 1,044 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,054              | 15,932                | 24,445    | 18,406  |
| Half (~span/4) (r=2.25)    | 54,951              | 66,544                | 123,769   | 137,183 |
| Quarter (~span/8) (r=1.13) | 64,928              | 67,652                | 339,772   | 220,841 |
| Tiny (~span/1000) (r=1)    | 64,098              | 67,598                | 339,277   | 221,739 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 55,933              | 60,989                | 320,797   | 34,413  |
| Half (size≈4.5x4.5x4.5)       | 59,696              | 66,905                | 182,730   | 179,103 |
| Quarter (size≈2.25x2.25x2.25) | 60,226              | 68,338                | 474,827   | 237,142 |
| Unit (size=1)                 | 60,047              | 69,199                | 479,167   | 235,579 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 16,556              | 15,084                | 3,199     | 616     |
| 100 neighbors                 | 74,309              | 69,859                | 15,257    | 4,009   |
| 10 neighbors                  | 510,947             | 444,251               | 72,910    | 28,889  |
| 1 neighbor                    | 794,725             | 811,390               | 83,900    | 39,962  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 39,840 (0.000s)     | 34,722 (0.000s)       | 23,866 (0.000s) | 21,052 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 139,844             | 138,046               | 290,272   | 173,042 |
| Half (~span/4) (r=2.25)    | 165,693             | 167,671               | 315,100   | 287,260 |
| Quarter (~span/8) (r=1.13) | 164,055             | 166,874               | 392,265   | 422,334 |
| Tiny (~span/1000) (r=1)    | 161,988             | 166,375               | 400,878   | 429,680 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 516,653             | 524,146               | 1,914,461 | 335,170 |
| Half (size≈4.5x2x1)     | 533,878             | 549,697               | 478,925   | 476,491 |
| Quarter (size≈2.25x1x1) | 548,240             | 562,704               | 725,217   | 790,171 |
| Unit (size=1)           | 559,334             | 563,231               | 726,714   | 788,231 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 99,014              | 93,338                | 68,578    | 68,165  |
| 10 neighbors                  | 770,360             | 603,470               | 107,606   | 89,750  |
| 1 neighbor                    | 1,094,329           | 727,692               | 178,057   | 187,160 |

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
