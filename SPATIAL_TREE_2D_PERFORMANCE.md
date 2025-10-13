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
| 1,000,000 entries | 4 (0.246s)          | 4 (0.224s)            | 3 (0.265s) | 3 (0.284s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 238                 | 235                   | 214        | 28      |
| Quarter (~span/8) (r=124.9) | 947                 | 937                   | 812        | 119     |
| Tiny (~span/1000) (r=1)     | 102,826             | 104,147               | 143,155    | 106,922 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 375                 | 376                   | 299        | 17      |
| Half (size=499.5x499.5)    | 1,748               | 1,734                 | 1,212      | 82      |
| Quarter (size=249.8x249.8) | 6,948               | 7,069                 | 3,770      | 378     |
| Unit (size=1)              | 147,771             | 151,269               | 195,483    | 103,640 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,432               | 16,941                | 12,659     | 63,928  |
| 100 neighbors                 | 79,012              | 76,134                | 79,369     | 151,190 |
| 10 neighbors                  | 354,172             | 336,281               | 253,028    | 235,497 |
| 1 neighbor                    | 532,502             | 534,697               | 263,337    | 293,872 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 48 (0.021s)         | 78 (0.013s)           | 48 (0.020s) | 47 (0.021s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 594                 | 595                   | 593        | 71      |
| Half (~span/4) (r=99.75)    | 1,351               | 1,343                 | 1,233      | 184     |
| Quarter (~span/8) (r=49.88) | 4,671               | 5,126                 | 4,259      | 723     |
| Tiny (~span/1000) (r=1)     | 127,392             | 126,389               | 176,895    | 144,540 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,421               | 4,503                 | 4,627      | 224     |
| Half (size=199.5x124.5)    | 9,403               | 11,733                | 7,704      | 944     |
| Quarter (size=99.75x62.25) | 25,758              | 31,652                | 18,828     | 3,742   |
| Unit (size=1)              | 180,077             | 181,814               | 233,505    | 152,910 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,927               | 9,842                 | 11,635     | 69,488  |
| 100 neighbors                 | 49,376              | 92,059                | 54,962     | 231,627 |
| 10 neighbors                  | 441,663             | 355,038               | 293,832    | 351,079 |
| 1 neighbor                    | 497,313             | 544,855               | 300,128    | 361,623 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 353 (0.003s)        | 801 (0.001s)          | 544 (0.002s) | 492 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,935               | 5,879                 | 5,944      | 732     |
| Half (~span/4) (r=24.75)    | 22,254              | 22,068                | 13,875     | 2,919   |
| Quarter (~span/8) (r=12.38) | 43,998              | 50,921                | 38,084     | 12,201  |
| Tiny (~span/1000) (r=1)     | 155,617             | 160,627               | 231,057    | 167,961 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 44,596              | 44,865                | 45,270     | 2,414   |
| Half (size=49.50x49.50)    | 164,082             | 143,516               | 35,724     | 9,281   |
| Quarter (size=24.75x24.75) | 73,832              | 100,527               | 70,871     | 35,446  |
| Unit (size=1)              | 235,343             | 229,783               | 298,780    | 180,227 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,121              | 12,981                | 13,535     | 65,370  |
| 100 neighbors                 | 61,965              | 56,598                | 95,886     | 234,525 |
| 10 neighbors                  | 415,886             | 374,183               | 316,122    | 417,759 |
| 1 neighbor                    | 568,132             | 638,935               | 352,718    | 411,584 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,205 (0.000s)      | 3,790 (0.000s)        | 4,833 (0.000s) | 4,401 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,096              | 57,658                | 56,538     | 7,385   |
| Half (~span/4) (r=12.25)   | 60,182              | 76,032                | 56,614     | 14,679  |
| Quarter (~span/8) (r=6.13) | 95,596              | 106,311               | 94,617     | 37,986  |
| Tiny (~span/1000) (r=1)    | 238,748             | 228,920               | 333,022    | 251,146 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 414,739             | 470,320               | 515,549    | 24,063  |
| Half (size=24.50x9.5)     | 164,697             | 287,309               | 120,277    | 74,699  |
| Quarter (size=12.25x4.75) | 267,097             | 285,429               | 182,478    | 175,655 |
| Unit (size=1)             | 338,316             | 334,893               | 415,411    | 279,184 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,284              | 47,202                | 40,221     | 70,459  |
| 100 neighbors                 | 80,808              | 78,302                | 92,322     | 273,121 |
| 10 neighbors                  | 474,758             | 538,241               | 425,873    | 480,648 |
| 1 neighbor                    | 750,345             | 599,094               | 387,663    | 589,145 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 37,593 (0.000s)     | 35,714 (0.000s)       | 27,472 (0.000s) | 22,522 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 500,291             | 502,276               | 507,569    | 72,648  |
| Half (~span/4) (r=2.25)    | 427,794             | 432,046               | 256,551    | 236,627 |
| Quarter (~span/8) (r=1.13) | 427,304             | 432,189               | 592,069    | 338,168 |
| Tiny (~span/1000) (r=1)    | 418,333             | 431,984               | 546,375    | 338,262 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,211,065           | 2,397,486             | 2,468,162  | 222,527 |
| Half (size=4.5x4.5)      | 564,152             | 485,481               | 348,572    | 366,318 |
| Quarter (size=2.25x2.25) | 585,009             | 519,317               | 754,164    | 359,418 |
| Unit (size=1)            | 585,765             | 503,496               | 754,022    | 392,014 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 164,874             | 154,248               | 194,218    | 322,083 |
| 10 neighbors                  | 576,064             | 513,406               | 590,270    | 731,734 |
| 1 neighbor                    | 586,842             | 718,754               | 631,970    | 803,155 |

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
