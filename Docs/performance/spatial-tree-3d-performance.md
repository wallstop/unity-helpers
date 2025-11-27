# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KDTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](../features/spatial/spatial-tree-semantics.md)

This document contains performance benchmarks for the 3D spatial tree implementations in Unity Helpers.

## Available 3D Spatial Trees

- **OctTree3D** - Easiest to use, good all-around performance for 3D
- **KDTree3D** - Balanced and unbalanced variants available
- **RTree3D** - Optimized for 3D bounding box queries
- **SpatialHash3D** - Efficient for uniformly distributed moving objects (stable)

## Performance Benchmarks

<!-- SPATIAL_TREE_3D_BENCHMARKS_START -->

### Datasets

<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

| Construction      | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D  | RTree3D    |
| ----------------- | ------------------- | --------------------- | ---------- | ---------- |
| 1,000,000 entries | 3 (0.280s)          | 6 (0.160s)            | 2 (0.343s) | 2 (0.454s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 23                    | 31        | 16      |
| Half (~span/4) (r=24.75)    | 138                 | 174                   | 186       | 140     |
| Quarter (~span/8) (r=12.38) | 1,015               | 1,359                 | 1,663     | 1,476   |
| Tiny (~span/1000) (r=1)     | 23,367              | 23,989                | 138,591   | 71,012  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 27                  | 33                    | 171       | 20      |
| Half (size≈49.50x49.50x49.50)    | 40                  | 46                    | 419       | 267     |
| Quarter (size≈24.75x24.75x24.75) | 39                  | 48                    | 879       | 2,518   |
| Unit (size=1)                    | 39                  | 50                    | 21,556    | 77,986  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,115               | 10,542                | 2,113     | 306     |
| 100 neighbors                 | 69,115              | 73,911                | 9,817     | 3,391   |
| 10 neighbors                  | 371,272             | 373,337               | 14,575    | 7,802   |
| 1 neighbor                    | 561,654             | 538,262               | 18,068    | 8,438   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 47 (0.021s)         | 93 (0.011s)           | 65 (0.015s) | 40 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 372                 | 576                   | 776       | 151     |
| Half (~span/4) (r=24.75)    | 1,095               | 1,676                 | 2,045     | 832     |
| Quarter (~span/8) (r=12.38) | 2,748               | 4,573                 | 5,985     | 3,363   |
| Tiny (~span/1000) (r=1)     | 27,323              | 31,749                | 176,592   | 94,890  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 568                 | 686                   | 2,766     | 335     |
| Half (size≈49.50x49.50x4.5)     | 690                 | 824                   | 2,322     | 3,513   |
| Quarter (size≈24.75x24.75x2.25) | 691                 | 840                   | 9,322     | 24,325  |
| Unit (size=1)                   | 725                 | 876                   | 31,482    | 102,840 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 7,038               | 12,535                | 1,547     | 273     |
| 100 neighbors                 | 41,588              | 47,563                | 8,438     | 2,265   |
| 10 neighbors                  | 473,007             | 344,217               | 17,291    | 7,636   |
| 1 neighbor                    | 446,339             | 320,877               | 27,613    | 12,042  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 581 (0.002s)        | 696 (0.001s)          | 570 (0.002s) | 402 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 5,003               | 4,974                 | 9,154     | 2,161   |
| Half (~span/4) (r=24.75)    | 6,250               | 6,884                 | 8,886     | 4,188   |
| Quarter (~span/8) (r=12.38) | 6,334               | 7,250                 | 11,125    | 7,344   |
| Tiny (~span/1000) (r=1)     | 42,580              | 39,731                | 220,589   | 153,201 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,215               | 6,181                 | 33,448    | 3,565   |
| Half (size≈49.50x4.5x4.5)      | 7,003               | 7,053                 | 9,421     | 37,064  |
| Quarter (size≈24.75x2.25x2.25) | 7,143               | 7,174                 | 28,074    | 121,768 |
| Unit (size=1)                  | 7,213               | 7,225                 | 44,252    | 163,879 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,327              | 11,200                | 611       | 185     |
| 100 neighbors                 | 55,113              | 72,281                | 5,590     | 2,241   |
| 10 neighbors                  | 435,713             | 433,743               | 25,024    | 13,010  |
| 1 neighbor                    | 706,722             | 610,621               | 42,065    | 22,241  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 5,141 (0.000s)      | 6,671 (0.000s)        | 4,344 (0.000s) | 3,892 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,352              | 15,692                | 24,628    | 19,929  |
| Half (~span/4) (r=2.25)    | 55,898              | 65,764                | 123,081   | 137,432 |
| Quarter (~span/8) (r=1.13) | 65,197              | 67,970                | 336,362   | 214,904 |
| Tiny (~span/1000) (r=1)    | 65,120              | 67,702                | 335,004   | 216,975 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 57,698              | 61,371                | 291,217   | 35,720  |
| Half (size≈4.5x4.5x4.5)       | 61,896              | 67,889                | 33,919    | 177,485 |
| Quarter (size≈2.25x2.25x2.25) | 62,595              | 69,341                | 74,265    | 226,953 |
| Unit (size=1)                 | 62,618              | 70,266                | 74,292    | 220,486 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,899              | 15,829                | 3,068     | 623     |
| 100 neighbors                 | 74,320              | 67,211                | 15,107    | 4,140   |
| 10 neighbors                  | 441,368             | 435,047               | 70,228    | 33,281  |
| 1 neighbor                    | 738,702             | 673,087               | 79,197    | 44,583  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 39,370 (0.000s)     | 32,362 (0.000s)       | 25,316 (0.000s) | 20,661 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 139,136             | 140,293               | 297,491   | 184,025 |
| Half (~span/4) (r=2.25)    | 164,459             | 166,496               | 316,841   | 283,729 |
| Quarter (~span/8) (r=1.13) | 165,025             | 167,511               | 389,216   | 412,513 |
| Tiny (~span/1000) (r=1)    | 164,879             | 168,720               | 390,526   | 412,074 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 524,433             | 523,528               | 1,296,327 | 329,322 |
| Half (size≈4.5x2x1)     | 542,954             | 550,835               | 107,050   | 459,756 |
| Quarter (size≈2.25x1x1) | 557,105             | 550,664               | 148,259   | 731,231 |
| Unit (size=1)           | 554,593             | 550,361               | 148,620   | 711,664 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 97,180              | 93,432                | 65,846    | 62,333  |
| 10 neighbors                  | 613,547             | 501,888               | 98,520    | 101,340 |
| 1 neighbor                    | 873,353             | 667,454               | 161,203   | 224,253 |

<!-- tabs:end -->
<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**OctTree3D**:

- Best for: General-purpose 3D spatial queries
- Strengths: Balanced performance, easy to use, good spatial locality
- Use cases: 3D collision detection, visibility culling, spatial audio

**KDTree3D (Balanced)**:

- Best for: Nearest-neighbor queries in 3D space
- Strengths: Fast point queries, good for smaller datasets
- Use cases: Pathfinding, AI spatial awareness, particle systems

**KDTree3D (Unbalanced)**:

- Best for: When you need fast construction and will rebuild frequently
- Strengths: Fastest construction, similar query performance to balanced
- Use cases: Dynamic environments, frequently changing spatial data

**RTree3D**:

- Best for: 3D bounding box queries, especially with volumetric data
- Strengths: Excellent for large bounding volumes, handles overlapping objects
- Use cases: Physics engines, frustum culling, volumetric effects

### Important Notes

- All spatial trees assume **immutable** positional data
- If positions change, you must reconstruct the tree
- Spatial queries are O(log n) vs O(n) for linear search
- 3D trees have higher construction costs than 2D variants due to additional dimension
- Construction cost is amortized over many queries
