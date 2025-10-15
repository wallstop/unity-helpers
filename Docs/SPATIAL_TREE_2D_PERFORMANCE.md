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
| 1,000,000 entries | 3 (0.325s)          | 4 (0.218s)            | 4 (0.221s) | 1 (0.595s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 58                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 236                 | 238                   | 215        | 28      |
| Quarter (~span/8) (r=124.9) | 945                 | 946                   | 815        | 119     |
| Tiny (~span/1000) (r=1)     | 103,233             | 105,580               | 142,838    | 107,126 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 314                 | 325                   | 344        | 17      |
| Half (size=499.5x499.5)    | 1,730               | 1,822                 | 1,238      | 73      |
| Quarter (size=249.8x249.8) | 7,188               | 7,170                 | 3,867      | 379     |
| Unit (size=1)              | 149,753             | 153,363               | 197,026    | 113,010 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,466               | 16,873                | 12,696     | 70,046  |
| 100 neighbors                 | 78,952              | 76,476                | 78,720     | 171,372 |
| 10 neighbors                  | 375,484             | 377,708               | 251,988    | 277,491 |
| 1 neighbor                    | 551,002             | 508,239               | 276,379    | 299,010 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 50 (0.020s)         | 83 (0.012s)           | 50 (0.020s) | 49 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 602                 | 602                   | 600        | 73      |
| Half (~span/4) (r=99.75)    | 1,355               | 1,359                 | 1,248      | 185     |
| Quarter (~span/8) (r=49.88) | 4,672               | 5,178                 | 4,300      | 723     |
| Tiny (~span/1000) (r=1)     | 127,810             | 128,060               | 179,544    | 145,029 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,436               | 4,468                 | 4,702      | 235     |
| Half (size=199.5x124.5)    | 9,433               | 11,669                | 7,996      | 967     |
| Quarter (size=99.75x62.25) | 25,419              | 32,186                | 19,754     | 3,805   |
| Unit (size=1)              | 184,712             | 185,636               | 245,390    | 155,220 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,986               | 9,929                 | 11,705     | 69,966  |
| 100 neighbors                 | 49,779              | 87,233                | 55,229     | 233,211 |
| 10 neighbors                  | 470,175             | 394,286               | 294,299    | 352,517 |
| 1 neighbor                    | 509,623             | 544,160               | 314,046    | 350,724 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 533 (0.002s)        | 796 (0.001s)          | 408 (0.002s) | 507 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,944               | 5,941                 | 5,936      | 735     |
| Half (~span/4) (r=24.75)    | 22,272              | 22,233                | 13,861     | 2,919   |
| Quarter (~span/8) (r=12.38) | 44,278              | 51,526                | 38,041     | 12,217  |
| Tiny (~span/1000) (r=1)     | 167,798             | 162,582               | 223,371    | 168,224 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 45,222              | 44,244                | 47,466     | 2,412   |
| Half (size=49.50x49.50)    | 167,135             | 166,097               | 36,985     | 9,274   |
| Quarter (size=24.75x24.75) | 76,007              | 104,425               | 74,227     | 35,573  |
| Unit (size=1)              | 242,092             | 234,546               | 320,164    | 181,677 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,178              | 12,888                | 14,441     | 65,299  |
| 100 neighbors                 | 62,415              | 57,120                | 96,738     | 235,553 |
| 10 neighbors                  | 418,552             | 392,340               | 316,886    | 420,625 |
| 1 neighbor                    | 595,519             | 632,000               | 372,667    | 433,485 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,672 (0.000s)      | 5,611 (0.000s)        | 4,847 (0.000s) | 4,764 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,142              | 56,029                | 57,082     | 7,394   |
| Half (~span/4) (r=12.25)   | 60,439              | 76,572                | 57,123     | 14,698  |
| Quarter (~span/8) (r=6.13) | 95,988              | 108,457               | 95,703     | 37,920  |
| Tiny (~span/1000) (r=1)    | 239,460             | 237,275               | 333,071    | 251,518 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 499,193             | 498,444               | 508,596    | 24,141  |
| Half (size=24.50x9.5)     | 167,532             | 291,283               | 126,274    | 75,139  |
| Quarter (size=12.25x4.75) | 271,878             | 288,421               | 194,135    | 176,768 |
| Unit (size=1)             | 342,402             | 339,166               | 460,883    | 281,383 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,434              | 47,214                | 40,048     | 70,543  |
| 100 neighbors                 | 80,837              | 77,933                | 91,884     | 267,760 |
| 10 neighbors                  | 487,252             | 574,172               | 426,710    | 516,096 |
| 1 neighbor                    | 754,604             | 618,120               | 404,966    | 594,659 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 37,174 (0.000s)     | 3,938 (0.000s)        | 10,384 (0.000s) | 21,978 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 505,197             | 462,582               | 480,895    | 72,679  |
| Half (~span/4) (r=2.25)    | 428,579             | 432,700               | 256,227    | 233,286 |
| Quarter (~span/8) (r=1.13) | 422,859             | 432,362               | 601,655    | 318,360 |
| Tiny (~span/1000) (r=1)    | 406,482             | 433,055               | 601,161    | 339,515 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,460,211           | 2,459,261             | 2,542,373  | 223,694 |
| Half (size=4.5x4.5)      | 572,726             | 561,349               | 365,082    | 372,098 |
| Quarter (size=2.25x2.25) | 594,157             | 580,857               | 738,232    | 396,288 |
| Unit (size=1)            | 593,669             | 576,995               | 787,477    | 395,564 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 166,728             | 165,861               | 199,072    | 303,148 |
| 10 neighbors                  | 610,079             | 520,568               | 639,716    | 735,646 |
| 1 neighbor                    | 457,409             | 765,967               | 667,953    | 818,651 |

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
