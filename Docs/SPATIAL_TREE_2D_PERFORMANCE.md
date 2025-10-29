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
| 1,000,000 entries | 4 (0.250s)          | 6 (0.157s)            | 4 (0.225s) | 2 (0.381s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 237                 | 235                   | 202        | 28      |
| Quarter (~span/8) (r=124.9) | 946                 | 946                   | 814        | 119     |
| Tiny (~span/1000) (r=1)     | 102,008             | 104,935               | 143,720    | 98,953  |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 331                 | 351                   | 303        | 17      |
| Half (size=499.5x499.5)    | 1,828               | 1,766                 | 1,217      | 70      |
| Quarter (size=249.8x249.8) | 7,164               | 7,134                 | 3,808      | 377     |
| Unit (size=1)              | 149,358             | 151,953               | 197,162    | 112,191 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,482               | 16,823                | 12,695     | 69,782  |
| 100 neighbors                 | 79,116              | 71,117                | 79,415     | 175,970 |
| 10 neighbors                  | 399,482             | 373,130               | 232,646    | 282,345 |
| 1 neighbor                    | 511,008             | 505,967               | 292,578    | 297,101 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 49 (0.020s)         | 63 (0.016s)           | 50 (0.020s) | 50 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 602                 | 601                   | 600        | 72      |
| Half (~span/4) (r=99.75)    | 1,354               | 1,351                 | 1,241      | 185     |
| Quarter (~span/8) (r=49.88) | 4,667               | 5,170                 | 4,260      | 722     |
| Tiny (~span/1000) (r=1)     | 123,736             | 126,456               | 174,629    | 144,743 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,590               | 4,455                 | 4,617      | 229     |
| Half (size=199.5x124.5)    | 9,551               | 12,062                | 7,965      | 965     |
| Quarter (size=99.75x62.25) | 25,406              | 32,599                | 19,560     | 3,783   |
| Unit (size=1)              | 179,410             | 176,458               | 244,992    | 154,883 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,951               | 9,880                 | 11,683     | 68,821  |
| 100 neighbors                 | 48,491              | 92,416                | 55,715     | 230,944 |
| 10 neighbors                  | 428,017             | 391,020               | 297,228    | 349,318 |
| 1 neighbor                    | 461,228             | 539,537               | 314,401    | 344,539 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 539 (0.002s)        | 803 (0.001s)          | 543 (0.002s) | 498 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,932               | 5,940                 | 5,943      | 732     |
| Half (~span/4) (r=24.75)    | 22,254              | 22,224                | 13,758     | 2,919   |
| Quarter (~span/8) (r=12.38) | 44,134              | 51,253                | 37,539     | 12,131  |
| Tiny (~span/1000) (r=1)     | 167,065             | 162,025               | 229,104    | 167,595 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 45,003              | 45,095                | 45,914     | 2,402   |
| Half (size=49.50x49.50)    | 155,630             | 145,029               | 36,146     | 9,272   |
| Quarter (size=24.75x24.75) | 75,618              | 104,381               | 74,014     | 34,737  |
| Unit (size=1)              | 238,856             | 232,757               | 313,827    | 180,703 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,157              | 12,999                | 14,384     | 65,442  |
| 100 neighbors                 | 62,108              | 57,245                | 91,901     | 235,648 |
| 10 neighbors                  | 408,037             | 392,049               | 318,500    | 418,230 |
| 1 neighbor                    | 590,282             | 651,854               | 376,786    | 458,203 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,607 (0.000s)      | 7,757 (0.000s)        | 4,526 (0.000s) | 2,362 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,055              | 58,163                | 57,007     | 7,378   |
| Half (~span/4) (r=12.25)   | 60,025              | 76,361                | 56,188     | 14,603  |
| Quarter (~span/8) (r=6.13) | 94,844              | 108,276               | 93,708     | 37,476  |
| Tiny (~span/1000) (r=1)    | 235,068             | 236,032               | 331,324    | 235,049 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 471,998             | 414,246               | 537,930    | 24,062  |
| Half (size=24.50x9.5)     | 165,972             | 291,164               | 125,941    | 74,608  |
| Quarter (size=12.25x4.75) | 268,906             | 283,248               | 193,155    | 176,324 |
| Unit (size=1)             | 339,160             | 326,826               | 456,508    | 280,647 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 45,922              | 44,937                | 40,386     | 70,300  |
| 100 neighbors                 | 79,958              | 77,481                | 92,254     | 272,283 |
| 10 neighbors                  | 514,066             | 594,727               | 391,641    | 536,920 |
| 1 neighbor                    | 700,759             | 560,659               | 424,317    | 549,469 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 41,841 (0.000s)     | 34,129 (0.000s)       | 27,173 (0.000s) | 20,964 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 506,920             | 507,531               | 507,567    | 72,229  |
| Half (~span/4) (r=2.25)    | 429,117             | 434,290               | 246,528    | 233,413 |
| Quarter (~span/8) (r=1.13) | 429,347             | 433,959               | 463,226    | 332,555 |
| Tiny (~span/1000) (r=1)    | 429,433             | 433,836               | 524,827    | 332,750 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,165,006           | 2,237,350             | 2,472,290  | 221,763 |
| Half (size=4.5x4.5)      | 569,870             | 555,960               | 361,468    | 339,590 |
| Quarter (size=2.25x2.25) | 576,260             | 594,071               | 781,205    | 388,833 |
| Unit (size=1)            | 562,212             | 595,488               | 780,229    | 394,094 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 165,360             | 165,141               | 186,729    | 321,180 |
| 10 neighbors                  | 634,717             | 382,634               | 623,164    | 699,117 |
| 1 neighbor                    | 605,535             | 666,924               | 642,836    | 858,205 |

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
