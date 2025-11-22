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
| 1,000,000 entries | 3 (0.264s)          | 4 (0.217s)            | 3 (0.255s) | 1 (0.590s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 57                  | 56                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 236                 | 227                   | 205        | 28      |
| Quarter (~span/8) (r=124.9) | 943                 | 946                   | 812        | 115     |
| Tiny (~span/1000) (r=1)     | 103,110             | 105,319               | 143,728    | 107,787 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 272                 | 261                   | 238        | 17      |
| Half (size=499.5x499.5)    | 1,780               | 1,689                 | 1,135      | 66      |
| Quarter (size=249.8x249.8) | 7,054               | 6,926                 | 3,835      | 374     |
| Unit (size=1)              | 147,032             | 152,550               | 194,493    | 112,996 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,439               | 16,951                | 12,413     | 69,208  |
| 100 neighbors                 | 78,474              | 76,129                | 79,286     | 176,906 |
| 10 neighbors                  | 369,492             | 346,561               | 251,974    | 265,722 |
| 1 neighbor                    | 547,774             | 539,833               | 253,880    | 297,308 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 50 (0.020s)         | 83 (0.012s)           | 37 (0.026s) | 13 (0.074s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 600                 | 598                   | 600        | 70      |
| Half (~span/4) (r=99.75)    | 1,356               | 1,342                 | 1,245      | 185     |
| Quarter (~span/8) (r=49.88) | 4,644               | 5,180                 | 4,289      | 724     |
| Tiny (~span/1000) (r=1)     | 127,146             | 128,158               | 173,080    | 145,862 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,395               | 4,486                 | 4,607      | 226     |
| Half (size=199.5x124.5)    | 9,480               | 11,721                | 7,966      | 968     |
| Quarter (size=99.75x62.25) | 25,265              | 32,033                | 19,519     | 3,747   |
| Unit (size=1)              | 181,656             | 184,131               | 244,427    | 154,511 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,963               | 9,939                 | 11,577     | 69,758  |
| 100 neighbors                 | 49,889              | 93,417                | 54,889     | 231,362 |
| 10 neighbors                  | 466,088             | 364,999               | 296,913    | 348,445 |
| 1 neighbor                    | 508,447             | 577,930               | 330,625    | 370,039 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 526 (0.002s)        | 781 (0.001s)          | 537 (0.002s) | 524 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,940               | 5,937                 | 5,903      | 734     |
| Half (~span/4) (r=24.75)    | 22,325              | 22,296                | 13,841     | 2,921   |
| Quarter (~span/8) (r=12.38) | 44,183              | 51,315                | 38,080     | 12,190  |
| Tiny (~span/1000) (r=1)     | 167,660             | 162,224               | 234,288    | 168,088 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 43,742              | 44,993                | 46,396     | 2,408   |
| Half (size=49.50x49.50)    | 142,528             | 165,083               | 35,760     | 9,281   |
| Quarter (size=24.75x24.75) | 74,296              | 102,901               | 75,177     | 35,569  |
| Unit (size=1)              | 237,700             | 232,303               | 316,859    | 181,778 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,180              | 12,994                | 14,447     | 64,540  |
| 100 neighbors                 | 58,721              | 57,295                | 96,457     | 216,296 |
| 10 neighbors                  | 420,438             | 424,345               | 291,730    | 417,808 |
| 1 neighbor                    | 596,687             | 603,117               | 389,932    | 459,140 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,157 (0.000s)      | 7,733 (0.000s)        | 4,730 (0.000s) | 4,364 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,059              | 58,167                | 57,391     | 7,366   |
| Half (~span/4) (r=12.25)   | 60,241              | 76,418                | 57,233     | 14,685  |
| Quarter (~span/8) (r=6.13) | 95,616              | 108,493               | 94,933     | 38,035  |
| Tiny (~span/1000) (r=1)    | 239,753             | 237,600               | 335,564    | 252,832 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 494,348             | 480,450               | 529,211    | 24,066  |
| Half (size=24.50x9.5)     | 165,122             | 286,594               | 125,992    | 74,319  |
| Quarter (size=12.25x4.75) | 267,503             | 285,689               | 192,527    | 176,838 |
| Unit (size=1)             | 336,903             | 334,136               | 453,158    | 280,743 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,022              | 46,951                | 39,713     | 69,462  |
| 100 neighbors                 | 75,166              | 77,581                | 91,380     | 258,543 |
| 10 neighbors                  | 522,401             | 564,518               | 390,459    | 540,709 |
| 1 neighbor                    | 716,629             | 573,242               | 428,074    | 589,655 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 37,735 (0.000s)     | 35,211 (0.000s)       | 25,062 (0.000s) | 18,214 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 495,517             | 499,698               | 506,032    | 72,796  |
| Half (~span/4) (r=2.25)    | 404,989             | 435,344               | 253,310    | 237,975 |
| Quarter (~span/8) (r=1.13) | 429,487             | 434,651               | 569,325    | 340,060 |
| Tiny (~span/1000) (r=1)    | 429,436             | 435,576               | 600,073    | 339,701 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,460,581           | 2,201,552             | 2,537,085  | 219,301 |
| Half (size=4.5x4.5)      | 564,672             | 487,478               | 364,658    | 353,053 |
| Quarter (size=2.25x2.25) | 590,618             | 506,292               | 777,873    | 390,470 |
| Unit (size=1)            | 590,421             | 557,966               | 753,401    | 395,296 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 165,412             | 165,323               | 191,542    | 322,960 |
| 10 neighbors                  | 600,422             | 520,672               | 639,274    | 714,086 |
| 1 neighbor                    | 620,217             | 764,320               | 660,694    | 867,459 |

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
