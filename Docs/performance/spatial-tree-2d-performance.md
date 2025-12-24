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
| 1,000,000 entries | 2 (0.350s)          | 6 (0.166s)            | 4 (0.225s) | 2 (0.346s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 57                  | 56                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 227                 | 231                   | 215        | 27      |
| Quarter (~span/8) (r=124.9) | 932                 | 943                   | 801        | 113     |
| Tiny (~span/1000) (r=1)     | 103,885             | 106,364               | 144,278    | 106,765 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 275                 | 280                   | 243        | 17      |
| Half (size=499.5x499.5)    | 1,789               | 1,734                 | 1,029      | 69      |
| Quarter (size=249.8x249.8) | 7,170               | 6,974                 | 3,827      | 378     |
| Unit (size=1)              | 147,048             | 154,155               | 194,354    | 114,147 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,118               | 16,572                | 12,220     | 65,543  |
| 100 neighbors                 | 73,766              | 65,473                | 73,848     | 150,518 |
| 10 neighbors                  | 263,078             | 251,428               | 189,627    | 225,627 |
| 1 neighbor                    | 378,383             | 373,203               | 226,578    | 239,091 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 48 (0.021s)         | 83 (0.012s)           | 50 (0.020s) | 51 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 591                 | 592                   | 601        | 72      |
| Half (~span/4) (r=99.75)    | 1,344               | 1,322                 | 1,247      | 182     |
| Quarter (~span/8) (r=49.88) | 4,630               | 5,048                 | 4,282      | 720     |
| Tiny (~span/1000) (r=1)     | 125,459             | 127,283               | 179,088    | 146,454 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,481               | 4,499                 | 4,488      | 197     |
| Half (size=199.5x124.5)    | 9,437               | 11,634                | 7,874      | 950     |
| Quarter (size=99.75x62.25) | 24,952              | 32,154                | 19,459     | 3,764   |
| Unit (size=1)              | 181,877             | 186,681               | 246,664    | 157,002 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,628               | 9,809                 | 11,469     | 64,429  |
| 100 neighbors                 | 46,856              | 86,353                | 52,888     | 187,712 |
| 10 neighbors                  | 294,430             | 260,057               | 213,617    | 265,586 |
| 1 neighbor                    | 276,672             | 392,347               | 261,260    | 277,235 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 527 (0.002s)        | 52 (0.019s)           | 540 (0.002s) | 508 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,782               | 5,894                 | 5,883      | 724     |
| Half (~span/4) (r=24.75)    | 21,945              | 22,098                | 13,657     | 2,898   |
| Quarter (~span/8) (r=12.38) | 43,969              | 51,336                | 37,269     | 12,132  |
| Tiny (~span/1000) (r=1)     | 169,416             | 160,477               | 233,808    | 170,243 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 44,915              | 44,616                | 46,033     | 2,411   |
| Half (size=49.50x49.50)    | 164,872             | 145,514               | 36,213     | 9,215   |
| Quarter (size=24.75x24.75) | 74,420              | 103,097               | 73,934     | 35,415  |
| Unit (size=1)              | 239,962             | 235,129               | 320,130    | 183,138 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,872              | 12,613                | 14,259     | 61,279  |
| 100 neighbors                 | 59,035              | 50,926                | 88,104     | 192,234 |
| 10 neighbors                  | 217,328             | 268,091               | 221,101    | 304,254 |
| 1 neighbor                    | 271,073             | 412,440               | 289,311    | 329,455 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,213 (0.000s)      | 7,087 (0.000s)        | 4,612 (0.000s) | 1,459 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,293              | 58,319                | 56,888     | 7,305   |
| Half (~span/4) (r=12.25)   | 60,382              | 75,873                | 56,345     | 14,733  |
| Quarter (~span/8) (r=6.13) | 95,360              | 108,873               | 96,156     | 37,599  |
| Tiny (~span/1000) (r=1)    | 241,573             | 236,614               | 339,206    | 256,105 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 504,685             | 505,787               | 544,198    | 23,886  |
| Half (size=24.50x9.5)     | 166,614             | 296,150               | 126,173    | 74,498  |
| Quarter (size=12.25x4.75) | 273,454             | 292,699               | 194,012    | 179,601 |
| Unit (size=1)             | 342,019             | 342,961               | 464,482    | 286,299 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 43,908              | 45,249                | 38,601     | 65,022  |
| 100 neighbors                 | 74,933              | 73,237                | 85,506     | 215,985 |
| 10 neighbors                  | 298,123             | 335,949               | 272,917    | 365,087 |
| 1 neighbor                    | 432,841             | 334,802               | 249,283    | 390,381 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 20,202 (0.000s)     | 33,444 (0.000s)       | 31,250 (0.000s) | 17,699 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 516,784             | 519,089               | 517,737    | 72,701  |
| Half (~span/4) (r=2.25)    | 437,843             | 446,595               | 259,567    | 239,418 |
| Quarter (~span/8) (r=1.13) | 439,854             | 442,716               | 618,578    | 348,256 |
| Tiny (~span/1000) (r=1)    | 440,868             | 440,805               | 616,016    | 352,428 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,812,554           | 2,716,484             | 2,880,080  | 225,043 |
| Half (size=4.5x4.5)      | 588,305             | 566,261               | 366,606    | 386,618 |
| Quarter (size=2.25x2.25) | 608,432             | 614,969               | 805,849    | 414,025 |
| Unit (size=1)            | 609,703             | 614,688               | 801,412    | 413,183 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 141,215             | 144,111               | 166,861    | 245,597 |
| 10 neighbors                  | 340,221             | 306,899               | 407,919    | 452,520 |
| 1 neighbor                    | 352,049             | 475,023               | 394,136    | 492,072 |

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
