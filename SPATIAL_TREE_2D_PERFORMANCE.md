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
| 1,000,000 entries | 4 (0.242s)          | 6 (0.153s)            | 4 (0.221s) | 2 (0.383s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=499.5)    | 59                  | 58                    | 56         | 7       |
| Half (~span/4) (r=249.8)    | 237                 | 238                   | 208        | 28      |
| Quarter (~span/8) (r=124.9) | 946                 | 946                   | 815        | 119     |
| Tiny (~span/1000) (r=1)     | 103,296             | 105,692               | 143,494    | 97,759  |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=999.0x999.0)    | 317                 | 353                   | 336        | 17      |
| Half (size=499.5x499.5)    | 1,857               | 1,849                 | 1,219      | 72      |
| Quarter (size=249.8x249.8) | 7,448               | 7,335                 | 3,828      | 377     |
| Unit (size=1)              | 148,705             | 152,996               | 197,558    | 112,830 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 8,468               | 16,899                | 12,711     | 70,133  |
| 100 neighbors                 | 73,055              | 75,462                | 79,088     | 177,523 |
| 10 neighbors                  | 402,113             | 359,757               | 236,395    | 283,695 |
| 1 neighbor                    | 517,286             | 507,752               | 293,106    | 297,633 |

#### **100,000 entries**

##### Construction

| Construction    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D  | RTree2D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 51 (0.020s)         | 84 (0.012s)           | 50 (0.020s) | 49 (0.020s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=199.5)    | 601                 | 601                   | 601        | 74      |
| Half (~span/4) (r=99.75)    | 1,356               | 1,360                 | 1,247      | 185     |
| Quarter (~span/8) (r=49.88) | 4,672               | 5,180                 | 4,307      | 722     |
| Tiny (~span/1000) (r=1)     | 127,754             | 128,131               | 179,416    | 145,140 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=399.0x249.0)    | 4,645               | 4,645                 | 4,706      | 234     |
| Half (size=199.5x124.5)    | 9,623               | 12,109                | 7,961      | 965     |
| Quarter (size=99.75x62.25) | 25,400              | 34,298                | 19,544     | 3,784   |
| Unit (size=1)              | 184,037             | 185,160               | 245,914    | 155,355 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 9,952               | 9,920                 | 11,686     | 70,056  |
| 100 neighbors                 | 49,710              | 92,455                | 55,495     | 229,190 |
| 10 neighbors                  | 471,032             | 388,654               | 281,101    | 333,022 |
| 1 neighbor                    | 480,991             | 549,542               | 338,876    | 373,297 |

#### **10,000 entries**

##### Construction

| Construction   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D   | RTree2D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 551 (0.002s)        | 800 (0.001s)          | 546 (0.002s) | 525 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=49.50)    | 5,942               | 5,945                 | 5,941      | 733     |
| Half (~span/4) (r=24.75)    | 22,304              | 22,191                | 13,873     | 2,920   |
| Quarter (~span/8) (r=12.38) | 44,208              | 51,431                | 38,116     | 12,194  |
| Tiny (~span/1000) (r=1)     | 167,413             | 161,761               | 234,097    | 167,770 |

##### Get Elements In Bounds

| Get Elements In Bounds     | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=99.00x99.00)    | 47,031              | 45,293                | 46,157     | 2,404   |
| Half (size=49.50x49.50)    | 167,032             | 165,451               | 36,111     | 9,307   |
| Quarter (size=24.75x24.75) | 75,684              | 103,502               | 75,649     | 35,561  |
| Unit (size=1)              | 240,255             | 227,585               | 320,229    | 181,472 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 13,185              | 13,070                | 14,419     | 65,450  |
| 100 neighbors                 | 62,217              | 57,218                | 96,702     | 230,930 |
| 10 neighbors                  | 390,709             | 424,507               | 299,714    | 399,779 |
| 1 neighbor                    | 641,458             | 605,931               | 398,228    | 460,806 |

#### **1,000 entries**

##### Construction

| Construction  | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D     | RTree2D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,293 (0.000s)      | 7,651 (0.000s)        | 4,833 (0.000s) | 4,714 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=24.50)   | 58,149              | 57,896                | 57,459     | 7,394   |
| Half (~span/4) (r=12.25)   | 60,405              | 76,413                | 57,031     | 14,679  |
| Quarter (~span/8) (r=6.13) | 95,613              | 108,483               | 95,695     | 37,751  |
| Tiny (~span/1000) (r=1)    | 239,168             | 237,044               | 338,176    | 244,401 |

##### Get Elements In Bounds

| Get Elements In Bounds    | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (size=49.00x19.00)   | 499,646             | 499,135               | 540,101    | 23,360  |
| Half (size=24.50x9.5)     | 166,957             | 292,171               | 126,505    | 74,879  |
| Quarter (size=12.25x4.75) | 271,449             | 289,235               | 194,116    | 175,956 |
| Unit (size=1)             | 341,843             | 338,190               | 460,298    | 281,087 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 500 neighbors                 | 46,443              | 46,967                | 40,228     | 70,534  |
| 100 neighbors                 | 79,342              | 78,217                | 92,396     | 272,589 |
| 10 neighbors                  | 497,653             | 573,470               | 405,080    | 532,922 |
| 1 neighbor                    | 728,027             | 586,007               | 434,731    | 543,105 |

#### **100 entries**

##### Construction

| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D      | RTree2D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 41,666 (0.000s)     | 35,714 (0.000s)       | 27,700 (0.000s) | 24,096 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| -------------------------- | ------------------- | --------------------- | ---------- | ------- |
| Full (~span/2) (r=4.5)     | 496,195             | 494,030               | 509,285    | 72,691  |
| Half (~span/4) (r=2.25)    | 421,999             | 423,114               | 257,119    | 236,378 |
| Quarter (~span/8) (r=1.13) | 421,665             | 394,791               | 599,129    | 338,518 |
| Tiny (~span/1000) (r=1)    | 421,578             | 398,874               | 599,521    | 338,820 |

##### Get Elements In Bounds

| Get Elements In Bounds   | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ------------------------ | ------------------- | --------------------- | ---------- | ------- |
| Full (size=9x9)          | 2,405,297           | 2,015,574             | 2,414,353  | 224,027 |
| Half (size=4.5x4.5)      | 548,616             | 559,875               | 365,374    | 368,394 |
| Quarter (size=2.25x2.25) | 541,107             | 599,286               | 786,141    | 375,705 |
| Unit (size=1)            | 563,974             | 599,575               | 786,313    | 394,632 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| ----------------------------- | ------------------- | --------------------- | ---------- | ------- |
| 100 neighbors (max)           | 156,774             | 165,187               | 202,641    | 323,451 |
| 10 neighbors                  | 618,887             | 495,826               | 609,986    | 720,591 |
| 1 neighbor                    | 619,752             | 773,756               | 675,868    | 864,801 |

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
