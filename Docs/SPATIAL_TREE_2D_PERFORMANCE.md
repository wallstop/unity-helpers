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
| 1,000,000 entries | 2 (0.354s)          | 6 (0.156s)            | 3 (0.268s) | 2 (0.339s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 58                  | 58                    | 57         | 7       |
| Half (~span/4) (r=249.8)    | 235                 | 237                   | 219        | 29      |
| Quarter (~span/8) (r=124.9) | 940                 | 947                   | 816        | 120     |
| Tiny (~span/1000) (r=1)     | 98,696              | 105,775               | 143,835    | 107,084 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 379                 | 393                   | 353        | 17      |
| Half (size=499.5x499.5)    | 1,821               | 1,817                 | 1,226      | 84      |
| Quarter (size=249.8x249.8) | 7,280               | 7,385                 | 3,859      | 378     |
| Unit (size=1)              | 147,586             | 151,318               | 195,441    | 112,431 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,395               | 16,697                | 12,604     | 69,633  |
| 100 neighbors                 | 78,733              | 75,612                | 78,788     | 176,887 |
| 10 neighbors                  | 390,411             | 349,222               | 246,078    | 282,260 |
| 1 neighbor                    | 358,817             | 525,696               | 271,846    | 296,279 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 51 (0.020s)         | 44 (0.022s)           | 50 (0.020s) | 50 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 601                 | 602                   | 601        | 73      |
| Half (~span/4) (r=99.75)    | 1,356               | 1,359                 | 1,249      | 185     |
| Quarter (~span/8) (r=49.88) | 4,671               | 5,178                 | 4,309      | 722     |
| Tiny (~span/1000) (r=1)     | 127,876             | 125,818               | 179,577    | 145,184 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,571               | 4,693                 | 4,555      | 204     |
| Half (size=199.5x124.5)    | 9,647               | 12,035                | 8,015      | 957     |
| Quarter (size=99.75x62.25) | 25,558              | 32,582                | 20,454     | 3,794   |
| Unit (size=1)              | 176,032             | 184,518               | 244,701    | 154,846 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,964               | 9,873                 | 11,621     | 69,560  |
| 100 neighbors                 | 47,419              | 92,946                | 55,461     | 225,496 |
| 10 neighbors                  | 365,326             | 393,246               | 279,798    | 335,585 |
| 1 neighbor                    | 505,353             | 546,586               | 338,093    | 373,647 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 538 (0.002s)        | 772 (0.001s)          | 540 (0.002s) | 496 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,946               | 5,942                 | 5,942      | 732     |
| Half (~span/4) (r=24.75)    | 22,323              | 22,256                | 13,887     | 2,921   |
| Quarter (~span/8) (r=12.38) | 44,263              | 51,436                | 38,120     | 12,196  |
| Tiny (~span/1000) (r=1)     | 167,914             | 162,828               | 234,758    | 165,059 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 45,033              | 46,170                | 47,169     | 2,412   |
| Half (size=49.50x49.50)    | 146,712             | 167,136               | 36,956     | 9,273   |
| Quarter (size=24.75x24.75) | 75,627              | 104,054               | 75,741     | 35,099  |
| Unit (size=1)              | 241,459             | 233,634               | 319,475    | 174,013 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,230              | 13,034                | 14,325     | 65,243  |
| 100 neighbors                 | 62,254              | 56,983                | 96,417     | 235,319 |
| 10 neighbors                  | 393,072             | 422,457               | 294,227    | 417,752 |
| 1 neighbor                    | 595,664             | 605,956               | 395,072    | 456,295 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,393 (0.000s)      | 3,003 (0.000s)        | 4,024 (0.000s) | 4,470 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,049              | 57,884                | 57,185     | 7,402   |
| Half (~span/4) (r=12.25)   | 60,121              | 76,349                | 56,904     | 14,683  |
| Quarter (~span/8) (r=6.13) | 95,051              | 97,890                | 94,976     | 37,941  |
| Tiny (~span/1000) (r=1)    | 229,998             | 237,245               | 323,494    | 250,167 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 498,335             | 498,701               | 538,383    | 24,071  |
| Half (size=24.50x9.5)     | 166,102             | 293,033               | 127,174    | 74,870  |
| Quarter (size=12.25x4.75) | 269,033             | 287,988               | 194,901    | 176,651 |
| Unit (size=1)             | 340,608             | 337,130               | 460,641    | 276,801 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,237              | 46,929                | 39,983     | 66,864  |
| 100 neighbors                 | 80,195              | 77,684                | 92,241     | 272,011 |
| 10 neighbors                  | 489,597             | 557,420               | 399,861    | 538,598 |
| 1 neighbor                    | 747,001             | 609,483               | 426,085    | 552,832 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 42,735 (0.000s)     | 36,231 (0.000s)       | 26,315 (0.000s) | 22,471 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 504,459             | 495,563               | 510,528    | 72,563  |
| Half (~span/4) (r=2.25)    | 431,002             | 397,329               | 257,274    | 235,707 |
| Quarter (~span/8) (r=1.13) | 430,715             | 394,013               | 601,013    | 336,466 |
| Tiny (~span/1000) (r=1)    | 430,883             | 392,679               | 600,979    | 337,014 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,436,724           | 2,282,647             | 2,391,545  | 223,564 |
| Half (size=4.5x4.5)      | 570,640             | 561,091               | 369,188    | 369,946 |
| Quarter (size=2.25x2.25) | 577,487             | 600,918               | 793,868    | 384,323 |
| Unit (size=1)            | 571,007             | 599,929               | 794,377    | 376,900 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 164,805             | 164,238               | 201,513    | 321,931 |
| 10 neighbors                  | 629,206             | 489,315               | 600,750    | 753,665 |
| 1 neighbor                    | 637,135             | 770,994               | 673,398    | 813,998 |

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
