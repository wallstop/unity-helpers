# 2D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Fast range/bounds/nearest‑neighbor queries on 2D data without scanning everything.
- Quick picks: QuadTree2D for broad‑phase; KDTree2D (Balanced) for NN; KDTree2D (Unbalanced) for fast rebuilds; RTree2D for bounds‑based data.

This document contains performance benchmarks for the 2D spatial tree implementations in Unity Helpers.

## Available 2D Spatial Trees

- **QuadTree2D** - Easiest to use, good all-around performance
- **KDTree2D** - Balanced and unbalanced variants available
- **RTree2D** - Optimized for bounding box queries

### Correctness & Semantics

- QuadTree2D and KdTree2D (balanced and unbalanced) guarantee the same results for the same input data and the same queries. They are both point-based trees and differ only in construction/query performance characteristics.
- RTree2D is bounds-based (stores rectangles/AABBs), not points. Its spatial knowledge and query semantics operate on rectangles, so its results will intentionally differ for sized objects and bounds intersection queries.

## Performance Benchmarks

<!-- SPATIAL_TREE_BENCHMARKS_START -->

### Datasets

<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

| Construction      | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D    |
| ----------------- | ------------------- | --------------------- | ---------- | ---------- |
| 1,000,000 entries | 3 (0.255s)          | 6 (0.161s)            | 4 (0.224s) | 1 (0.503s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 55                  | 56                    | 53         | 7       |
| Half (~span/4) (r=249.8)    | 224                 | 222                   | 191        | 27      |
| Quarter (~span/8) (r=124.9) | 943                 | 931                   | 795        | 114     |
| Tiny (~span/1000) (r=1)     | 100,547             | 103,114               | 142,261    | 94,113  |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 223                 | 225                   | 234        | 16      |
| Half (size=499.5x499.5)    | 1,687               | 1,654                 | 1,203      | 62      |
| Quarter (size=249.8x249.8) | 6,860               | 6,881                 | 3,745      | 352     |
| Unit (size=1)              | 145,270             | 148,371               | 177,075    | 110,131 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,249               | 16,269                | 12,069     | 62,477  |
| 100 neighbors                 | 71,374              | 70,276                | 69,844     | 142,122 |
| 10 neighbors                  | 237,122             | 213,039               | 176,000    | 211,213 |
| 1 neighbor                    | 313,561             | 349,488               | 217,382    | 219,491 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 49 (0.020s)         | 81 (0.012s)           | 49 (0.020s) | 51 (0.019s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 588                 | 588                   | 586        | 70      |
| Half (~span/4) (r=99.75)    | 1,353               | 1,319                 | 1,220      | 184     |
| Quarter (~span/8) (r=49.88) | 4,656               | 5,044                 | 4,209      | 726     |
| Tiny (~span/1000) (r=1)     | 124,961             | 125,898               | 175,410    | 144,803 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,395               | 4,476                 | 4,437      | 227     |
| Half (size=199.5x124.5)    | 9,273               | 11,681                | 7,840      | 962     |
| Quarter (size=99.75x62.25) | 24,435              | 31,618                | 19,092     | 3,693   |
| Unit (size=1)              | 179,219             | 184,183               | 243,274    | 147,860 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,697               | 9,759                 | 11,549     | 59,927  |
| 100 neighbors                 | 47,140              | 84,266                | 52,198     | 165,088 |
| 10 neighbors                  | 276,388             | 245,932               | 205,848    | 250,120 |
| 1 neighbor                    | 262,093             | 328,553               | 251,731    | 262,832 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 549 (0.002s)        | 799 (0.001s)          | 537 (0.002s) | 530 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,786               | 5,754                 | 5,849      | 728     |
| Half (~span/4) (r=24.75)    | 21,854              | 21,783                | 13,524     | 2,884   |
| Quarter (~span/8) (r=12.38) | 43,326              | 51,484                | 37,410     | 11,966  |
| Tiny (~span/1000) (r=1)     | 163,988             | 162,570               | 229,922    | 163,969 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 44,167              | 44,274                | 45,359     | 2,344   |
| Half (size=49.50x49.50)    | 159,122             | 162,112               | 35,834     | 8,974   |
| Quarter (size=24.75x24.75) | 73,618              | 102,088               | 73,941     | 34,678  |
| Unit (size=1)              | 235,509             | 232,129               | 315,519    | 173,983 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,581              | 12,653                | 14,278     | 57,412  |
| 100 neighbors                 | 56,960              | 53,317                | 87,994     | 174,362 |
| 10 neighbors                  | 246,768             | 252,787               | 213,248    | 288,903 |
| 1 neighbor                    | 319,850             | 330,998               | 277,923    | 311,995 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,324 (0.000s)      | 7,396 (0.000s)        | 4,290 (0.000s) | 4,359 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 57,854              | 55,737                | 56,391     | 7,363   |
| Half (~span/4) (r=12.25)   | 60,209              | 74,921                | 55,756     | 14,649  |
| Quarter (~span/8) (r=6.13) | 95,786              | 106,354               | 94,746     | 37,143  |
| Tiny (~span/1000) (r=1)    | 234,925             | 233,698               | 337,789    | 241,337 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 490,411             | 491,427               | 530,454    | 23,657  |
| Half (size=24.50x9.5)     | 161,883             | 280,421               | 126,963    | 73,597  |
| Quarter (size=12.25x4.75) | 262,278             | 279,046               | 193,896    | 175,429 |
| Unit (size=1)             | 325,000             | 331,656               | 463,365    | 276,042 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 42,129              | 43,788                | 38,136     | 63,758  |
| 100 neighbors                 | 69,655              | 69,541                | 82,294     | 206,695 |
| 10 neighbors                  | 234,856             | 269,348               | 230,560    | 296,385 |
| 1 neighbor                    | 388,482             | 303,993               | 260,803    | 357,658 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 36,496 (0.000s)     | 28,571 (0.000s)       | 26,455 (0.000s) | 19,960 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 501,264             | 487,951               | 494,804    | 72,213  |
| Half (~span/4) (r=2.25)    | 420,404             | 410,828               | 252,296    | 235,727 |
| Quarter (~span/8) (r=1.13) | 427,817             | 400,731               | 595,504    | 334,254 |
| Tiny (~span/1000) (r=1)    | 417,250             | 425,515               | 584,466    | 329,085 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,390,294           | 2,396,880             | 2,250,310  | 215,322 |
| Half (size=4.5x4.5)      | 520,827             | 545,103               | 359,441    | 359,893 |
| Quarter (size=2.25x2.25) | 518,805             | 585,176               | 775,634    | 394,432 |
| Unit (size=1)            | 580,182             | 588,321               | 765,130    | 388,880 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 134,124             | 132,499               | 163,003    | 220,302 |
| 10 neighbors                  | 311,451             | 241,692               | 370,663    | 385,824 |
| 1 neighbor                    | 313,893             | 407,589               | 358,487    | 444,770 |

<!-- tabs:end -->
<!-- SPATIAL_TREE_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**QuadTree2D**:

- Best for: General-purpose 2D spatial queries
- Strengths: Balanced performance across all operation types, simple to use
- Weaknesses: Slightly slower than KDTree for point queries

**KDTree2D (Balanced)**:

- Best for: When you need consistent query performance
- Strengths: Fast nearest-neighbor queries, good for smaller datasets
- Weaknesses: Slower construction time

**KDTree2D (Unbalanced)**:

- Best for: When you need fast construction and will rebuild frequently
- Strengths: Fastest construction, similar query performance to balanced
- Weaknesses: May degrade on pathological data distributions

**RTree2D**:

- Best for: Bounding box queries, especially with large query areas
- Strengths: Excellent for large bounding box queries, handles overlapping objects well
- Weaknesses: Slower for point queries and small ranges

### Important Notes

- All spatial trees assume **immutable** positional data
- If positions change, you must reconstruct the tree
- Spatial queries are O(log n) vs O(n) for linear search
- Construction cost is amortized over many queries
