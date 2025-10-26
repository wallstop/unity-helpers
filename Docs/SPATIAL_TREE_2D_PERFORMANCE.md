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
| 1,000,000 entries | 4 (0.247s)          | 6 (0.158s)            | 3 (0.260s) | 2 (0.371s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 57                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 238                 | 237                   | 212        | 28      |
| Quarter (~span/8) (r=124.9) | 946                 | 947                   | 810        | 117     |
| Tiny (~span/1000) (r=1)     | 98,434              | 105,450               | 143,326    | 100,262 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 403                 | 424                   | 408        | 17      |
| Half (size=499.5x499.5)    | 1,801               | 1,805                 | 1,233      | 86      |
| Quarter (size=249.8x249.8) | 6,931               | 6,919                 | 3,796      | 378     |
| Unit (size=1)              | 144,441             | 147,676               | 190,607    | 108,770 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,215               | 16,455                | 12,373     | 66,023  |
| 100 neighbors                 | 71,589              | 76,394                | 76,066     | 175,563 |
| 10 neighbors                  | 396,400             | 353,706               | 232,223    | 282,978 |
| 1 neighbor                    | 515,059             | 521,836               | 289,193    | 297,522 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 49 (0.020s)         | 84 (0.012s)           | 49 (0.020s) | 49 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 602                 | 584                   | 601        | 72      |
| Half (~span/4) (r=99.75)    | 1,356               | 1,315                 | 1,248      | 184     |
| Quarter (~span/8) (r=49.88) | 4,666               | 5,014                 | 4,227      | 722     |
| Tiny (~span/1000) (r=1)     | 123,692             | 124,176               | 177,014    | 144,611 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,397               | 4,479                 | 4,616      | 235     |
| Half (size=199.5x124.5)    | 9,286               | 11,932                | 8,002      | 967     |
| Quarter (size=99.75x62.25) | 24,540              | 32,163                | 19,726     | 3,738   |
| Unit (size=1)              | 183,028             | 184,582               | 244,412    | 152,512 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,966               | 9,639                 | 11,541     | 69,208  |
| 100 neighbors                 | 49,796              | 90,353                | 53,438     | 230,407 |
| 10 neighbors                  | 466,552             | 387,689               | 277,034    | 345,328 |
| 1 neighbor                    | 473,688             | 532,273               | 307,580    | 352,576 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 529 (0.002s)        | 815 (0.001s)          | 455 (0.002s) | 512 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,942               | 5,947                 | 5,797      | 734     |
| Half (~span/4) (r=24.75)    | 22,301              | 22,284                | 13,857     | 2,923   |
| Quarter (~span/8) (r=12.38) | 44,114              | 51,456                | 36,995     | 12,213  |
| Tiny (~span/1000) (r=1)     | 167,221             | 162,154               | 233,341    | 166,907 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 45,269              | 43,416                | 46,564     | 2,420   |
| Half (size=49.50x49.50)    | 167,232             | 141,687               | 36,664     | 9,265   |
| Quarter (size=24.75x24.75) | 75,491              | 101,011               | 73,844     | 34,489  |
| Unit (size=1)              | 240,188             | 227,996               | 309,160    | 174,684 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,200              | 13,025                | 14,094     | 62,841  |
| 100 neighbors                 | 62,347              | 57,224                | 94,155     | 234,789 |
| 10 neighbors                  | 417,844             | 385,871               | 306,896    | 415,602 |
| 1 neighbor                    | 597,726             | 605,838               | 372,639    | 428,203 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,083 (0.000s)      | 5,184 (0.000s)        | 5,015 (0.000s) | 4,734 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,157              | 57,216                | 55,735     | 7,410   |
| Half (~span/4) (r=12.25)   | 60,401              | 74,867                | 55,646     | 14,536  |
| Quarter (~span/8) (r=6.13) | 95,855              | 104,856               | 92,720     | 36,861  |
| Tiny (~span/1000) (r=1)    | 238,667             | 226,635               | 330,912    | 246,972 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 499,140             | 499,234               | 428,783    | 24,121  |
| Half (size=24.50x9.5)     | 166,826             | 285,095               | 125,435    | 74,946  |
| Quarter (size=12.25x4.75) | 269,508             | 278,944               | 192,644    | 175,535 |
| Unit (size=1)             | 339,865             | 325,878               | 430,380    | 279,436 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 44,952              | 44,992                | 39,051     | 70,210  |
| 100 neighbors                 | 80,437              | 75,964                | 89,052     | 271,927 |
| 10 neighbors                  | 489,189             | 560,687               | 410,907    | 497,753 |
| 1 neighbor                    | 754,167             | 586,966               | 400,770    | 571,875 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 39,682 (0.000s)     | 24,213 (0.000s)       | 25,839 (0.000s) | 21,598 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 502,463             | 470,481               | 510,232    | 70,387  |
| Half (~span/4) (r=2.25)    | 414,623             | 418,953               | 256,757    | 227,109 |
| Quarter (~span/8) (r=1.13) | 413,902             | 419,061               | 594,832    | 328,502 |
| Tiny (~span/1000) (r=1)    | 413,771             | 418,761               | 595,099    | 332,198 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,279,797           | 2,440,244             | 2,399,399  | 222,330 |
| Half (size=4.5x4.5)      | 566,414             | 557,022               | 365,367    | 358,174 |
| Quarter (size=2.25x2.25) | 586,347             | 595,738               | 781,145    | 378,042 |
| Unit (size=1)            | 586,344             | 546,990               | 781,899    | 365,952 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 165,518             | 148,266               | 188,796    | 303,865 |
| 10 neighbors                  | 606,013             | 409,928               | 587,065    | 727,089 |
| 1 neighbor                    | 623,002             | 744,154               | 663,384    | 809,173 |

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
