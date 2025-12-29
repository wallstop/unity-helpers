---
---

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
| 1,000,000 entries | 3 (0.254s)          | 6 (0.163s)            | 3 (0.251s) | 3 (0.299s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 57                  | 56                    | 54         | 7       |
| Half (~span/4) (r=249.8)    | 229                 | 231                   | 205        | 27      |
| Quarter (~span/8) (r=124.9) | 913                 | 926                   | 787        | 116     |
| Tiny (~span/1000) (r=1)     | 99,572              | 104,019               | 140,943    | 105,235 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 270                 | 334                   | 307        | 17      |
| Half (size=499.5x499.5)    | 1,732               | 1,789                 | 1,182      | 72      |
| Quarter (size=249.8x249.8) | 6,569               | 6,836                 | 3,538      | 361     |
| Unit (size=1)              | 134,850             | 148,054               | 193,434    | 110,130 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 7,628               | 15,960                | 12,050     | 62,826  |
| 100 neighbors                 | 70,691              | 68,039                | 70,831     | 145,032 |
| 10 neighbors                  | 247,159             | 211,557               | 178,739    | 213,226 |
| 1 neighbor                    | 342,017             | 320,196               | 188,391    | 218,286 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 32 (0.031s)         | 83 (0.012s)           | 49 (0.020s) | 45 (0.022s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 584                 | 580                   | 570        | 68      |
| Half (~span/4) (r=99.75)    | 1,301               | 1,305                 | 1,191      | 178     |
| Quarter (~span/8) (r=49.88) | 4,489               | 4,927                 | 4,151      | 707     |
| Tiny (~span/1000) (r=1)     | 110,790             | 124,357               | 173,762    | 143,972 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,092               | 4,337                 | 4,293      | 173     |
| Half (size=199.5x124.5)    | 9,081               | 11,349                | 7,554      | 818     |
| Quarter (size=99.75x62.25) | 24,735              | 31,853                | 18,819     | 3,513   |
| Unit (size=1)              | 178,072             | 177,513               | 239,334    | 147,219 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,150               | 9,272                 | 11,009     | 61,145  |
| 100 neighbors                 | 45,513              | 82,292                | 50,716     | 170,484 |
| 10 neighbors                  | 272,696             | 242,229               | 199,069    | 243,503 |
| 1 neighbor                    | 280,173             | 297,148               | 240,319    | 259,221 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 403 (0.002s)        | 792 (0.001s)          | 545 (0.002s) | 505 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,761               | 5,726                 | 5,753      | 703     |
| Half (~span/4) (r=24.75)    | 21,756              | 21,772                | 13,510     | 2,770   |
| Quarter (~span/8) (r=12.38) | 42,789              | 49,433                | 37,198     | 11,790  |
| Tiny (~span/1000) (r=1)     | 159,448             | 156,791               | 227,998    | 163,665 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 43,993              | 44,519                | 44,961     | 2,295   |
| Half (size=49.50x49.50)    | 157,291             | 161,771               | 35,674     | 8,890   |
| Quarter (size=24.75x24.75) | 71,621              | 100,296               | 72,741     | 34,474  |
| Unit (size=1)              | 211,792             | 225,866               | 309,856    | 174,626 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,070              | 12,023                | 13,558     | 58,348  |
| 100 neighbors                 | 55,290              | 52,332                | 84,674     | 178,713 |
| 10 neighbors                  | 249,049             | 253,463               | 214,354    | 273,246 |
| 1 neighbor                    | 360,678             | 311,833               | 267,562    | 267,089 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,373 (0.000s)      | 7,770 (0.000s)        | 4,688 (0.000s) | 4,599 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 56,863              | 56,884                | 55,994     | 7,277   |
| Half (~span/4) (r=12.25)   | 58,444              | 73,754                | 55,810     | 14,443  |
| Quarter (~span/8) (r=6.13) | 94,455              | 105,779               | 93,735     | 36,833  |
| Tiny (~span/1000) (r=1)    | 236,175             | 231,817               | 329,172    | 245,654 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 492,567             | 491,501               | 524,518    | 23,601  |
| Half (size=24.50x9.5)     | 163,383             | 283,559               | 122,938    | 73,660  |
| Quarter (size=12.25x4.75) | 262,730             | 279,580               | 189,705    | 170,846 |
| Unit (size=1)             | 331,819             | 323,932               | 450,633    | 273,094 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 42,430              | 42,933                | 36,961     | 62,882  |
| 100 neighbors                 | 72,675              | 64,178                | 80,660     | 202,015 |
| 10 neighbors                  | 287,329             | 311,573               | 248,672    | 327,570 |
| 1 neighbor                    | 417,778             | 318,007               | 232,683    | 349,215 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D         |
| ------------ | ------------------- | --------------------- | -------------- | --------------- |
| 100 entries  | 34,013 (0.000s)     | 38,759 (0.000s)       | 8,673 (0.000s) | 15,037 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 500,512             | 499,105               | 435,854    | 65,910  |
| Half (~span/4) (r=2.25)    | 421,443             | 429,533               | 229,725    | 232,216 |
| Quarter (~span/8) (r=1.13) | 424,543             | 421,724               | 501,071    | 330,938 |
| Tiny (~span/1000) (r=1)    | 423,866             | 419,996               | 441,846    | 331,369 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,147,801           | 2,240,635             | 2,077,802  | 219,580 |
| Half (size=4.5x4.5)      | 558,112             | 548,366               | 356,002    | 357,432 |
| Quarter (size=2.25x2.25) | 573,664             | 583,506               | 767,456    | 380,632 |
| Unit (size=1)            | 575,442             | 583,853               | 772,815    | 382,688 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 134,854             | 128,880               | 160,478    | 227,602 |
| 10 neighbors                  | 324,407             | 270,598               | 366,789    | 400,400 |
| 1 neighbor                    | 331,468             | 395,204               | 376,926    | 400,706 |

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
