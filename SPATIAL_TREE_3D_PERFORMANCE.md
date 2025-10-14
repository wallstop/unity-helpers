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
| 1,000,000 entries | 2 (0.462s)          | 2 (0.334s)            | 4 (0.238s) | 2 (0.415s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 32        | 15      |
| Half (~span/4) (r=24.75)    | 127                 | 157                   | 244       | 159     |
| Quarter (~span/8) (r=12.38) | 934                 | 1,205                 | 1,662     | 1,521   |
| Tiny (~span/1000) (r=1)     | 23,474              | 23,256                | 136,339   | 76,528  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 32                  | 38                    | 183       | 20      |
| Half (size≈49.50x49.50x49.50)    | 38                  | 44                    | 1,296     | 269     |
| Quarter (size≈24.75x24.75x24.75) | 39                  | 45                    | 4,051     | 2,514   |
| Unit (size=1)                    | 40                  | 47                    | 176,851   | 71,895  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,917               | 10,145                | 2,299     | 305     |
| 100 neighbors                 | 65,265              | 72,176                | 10,753    | 3,343   |
| 10 neighbors                  | 406,365             | 415,717               | 15,601    | 7,643   |
| 1 neighbor                    | 550,998             | 479,367               | 19,376    | 8,299   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 35 (0.029s)         | 48 (0.021s)           | 66 (0.015s) | 41 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 328                 | 476                   | 767       | 198     |
| Half (~span/4) (r=24.75)    | 1,000               | 1,424                 | 2,024     | 856     |
| Quarter (~span/8) (r=12.38) | 2,519               | 3,929                 | 5,943     | 3,444   |
| Tiny (~span/1000) (r=1)     | 26,289              | 29,307                | 174,966   | 101,213 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 636                 | 764                   | 2,638     | 349     |
| Half (size≈49.50x49.50x4.5)     | 741                 | 903                   | 9,365     | 3,477   |
| Quarter (size≈24.75x24.75x2.25) | 755                 | 933                   | 48,138    | 24,155  |
| Unit (size=1)                   | 759                 | 940                   | 246,230   | 101,413 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,677               | 12,019                | 1,631     | 271     |
| 100 neighbors                 | 38,824              | 44,184                | 9,263     | 2,236   |
| 10 neighbors                  | 449,797             | 330,300               | 19,167    | 7,478   |
| 1 neighbor                    | 426,499             | 310,941               | 30,037    | 11,896  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 466 (0.002s)        | 473 (0.002s)          | 615 (0.002s) | 450 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,683               | 4,522                 | 8,927     | 2,011   |
| Half (~span/4) (r=24.75)    | 5,822               | 6,362                 | 8,898     | 3,899   |
| Quarter (~span/8) (r=12.38) | 5,798               | 6,791                 | 11,108    | 7,083   |
| Tiny (~span/1000) (r=1)     | 40,628              | 38,481                | 218,875   | 158,338 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,478               | 6,481                 | 26,295    | 3,562   |
| Half (size≈49.50x4.5x4.5)      | 7,367               | 7,417                 | 46,056    | 37,401  |
| Quarter (size≈24.75x2.25x2.25) | 7,531               | 7,578                 | 169,110   | 120,511 |
| Unit (size=1)                  | 7,612               | 7,626                 | 325,145   | 161,706 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,732               | 10,597                | 637       | 185     |
| 100 neighbors                 | 51,320              | 69,698                | 5,914     | 2,235   |
| 10 neighbors                  | 435,223             | 421,196               | 27,006    | 12,804  |
| 1 neighbor                    | 664,302             | 592,441               | 45,255    | 21,503  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,617 (0.000s)      | 5,141 (0.000s)        | 3,954 (0.000s) | 4,130 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 11,802              | 13,903                | 24,582    | 21,692  |
| Half (~span/4) (r=2.25)    | 51,580              | 61,355                | 123,544   | 144,993 |
| Quarter (~span/8) (r=1.13) | 61,600              | 63,517                | 337,985   | 224,573 |
| Tiny (~span/1000) (r=1)    | 61,597              | 63,370                | 337,192   | 215,561 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 59,706              | 64,466                | 323,948   | 35,609  |
| Half (size≈4.5x4.5x4.5)       | 65,193              | 71,417                | 199,275   | 177,711 |
| Quarter (size≈2.25x2.25x2.25) | 65,932              | 73,139                | 502,862   | 234,032 |
| Unit (size=1)                 | 65,955              | 74,000                | 502,591   | 234,547 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,674              | 14,692                | 3,264     | 621     |
| 100 neighbors                 | 70,092              | 63,853                | 15,646    | 4,137   |
| 10 neighbors                  | 436,821             | 416,753               | 74,823    | 32,560  |
| 1 neighbor                    | 709,835             | 663,405               | 84,669    | 43,468  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 35,714 (0.000s)     | 32,051 (0.000s)       | 27,397 (0.000s) | 21,413 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 123,749             | 124,911               | 295,441   | 173,986 |
| Half (~span/4) (r=2.25)    | 144,742             | 146,831               | 317,894   | 305,105 |
| Quarter (~span/8) (r=1.13) | 144,970             | 148,219               | 392,233   | 422,688 |
| Tiny (~span/1000) (r=1)    | 144,979             | 148,160               | 394,402   | 397,230 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 542,170             | 541,750               | 1,808,352 | 327,419 |
| Half (size≈4.5x2x1)     | 567,151             | 574,657               | 499,944   | 457,996 |
| Quarter (size≈2.25x1x1) | 581,138             | 575,322               | 777,105   | 728,444 |
| Unit (size=1)           | 580,654             | 547,510               | 776,427   | 729,736 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 92,016              | 88,604                | 69,691    | 65,613  |
| 10 neighbors                  | 622,325             | 548,943               | 105,436   | 99,977  |
| 1 neighbor                    | 876,886             | 640,575               | 172,804   | 210,070 |

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
