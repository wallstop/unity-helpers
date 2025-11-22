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
| 1,000,000 entries | 2 (0.483s)          | 3 (0.316s)            | 4 (0.249s) | 1 (0.719s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 16                  | 19                    | 31        | 14      |
| Half (~span/4) (r=24.75)    | 120                 | 129                   | 221       | 127     |
| Quarter (~span/8) (r=12.38) | 934                 | 1,201                 | 1,662     | 1,506   |
| Tiny (~span/1000) (r=1)     | 23,371              | 23,390                | 138,193   | 76,262  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 27                  | 35                    | 157       | 19      |
| Half (size≈49.50x49.50x49.50)    | 36                  | 37                    | 1,382     | 203     |
| Quarter (size≈24.75x24.75x24.75) | 37                  | 41                    | 3,993     | 2,430   |
| Unit (size=1)                    | 35                  | 45                    | 182,605   | 77,778  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,746               | 10,139                | 2,294     | 306     |
| 100 neighbors                 | 64,677              | 73,822                | 10,722    | 3,369   |
| 10 neighbors                  | 402,737             | 414,383               | 16,134    | 7,713   |
| 1 neighbor                    | 536,739             | 469,658               | 20,079    | 8,461   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 47 (0.021s)           | 64 (0.015s) | 40 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 301                 | 483                   | 737       | 186     |
| Half (~span/4) (r=24.75)    | 1,018               | 1,436                 | 2,042     | 838     |
| Quarter (~span/8) (r=12.38) | 2,580               | 3,933                 | 5,995     | 3,354   |
| Tiny (~span/1000) (r=1)     | 26,665              | 29,858                | 177,772   | 101,279 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 458                 | 764                   | 2,624     | 337     |
| Half (size≈49.50x49.50x4.5)     | 724                 | 898                   | 9,342     | 3,489   |
| Quarter (size≈24.75x24.75x2.25) | 743                 | 913                   | 47,689    | 24,265  |
| Unit (size=1)                   | 709                 | 939                   | 246,258   | 102,910 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,598               | 12,111                | 1,619     | 272     |
| 100 neighbors                 | 38,596              | 44,119                | 9,266     | 2,277   |
| 10 neighbors                  | 442,288             | 330,465               | 19,156    | 7,779   |
| 1 neighbor                    | 431,322             | 308,508               | 30,410    | 12,341  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 453 (0.002s)        | 465 (0.002s)          | 592 (0.002s) | 428 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,754               | 4,594                 | 9,369     | 2,135   |
| Half (~span/4) (r=24.75)    | 5,948               | 6,496                 | 8,885     | 4,226   |
| Quarter (~span/8) (r=12.38) | 5,913               | 6,973                 | 10,999    | 7,476   |
| Tiny (~span/1000) (r=1)     | 41,200              | 38,942                | 222,143   | 159,133 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,499               | 6,411                 | 26,300    | 3,540   |
| Half (size≈49.50x4.5x4.5)      | 7,330               | 7,379                 | 46,213    | 37,593  |
| Quarter (size≈24.75x2.25x2.25) | 7,416               | 7,565                 | 167,267   | 122,665 |
| Unit (size=1)                  | 7,608               | 7,616                 | 325,012   | 165,185 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,760               | 10,490                | 623       | 186     |
| 100 neighbors                 | 50,925              | 65,221                | 5,852     | 2,253   |
| 10 neighbors                  | 428,458             | 449,236               | 27,280    | 13,057  |
| 1 neighbor                    | 662,163             | 588,721               | 45,567    | 22,289  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,943 (0.000s)      | 3,408 (0.000s)        | 4,338 (0.000s) | 3,779 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,017              | 14,042                | 24,664    | 21,469  |
| Half (~span/4) (r=2.25)    | 52,586              | 62,483                | 124,061   | 148,377 |
| Quarter (~span/8) (r=1.13) | 62,306              | 64,637                | 340,778   | 223,994 |
| Tiny (~span/1000) (r=1)    | 62,496              | 64,623                | 340,573   | 222,111 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 59,625              | 64,237                | 317,933   | 35,842  |
| Half (size≈4.5x4.5x4.5)       | 65,069              | 71,323                | 198,932   | 171,193 |
| Quarter (size≈2.25x2.25x2.25) | 65,834              | 72,218                | 500,256   | 237,757 |
| Unit (size=1)                 | 65,772              | 71,183                | 500,011   | 239,922 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,029              | 14,964                | 3,210     | 623     |
| 100 neighbors                 | 69,649              | 65,790                | 15,510    | 4,148   |
| 10 neighbors                  | 438,390             | 415,113               | 74,960    | 33,364  |
| 1 neighbor                    | 709,819             | 654,283               | 85,681    | 44,582  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 36,231 (0.000s)     | 30,030 (0.000s)       | 27,173 (0.000s) | 17,636 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 125,237             | 128,345               | 296,064   | 184,839 |
| Half (~span/4) (r=2.25)    | 147,089             | 150,320               | 315,886   | 295,598 |
| Quarter (~span/8) (r=1.13) | 146,823             | 151,437               | 392,138   | 428,342 |
| Tiny (~span/1000) (r=1)    | 146,927             | 149,811               | 394,345   | 419,975 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 533,966             | 533,932               | 1,846,992 | 331,751 |
| Half (size≈4.5x2x1)     | 566,611             | 575,881               | 518,754   | 465,129 |
| Quarter (size≈2.25x1x1) | 577,429             | 579,419               | 766,089   | 684,022 |
| Unit (size=1)           | 579,312             | 577,314               | 761,633   | 735,883 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 91,325              | 87,998                | 66,390    | 65,849  |
| 10 neighbors                  | 611,217             | 503,424               | 100,396   | 101,600 |
| 1 neighbor                    | 848,656             | 628,245               | 173,946   | 224,233 |

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
