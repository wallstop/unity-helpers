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
| 1,000,000 entries | 4 (0.243s)          | 4 (0.207s)            | 4 (0.245s) | 1 (0.617s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 59                    | 55         | 7       |
| Half (~span/4) (r=249.8)    | 238                 | 237                   | 204        | 28      |
| Quarter (~span/8) (r=124.9) | 946                 | 946                   | 815        | 119     |
| Tiny (~span/1000) (r=1)     | 103,114             | 105,599               | 143,605    | 107,881 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 387                 | 387                   | 260        | 18      |
| Half (size=499.5x499.5)    | 1,743               | 1,783                 | 1,225      | 78      |
| Quarter (size=249.8x249.8) | 6,999               | 7,184                 | 3,830      | 379     |
| Unit (size=1)              | 149,073             | 153,060               | 197,411    | 112,249 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,393               | 16,831                | 12,443     | 69,035  |
| 100 neighbors                 | 79,169              | 76,070                | 73,196     | 175,174 |
| 10 neighbors                  | 375,437             | 347,050               | 210,299    | 283,034 |
| 1 neighbor                    | 548,229             | 537,303               | 236,159    | 296,044 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 30 (0.033s)         | 83 (0.012s)           | 50 (0.020s) | 52 (0.019s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 601                 | 601                   | 601        | 75      |
| Half (~span/4) (r=99.75)    | 1,355               | 1,360                 | 1,247      | 186     |
| Quarter (~span/8) (r=49.88) | 4,647               | 5,176                 | 4,308      | 724     |
| Tiny (~span/1000) (r=1)     | 123,882             | 127,973               | 178,801    | 146,439 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,564               | 4,426                 | 4,528      | 236     |
| Half (size=199.5x124.5)    | 9,936               | 11,933                | 7,900      | 968     |
| Quarter (size=99.75x62.25) | 25,355              | 34,261                | 19,666     | 3,807   |
| Unit (size=1)              | 183,758             | 185,397               | 243,205    | 154,754 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,954               | 9,765                 | 11,488     | 68,918  |
| 100 neighbors                 | 49,784              | 92,586                | 54,095     | 228,430 |
| 10 neighbors                  | 495,765             | 392,500               | 232,811    | 330,114 |
| 1 neighbor                    | 474,000             | 548,240               | 276,662    | 373,417 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 547 (0.002s)        | 834 (0.001s)          | 400 (0.002s) | 495 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,936               | 5,940                 | 5,937      | 728     |
| Half (~span/4) (r=24.75)    | 22,308              | 22,212                | 13,883     | 2,898   |
| Quarter (~span/8) (r=12.38) | 44,216              | 51,466                | 38,049     | 12,112  |
| Tiny (~span/1000) (r=1)     | 167,797             | 162,536               | 225,225    | 167,319 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 43,812              | 45,320                | 45,943     | 2,393   |
| Half (size=49.50x49.50)    | 165,611             | 167,277               | 37,147     | 9,201   |
| Quarter (size=24.75x24.75) | 75,380              | 103,887               | 74,913     | 35,161  |
| Unit (size=1)              | 240,461             | 232,011               | 316,878    | 179,116 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,093              | 13,040                | 14,300     | 64,499  |
| 100 neighbors                 | 61,778              | 56,766                | 92,858     | 234,910 |
| 10 neighbors                  | 384,629             | 375,996               | 277,725    | 418,141 |
| 1 neighbor                    | 570,984             | 631,087               | 315,877    | 458,622 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,068 (0.000s)      | 8,012 (0.000s)        | 4,892 (0.000s) | 4,601 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 57,446              | 57,835                | 57,233     | 7,336   |
| Half (~span/4) (r=12.25)   | 59,633              | 76,664                | 56,713     | 14,575  |
| Quarter (~span/8) (r=6.13) | 94,257              | 108,779               | 94,736     | 37,945  |
| Tiny (~span/1000) (r=1)    | 234,339             | 237,770               | 336,003    | 248,171 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 470,407             | 412,267               | 538,202    | 24,028  |
| Half (size=24.50x9.5)     | 165,816             | 290,631               | 126,258    | 74,325  |
| Quarter (size=12.25x4.75) | 268,875             | 284,067               | 192,865    | 167,079 |
| Unit (size=1)             | 339,691             | 328,876               | 456,878    | 277,918 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 45,750              | 42,976                | 39,035     | 69,433  |
| 100 neighbors                 | 81,044              | 75,353                | 88,211     | 270,879 |
| 10 neighbors                  | 519,642             | 601,494               | 355,438    | 532,110 |
| 1 neighbor                    | 703,159             | 575,482               | 385,585    | 546,455 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 43,103 (0.000s)     | 37,453 (0.000s)       | 27,322 (0.000s) | 23,640 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 505,251             | 506,757               | 504,329    | 71,963  |
| Half (~span/4) (r=2.25)    | 430,851             | 430,658               | 256,517    | 235,075 |
| Quarter (~span/8) (r=1.13) | 430,724             | 430,736               | 601,355    | 338,153 |
| Tiny (~span/1000) (r=1)    | 430,754             | 430,284               | 598,471    | 340,692 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,448,546           | 2,259,205             | 2,137,171  | 223,802 |
| Half (size=4.5x4.5)      | 567,838             | 557,169               | 365,308    | 370,472 |
| Quarter (size=2.25x2.25) | 583,946             | 592,479               | 788,572    | 393,465 |
| Unit (size=1)            | 556,276             | 593,260               | 788,937    | 392,330 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 158,290             | 155,464               | 200,721    | 301,513 |
| 10 neighbors                  | 605,208             | 436,057               | 535,377    | 756,820 |
| 1 neighbor                    | 573,182             | 536,531               | 597,137    | 799,246 |

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
