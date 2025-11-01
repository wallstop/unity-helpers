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
| 1,000,000 entries | 2 (0.386s)          | 3 (0.311s)            | 3 (0.314s) | 2 (0.387s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 34        | 17      |
| Half (~span/4) (r=24.75)    | 134                 | 157                   | 256       | 150     |
| Quarter (~span/8) (r=12.38) | 925                 | 1,215                 | 1,661     | 1,427   |
| Tiny (~span/1000) (r=1)     | 23,280              | 23,446                | 137,215   | 74,463  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 34                  | 41                    | 227       | 24      |
| Half (size≈49.50x49.50x49.50)    | 41                  | 47                    | 1,259     | 279     |
| Quarter (size≈24.75x24.75x24.75) | 41                  | 48                    | 3,996     | 2,504   |
| Unit (size=1)                    | 41                  | 46                    | 181,169   | 73,748  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,889               | 10,198                | 2,309     | 303     |
| 100 neighbors                 | 62,035              | 74,060                | 11,015    | 3,398   |
| 10 neighbors                  | 386,107             | 383,898               | 16,257    | 7,968   |
| 1 neighbor                    | 541,268             | 511,878               | 20,078    | 8,672   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 47 (0.021s)           | 42 (0.024s) | 43 (0.023s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 337                 | 486                   | 769       | 200     |
| Half (~span/4) (r=24.75)    | 1,015               | 1,448                 | 2,018     | 815     |
| Quarter (~span/8) (r=12.38) | 2,567               | 4,000                 | 5,907     | 3,314   |
| Tiny (~span/1000) (r=1)     | 26,783              | 29,830                | 176,298   | 100,804 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 637                 | 772                   | 2,696     | 339     |
| Half (size≈49.50x49.50x4.5)     | 731                 | 899                   | 10,007    | 3,497   |
| Quarter (size≈24.75x24.75x2.25) | 745                 | 924                   | 47,730    | 24,350  |
| Unit (size=1)                   | 750                 | 934                   | 246,402   | 103,577 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,701               | 12,076                | 1,626     | 272     |
| 100 neighbors                 | 38,478              | 43,819                | 9,281     | 2,287   |
| 10 neighbors                  | 457,528             | 324,448               | 19,296    | 7,848   |
| 1 neighbor                    | 428,011             | 322,856               | 30,123    | 12,433  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 440 (0.002s)        | 467 (0.002s)          | 602 (0.002s) | 425 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,788               | 4,609                 | 8,939     | 2,061   |
| Half (~span/4) (r=24.75)    | 5,959               | 6,429                 | 8,899     | 4,006   |
| Quarter (~span/8) (r=12.38) | 5,929               | 6,874                 | 11,071    | 7,017   |
| Tiny (~span/1000) (r=1)     | 41,423              | 38,778                | 217,367   | 157,581 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,446               | 6,428                 | 26,035    | 3,555   |
| Half (size≈49.50x4.5x4.5)      | 7,296               | 7,344                 | 45,721    | 37,319  |
| Quarter (size≈24.75x2.25x2.25) | 7,459               | 7,495                 | 165,622   | 121,514 |
| Unit (size=1)                  | 7,506               | 7,548                 | 317,874   | 163,916 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,716               | 10,667                | 638       | 184     |
| 100 neighbors                 | 50,838              | 68,861                | 5,947     | 2,267   |
| 10 neighbors                  | 452,030             | 403,786               | 27,125    | 13,409  |
| 1 neighbor                    | 625,292             | 623,208               | 43,364    | 22,623  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,951 (0.000s)      | 5,170 (0.000s)        | 3,157 (0.000s) | 4,184 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,041              | 14,157                | 24,772    | 21,692  |
| Half (~span/4) (r=2.25)    | 52,426              | 62,638                | 123,516   | 146,308 |
| Quarter (~span/8) (r=1.13) | 62,349              | 64,703                | 323,699   | 226,590 |
| Tiny (~span/1000) (r=1)    | 62,088              | 64,685                | 338,837   | 226,613 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 58,987              | 64,476                | 322,955   | 36,020  |
| Half (size≈4.5x4.5x4.5)       | 64,592              | 71,575                | 198,560   | 181,224 |
| Quarter (size≈2.25x2.25x2.25) | 65,299              | 73,113                | 500,481   | 239,455 |
| Unit (size=1)                 | 65,256              | 74,180                | 501,485   | 240,070 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,930              | 14,706                | 3,297     | 622     |
| 100 neighbors                 | 70,057              | 66,204                | 15,802    | 4,169   |
| 10 neighbors                  | 461,442             | 384,350               | 75,444    | 33,715  |
| 1 neighbor                    | 673,046             | 704,737               | 86,033    | 45,140  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 32,679 (0.000s)     | 33,003 (0.000s)       | 25,974 (0.000s) | 13,404 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 125,671             | 127,643               | 291,486   | 184,062 |
| Half (~span/4) (r=2.25)    | 146,268             | 149,611               | 314,058   | 296,509 |
| Quarter (~span/8) (r=1.13) | 146,574             | 151,138               | 376,930   | 419,883 |
| Tiny (~span/1000) (r=1)    | 143,793             | 151,149               | 395,012   | 419,518 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 539,503             | 540,192               | 1,850,861 | 335,224 |
| Half (size≈4.5x2x1)     | 567,669             | 576,187               | 517,599   | 467,910 |
| Quarter (size≈2.25x1x1) | 580,752             | 579,288               | 772,525   | 747,066 |
| Unit (size=1)           | 581,174             | 582,127               | 773,401   | 693,825 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 92,209              | 88,369                | 69,873    | 66,050  |
| 10 neighbors                  | 626,406             | 497,039               | 105,917   | 102,085 |
| 1 neighbor                    | 886,386             | 681,324               | 170,819   | 226,654 |

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
