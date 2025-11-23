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
| 1,000,000 entries | 3 (0.260s)          | 6 (0.161s)            | 3 (0.332s) | 2 (0.438s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 57                  | 57                    | 54         | 7       |
| Half (~span/4) (r=249.8)    | 221                 | 232                   | 198        | 28      |
| Quarter (~span/8) (r=124.9) | 944                 | 945                   | 814        | 115     |
| Tiny (~span/1000) (r=1)     | 102,738             | 102,977               | 143,019    | 106,101 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 274                 | 285                   | 283        | 16      |
| Half (size=499.5x499.5)    | 1,769               | 1,771                 | 1,209      | 65      |
| Quarter (size=249.8x249.8) | 7,060               | 7,028                 | 3,787      | 372     |
| Unit (size=1)              | 146,061             | 149,809               | 192,474    | 111,918 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,305               | 16,867                | 12,589     | 69,433  |
| 100 neighbors                 | 75,359              | 76,346                | 78,858     | 173,297 |
| 10 neighbors                  | 357,486             | 338,192               | 251,843    | 267,452 |
| 1 neighbor                    | 503,027             | 533,861               | 222,502    | 297,813 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 50 (0.020s)         | 82 (0.012s)           | 13 (0.072s) | 46 (0.022s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 601                 | 601                   | 600        | 70      |
| Half (~span/4) (r=99.75)    | 1,353               | 1,355                 | 1,241      | 184     |
| Quarter (~span/8) (r=49.88) | 4,645               | 5,164                 | 4,289      | 722     |
| Tiny (~span/1000) (r=1)     | 125,108             | 126,889               | 177,363    | 145,196 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,452               | 4,551                 | 4,483      | 222     |
| Half (size=199.5x124.5)    | 9,410               | 11,672                | 7,852      | 968     |
| Quarter (size=99.75x62.25) | 25,135              | 31,388                | 19,176     | 3,796   |
| Unit (size=1)              | 180,107             | 177,302               | 237,424    | 155,036 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,868               | 9,743                 | 11,647     | 69,660  |
| 100 neighbors                 | 49,807              | 92,527                | 54,540     | 229,455 |
| 10 neighbors                  | 457,033             | 364,971               | 291,394    | 344,185 |
| 1 neighbor                    | 501,758             | 567,421               | 311,325    | 367,809 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 535 (0.002s)        | 814 (0.001s)          | 540 (0.002s) | 505 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,877               | 5,940                 | 5,919      | 734     |
| Half (~span/4) (r=24.75)    | 22,043              | 22,293                | 13,847     | 2,910   |
| Quarter (~span/8) (r=12.38) | 43,058              | 51,204                | 38,071     | 12,088  |
| Tiny (~span/1000) (r=1)     | 162,154             | 159,459               | 234,045    | 166,850 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 43,947              | 44,257                | 46,179     | 2,386   |
| Half (size=49.50x49.50)    | 162,459             | 163,382               | 36,307     | 9,207   |
| Quarter (size=24.75x24.75) | 73,922              | 100,644               | 72,619     | 35,185  |
| Unit (size=1)              | 232,335             | 228,886               | 306,267    | 180,332 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 12,576              | 12,824                | 14,210     | 63,728  |
| 100 neighbors                 | 61,566              | 56,191                | 94,982     | 216,069 |
| 10 neighbors                  | 415,205             | 410,845               | 286,311    | 409,316 |
| 1 neighbor                    | 570,403             | 567,570               | 386,149    | 451,053 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,170 (0.000s)      | 7,987 (0.000s)        | 4,833 (0.000s) | 4,161 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,051              | 57,976                | 57,242     | 7,388   |
| Half (~span/4) (r=12.25)   | 59,946              | 75,266                | 57,042     | 14,688  |
| Quarter (~span/8) (r=6.13) | 94,765              | 105,986               | 95,619     | 37,921  |
| Tiny (~span/1000) (r=1)    | 237,582             | 234,955               | 335,412    | 252,367 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 488,756             | 485,590               | 526,119    | 23,965  |
| Half (size=24.50x9.5)     | 163,441             | 283,000               | 121,714    | 74,218  |
| Quarter (size=12.25x4.75) | 265,705             | 283,468               | 187,400    | 174,537 |
| Unit (size=1)             | 331,818             | 332,552               | 451,497    | 273,195 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 45,636              | 46,665                | 39,776     | 67,549  |
| 100 neighbors                 | 75,600              | 77,470                | 91,512     | 246,132 |
| 10 neighbors                  | 500,411             | 531,379               | 392,690    | 530,898 |
| 1 neighbor                    | 658,472             | 539,234               | 430,123    | 584,563 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 38,759 (0.000s)     | 33,222 (0.000s)       | 26,109 (0.000s) | 20,366 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 497,350             | 498,290               | 507,378    | 72,737  |
| Half (~span/4) (r=2.25)    | 395,712             | 429,606               | 257,075    | 237,421 |
| Quarter (~span/8) (r=1.13) | 423,518             | 432,367               | 593,688    | 340,441 |
| Tiny (~span/1000) (r=1)    | 427,705             | 431,474               | 549,588    | 340,395 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,413,367           | 2,398,689             | 2,476,077  | 222,050 |
| Half (size=4.5x4.5)      | 560,719             | 478,136               | 353,489    | 345,539 |
| Quarter (size=2.25x2.25) | 584,211             | 521,346               | 767,577    | 392,460 |
| Unit (size=1)            | 585,439             | 500,066               | 760,414    | 384,299 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 161,775             | 161,654               | 195,562    | 317,159 |
| 10 neighbors                  | 546,250             | 492,450               | 611,306    | 687,062 |
| 1 neighbor                    | 545,095             | 699,857               | 662,539    | 867,993 |

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
