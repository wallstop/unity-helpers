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
<!-- tabs:start -->

### 1,000,000 entries

#### Construction

| Construction      | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D    |
| ----------------- | ------------------- | --------------------- | ---------- | ---------- |
| 1,000,000 entries | 4 (0.244s)          | 6 (0.153s)            | 4 (0.222s) | 2 (0.386s) |

#### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 236                 | 236                   | 217        | 28      |
| Quarter (~span/8) (r=124.9) | 946                 | 945                   | 816        | 119     |
| Tiny (~span/1000) (r=1)     | 101,504             | 105,285               | 142,984    | 106,816 |

#### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 380                 | 382                   | 344        | 17      |
| Half (size=499.5x499.5)    | 1,851               | 1,843                 | 1,236      | 75      |
| Quarter (size=249.8x249.8) | 7,483               | 7,248                 | 3,887      | 377     |
| Unit (size=1)              | 147,825             | 152,474               | 196,544    | 110,185 |

#### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,454               | 16,901                | 12,715     | 69,581  |
| 100 neighbors                 | 77,855              | 76,370                | 78,317     | 175,585 |
| 10 neighbors                  | 377,857             | 352,657               | 225,271    | 281,743 |
| 1 neighbor                    | 516,596             | 504,358               | 276,636    | 296,533 |

### 100,000 entries

#### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 50 (0.020s)         | 83 (0.012s)           | 50 (0.020s) | 50 (0.020s) |

#### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 601                 | 601                   | 601        | 74      |
| Half (~span/4) (r=99.75)    | 1,353               | 1,358                 | 1,248      | 186     |
| Quarter (~span/8) (r=49.88) | 4,667               | 5,176                 | 4,299      | 724     |
| Tiny (~span/1000) (r=1)     | 127,432             | 127,776               | 178,260    | 145,496 |

#### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,571               | 4,588                 | 4,675      | 232     |
| Half (size=199.5x124.5)    | 9,686               | 12,037                | 8,043      | 973     |
| Quarter (size=99.75x62.25) | 25,538              | 33,097                | 19,916     | 3,832   |
| Unit (size=1)              | 182,094             | 183,264               | 244,353    | 154,606 |

#### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,965               | 9,922                 | 11,666     | 69,077  |
| 100 neighbors                 | 49,164              | 92,888                | 55,324     | 216,799 |
| 10 neighbors                  | 474,916             | 390,202               | 265,444    | 350,326 |
| 1 neighbor                    | 474,933             | 543,573               | 323,662    | 372,714 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 537 (0.002s)        | 801 (0.001s)          | 542 (0.002s) | 504 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,935               | 5,938                 | 5,944      | 735     |
| Half (~span/4) (r=24.75)    | 22,275              | 22,241                | 13,866     | 2,926   |
| Quarter (~span/8) (r=12.38) | 44,200              | 51,396                | 38,060     | 12,233  |
| Tiny (~span/1000) (r=1)     | 166,755             | 161,974               | 232,843    | 168,516 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 46,195              | 46,083                | 46,681     | 2,426   |
| Half (size=49.50x49.50)    | 167,388             | 148,478               | 36,590     | 9,339   |
| Quarter (size=24.75x24.75) | 75,940              | 104,014               | 75,785     | 35,720  |
| Unit (size=1)              | 239,397             | 231,168               | 318,259    | 179,268 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,166              | 13,003                | 14,458     | 64,265  |
| 100 neighbors                 | 62,425              | 56,807                | 96,006     | 221,880 |
| 10 neighbors                  | 395,764             | 419,775               | 284,603    | 415,788 |
| 1 neighbor                    | 597,510             | 602,338               | 369,442    | 459,139 |

### 1,000 entries

#### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,327 (0.000s)      | 7,739 (0.000s)        | 4,952 (0.000s) | 4,764 (0.000s) |

#### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,072              | 57,956                | 57,377     | 7,419   |
| Half (~span/4) (r=12.25)   | 60,307              | 76,412                | 57,036     | 14,715  |
| Quarter (~span/8) (r=6.13) | 95,578              | 108,397               | 95,395     | 38,031  |
| Tiny (~span/1000) (r=1)    | 238,625             | 236,418               | 334,236    | 251,174 |

#### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 496,607             | 498,142               | 539,878    | 24,229  |
| Half (size=24.50x9.5)     | 165,447             | 287,325               | 127,121    | 75,159  |
| Quarter (size=12.25x4.75) | 255,238             | 273,498               | 194,690    | 176,327 |
| Unit (size=1)             | 338,275             | 335,033               | 453,237    | 279,167 |

#### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,525              | 47,120                | 38,307     | 69,953  |
| 100 neighbors                 | 80,804              | 78,180                | 92,282     | 268,937 |
| 10 neighbors                  | 520,835             | 601,494               | 416,276    | 506,668 |
| 1 neighbor                    | 725,705             | 581,979               | 388,981    | 591,498 |

### 100 entries

#### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 42,553 (0.000s)     | 21,929 (0.000s)       | 29,325 (0.000s) | 21,786 (0.000s) |

#### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 504,629             | 463,895               | 509,215    | 72,746  |
| Half (~span/4) (r=2.25)    | 425,242             | 430,575               | 256,046    | 236,805 |
| Quarter (~span/8) (r=1.13) | 425,958             | 430,848               | 593,604    | 338,080 |
| Tiny (~span/1000) (r=1)    | 424,528             | 430,448               | 591,691    | 338,365 |

#### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,269,583           | 2,434,901             | 2,501,705  | 224,429 |
| Half (size=4.5x4.5)      | 566,944             | 555,712               | 360,504    | 369,394 |
| Quarter (size=2.25x2.25) | 592,102             | 565,569               | 753,693    | 390,919 |
| Unit (size=1)            | 592,678             | 595,395               | 784,153    | 386,053 |

#### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 165,219             | 164,632               | 200,629    | 306,317 |
| 10 neighbors                  | 610,304             | 516,470               | 619,678    | 760,800 |
| 1 neighbor                    | 499,393             | 737,678               | 642,498    | 823,829 |

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
