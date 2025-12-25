# 2D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Fast range/bounds/nearest‑neighbor queries on 2D data without scanning everything.
- Quick picks: QuadTree2D for broad‑phase; KdTree2D (Balanced) for NN; KdTree2D (Unbalanced) for fast rebuilds; RTree2D for bounds‑based data.

This document contains performance benchmarks for the 2D spatial tree implementations in Unity Helpers.

## Available 2D Spatial Trees

- **QuadTree2D** - Easiest to use, good all-around performance
- **KdTree2D** - Balanced and unbalanced variants available
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
| 1,000,000 entries | 2 (0.374s)          | 6 (0.160s)            | 4 (0.225s) | 1 (0.705s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 58                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 236                 | 235                   | 209        | 27      |
| Quarter (~span/8) (r=124.9) | 937                 | 929                   | 791        | 116     |
| Tiny (~span/1000) (r=1)     | 103,514             | 105,438               | 141,967    | 104,884 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 362                 | 307                   | 322        | 17      |
| Half (size=499.5x499.5)    | 1,756               | 1,787                 | 1,211      | 74      |
| Quarter (size=249.8x249.8) | 6,834               | 6,928                 | 3,670      | 368     |
| Unit (size=1)              | 143,955             | 152,422               | 197,071    | 109,823 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,272               | 16,397                | 12,253     | 64,225  |
| 100 neighbors                 | 73,216              | 69,292                | 73,099     | 149,669 |
| 10 neighbors                  | 260,055             | 231,234               | 187,514    | 224,036 |
| 1 neighbor                    | 376,256             | 371,507               | 216,845    | 233,989 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 48 (0.021s)         | 81 (0.012s)           | 50 (0.020s) | 48 (0.021s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 592                 | 592                   | 586        | 69      |
| Half (~span/4) (r=99.75)    | 1,353               | 1,336                 | 1,215      | 181     |
| Quarter (~span/8) (r=49.88) | 4,673               | 5,101                 | 4,213      | 704     |
| Tiny (~span/1000) (r=1)     | 126,858             | 128,210               | 177,813    | 145,535 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,409               | 4,412                 | 4,568      | 221     |
| Half (size=199.5x124.5)    | 9,486               | 12,065                | 7,891      | 900     |
| Quarter (size=99.75x62.25) | 25,170              | 32,865                | 19,517     | 3,540   |
| Unit (size=1)              | 183,360             | 184,882               | 246,868    | 153,392 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,529               | 9,582                 | 11,269     | 63,726  |
| 100 neighbors                 | 46,699              | 83,570                | 51,927     | 182,830 |
| 10 neighbors                  | 299,991             | 240,861               | 210,336    | 259,535 |
| 1 neighbor                    | 303,514             | 387,967               | 256,672    | 278,437 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 501 (0.002s)        | 786 (0.001s)          | 540 (0.002s) | 516 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,850               | 5,875                 | 5,827      | 721     |
| Half (~span/4) (r=24.75)    | 22,054              | 22,078                | 13,647     | 2,879   |
| Quarter (~span/8) (r=12.38) | 43,893              | 50,967                | 37,436     | 11,971  |
| Tiny (~span/1000) (r=1)     | 167,413             | 163,396               | 232,648    | 169,952 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 44,640              | 44,860                | 45,353     | 2,357   |
| Half (size=49.50x49.50)    | 146,639             | 164,616               | 37,028     | 8,893   |
| Quarter (size=24.75x24.75) | 74,741              | 102,012               | 74,855     | 34,541  |
| Unit (size=1)              | 238,890             | 232,514               | 321,895    | 180,528 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,508              | 12,595                | 13,912     | 60,304  |
| 100 neighbors                 | 58,280              | 53,580                | 86,872     | 189,169 |
| 10 neighbors                  | 269,147             | 269,647               | 225,750    | 300,743 |
| 1 neighbor                    | 366,559             | 412,254               | 288,851    | 324,079 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,243 (0.000s)      | 6,854 (0.000s)        | 4,821 (0.000s) | 2,649 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 56,948              | 56,950                | 56,791     | 7,265   |
| Half (~span/4) (r=12.25)   | 59,584              | 75,601                | 56,478     | 14,477  |
| Quarter (~span/8) (r=6.13) | 94,632              | 108,320               | 94,822     | 37,568  |
| Tiny (~span/1000) (r=1)    | 240,466             | 238,407               | 338,278    | 254,231 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 509,300             | 509,817               | 464,618    | 23,094  |
| Half (size=24.50x9.5)     | 165,938             | 292,014               | 125,433    | 72,837  |
| Quarter (size=12.25x4.75) | 271,101             | 289,103               | 191,382    | 176,082 |
| Unit (size=1)             | 342,066             | 337,701               | 466,803    | 284,695 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 43,764              | 44,014                | 37,901     | 64,441  |
| 100 neighbors                 | 73,991              | 71,730                | 84,198     | 212,920 |
| 10 neighbors                  | 289,142             | 332,557               | 269,419    | 355,260 |
| 1 neighbor                    | 422,051             | 337,892               | 267,394    | 379,643 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 14,430 (0.000s)     | 38,910 (0.000s)       | 28,490 (0.000s) | 20,920 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 514,625             | 516,104               | 513,946    | 72,412  |
| Half (~span/4) (r=2.25)    | 436,730             | 437,249               | 256,846    | 240,260 |
| Quarter (~span/8) (r=1.13) | 433,836             | 442,181               | 603,799    | 347,405 |
| Tiny (~span/1000) (r=1)    | 435,569             | 442,365               | 606,463    | 348,127 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,782,142           | 2,784,506             | 2,914,728  | 223,030 |
| Half (size=4.5x4.5)      | 578,978             | 569,871               | 367,206    | 378,181 |
| Quarter (size=2.25x2.25) | 606,386             | 603,748               | 800,963    | 404,449 |
| Unit (size=1)            | 605,585             | 598,488               | 809,381    | 402,838 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 141,447             | 140,032               | 167,130    | 239,554 |
| 10 neighbors                  | 314,031             | 302,932               | 396,364    | 438,315 |
| 1 neighbor                    | 355,337             | 462,238               | 426,645    | 488,583 |

<!-- tabs:end -->
<!-- SPATIAL_TREE_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**QuadTree2D**:

- Best for: General-purpose 2D spatial queries
- Strengths: Balanced performance across all operation types, simple to use
- Weaknesses: Slightly slower than KdTree for point queries

**KdTree2D (Balanced)**:

- Best for: When you need consistent query performance
- Strengths: Fast nearest-neighbor queries, good for smaller datasets
- Weaknesses: Slower construction time

**KdTree2D (Unbalanced)**:

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
