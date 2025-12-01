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
| 1,000,000 entries | 2 (0.338s)          | 4 (0.218s)            | 4 (0.229s) | 3 (0.293s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 58                  | 57                    | 55         | 7       |
| Half (~span/4) (r=249.8)    | 237                 | 238                   | 211        | 27      |
| Quarter (~span/8) (r=124.9) | 948                 | 944                   | 813        | 116     |
| Tiny (~span/1000) (r=1)     | 103,881             | 105,576               | 142,009    | 107,850 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 273                 | 324                   | 295        | 17      |
| Half (size=499.5x499.5)    | 1,838               | 1,825                 | 1,199      | 68      |
| Quarter (size=249.8x249.8) | 7,438               | 7,291                 | 3,767      | 379     |
| Unit (size=1)              | 149,258             | 151,037               | 194,664    | 114,390 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,398               | 16,382                | 12,387     | 64,353  |
| 100 neighbors                 | 72,530              | 70,442                | 70,968     | 149,117 |
| 10 neighbors                  | 257,352             | 247,844               | 172,179    | 224,198 |
| 1 neighbor                    | 368,042             | 369,082               | 230,870    | 235,804 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 49 (0.020s)         | 82 (0.012s)           | 50 (0.020s) | 10 (0.096s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 590                 | 592                   | 601        | 71      |
| Half (~span/4) (r=99.75)    | 1,342               | 1,338                 | 1,248      | 185     |
| Quarter (~span/8) (r=49.88) | 4,658               | 5,094                 | 4,282      | 723     |
| Tiny (~span/1000) (r=1)     | 128,803             | 127,118               | 178,485    | 148,850 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,618               | 4,606                 | 4,507      | 235     |
| Half (size=199.5x124.5)    | 9,665               | 12,061                | 7,841      | 965     |
| Quarter (size=99.75x62.25) | 25,449              | 32,464                | 19,369     | 3,754   |
| Unit (size=1)              | 182,592             | 186,021               | 245,852    | 155,472 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,777               | 9,781                 | 11,632     | 64,135  |
| 100 neighbors                 | 46,060              | 85,453                | 52,893     | 184,842 |
| 10 neighbors                  | 269,051             | 260,020               | 210,912    | 263,129 |
| 1 neighbor                    | 303,462             | 394,546               | 205,970    | 276,477 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 105 (0.009s)        | 790 (0.001s)          | 537 (0.002s) | 485 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,843               | 5,904                 | 5,936      | 725     |
| Half (~span/4) (r=24.75)    | 21,973              | 21,973                | 13,893     | 2,888   |
| Quarter (~span/8) (r=12.38) | 43,615              | 50,984                | 38,177     | 12,032  |
| Tiny (~span/1000) (r=1)     | 166,941             | 162,122               | 237,430    | 170,313 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 46,299              | 45,485                | 46,388     | 2,388   |
| Half (size=49.50x49.50)    | 167,801             | 146,697               | 36,130     | 9,182   |
| Quarter (size=24.75x24.75) | 75,205              | 101,694               | 75,978     | 35,312  |
| Unit (size=1)              | 243,717             | 232,574               | 326,608    | 182,146 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,793              | 12,773                | 14,274     | 60,088  |
| 100 neighbors                 | 56,382              | 53,681                | 88,844     | 188,368 |
| 10 neighbors                  | 230,862             | 268,624               | 220,735    | 296,263 |
| 1 neighbor                    | 409,467             | 416,317               | 264,031    | 323,927 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,943 (0.000s)      | 6,934 (0.000s)        | 4,659 (0.000s) | 1,551 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 57,564              | 57,647                | 57,619     | 7,271   |
| Half (~span/4) (r=12.25)   | 60,174              | 75,928                | 57,466     | 14,453  |
| Quarter (~span/8) (r=6.13) | 96,482              | 107,924               | 95,806     | 37,615  |
| Tiny (~span/1000) (r=1)    | 243,100             | 237,619               | 339,007    | 256,514 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 509,120             | 508,038               | 425,054    | 23,935  |
| Half (size=24.50x9.5)     | 166,711             | 290,938               | 124,093    | 75,494  |
| Quarter (size=12.25x4.75) | 270,220             | 290,685               | 193,809    | 176,525 |
| Unit (size=1)             | 343,510             | 343,471               | 472,566    | 285,031 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 42,830              | 44,118                | 38,088     | 64,393  |
| 100 neighbors                 | 75,094              | 72,181                | 83,359     | 212,976 |
| 10 neighbors                  | 308,195             | 331,951               | 264,024    | 359,459 |
| 1 neighbor                    | 461,273             | 303,348               | 268,188    | 382,152 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 42,372 (0.000s)     | 37,174 (0.000s)       | 26,525 (0.000s) | 17,152 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 524,222             | 522,434               | 517,728    | 73,433  |
| Half (~span/4) (r=2.25)    | 442,223             | 447,505               | 260,263    | 243,780 |
| Quarter (~span/8) (r=1.13) | 442,263             | 447,388               | 615,028    | 349,771 |
| Tiny (~span/1000) (r=1)    | 442,413             | 443,996               | 610,062    | 349,727 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,681,091           | 2,671,218             | 2,827,137  | 226,920 |
| Half (size=4.5x4.5)      | 577,697             | 567,390               | 368,436    | 384,395 |
| Quarter (size=2.25x2.25) | 603,787             | 609,536               | 811,112    | 410,048 |
| Unit (size=1)            | 610,591             | 609,805               | 815,193    | 414,809 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 142,909             | 140,969               | 168,955    | 246,841 |
| 10 neighbors                  | 350,787             | 285,160               | 401,688    | 459,262 |
| 1 neighbor                    | 355,622             | 447,185               | 433,092    | 494,363 |

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
