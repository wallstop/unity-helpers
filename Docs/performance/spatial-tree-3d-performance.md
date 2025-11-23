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
| 1,000,000 entries | 3 (0.274s)          | 6 (0.166s)            | 2 (0.372s) | 1 (0.505s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 19                  | 22                    | 27        | 14      |
| Half (~span/4) (r=24.75)    | 134                 | 158                   | 169       | 111     |
| Quarter (~span/8) (r=12.38) | 1,013               | 1,329                 | 1,668     | 1,310   |
| Tiny (~span/1000) (r=1)     | 23,152              | 23,448                | 133,469   | 69,319  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 28                  | 30                    | 199       | 20      |
| Half (size≈49.50x49.50x49.50)    | 38                  | 43                    | 1,282     | 195     |
| Quarter (size≈24.75x24.75x24.75) | 39                  | 45                    | 3,982     | 2,431   |
| Unit (size=1)                    | 37                  | 47                    | 181,300   | 75,259  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,990               | 10,594                | 39        | 300     |
| 100 neighbors                 | 67,639              | 76,298                | 299       | 3,354   |
| 10 neighbors                  | 367,607             | 383,247               | 1,432     | 7,836   |
| 1 neighbor                    | 546,350             | 519,327               | 1,671     | 8,518   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 47 (0.021s)         | 93 (0.011s)           | 60 (0.017s) | 37 (0.027s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 362                 | 559                   | 729       | 148     |
| Half (~span/4) (r=24.75)    | 1,098               | 1,662                 | 1,992     | 844     |
| Quarter (~span/8) (r=12.38) | 2,704               | 4,558                 | 6,019     | 3,129   |
| Tiny (~span/1000) (r=1)     | 26,729              | 31,799                | 176,026   | 97,339  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 443                 | 697                   | 3,150     | 310     |
| Half (size≈49.50x49.50x4.5)     | 604                 | 813                   | 9,384     | 3,399   |
| Quarter (size≈24.75x24.75x2.25) | 568                 | 817                   | 47,627    | 24,121  |
| Unit (size=1)                   | 683                 | 829                   | 245,466   | 101,586 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,884               | 11,871                | 71        | 273     |
| 100 neighbors                 | 39,494              | 44,692                | 944       | 2,283   |
| 10 neighbors                  | 460,715             | 336,575               | 5,243     | 7,720   |
| 1 neighbor                    | 446,959             | 315,873               | 5,712     | 12,237  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 597 (0.002s)        | 490 (0.002s)          | 569 (0.002s) | 406 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,928               | 4,882                 | 8,952     | 2,128   |
| Half (~span/4) (r=24.75)    | 5,986               | 6,735                 | 8,744     | 4,153   |
| Quarter (~span/8) (r=12.38) | 6,034               | 7,114                 | 11,213    | 7,358   |
| Tiny (~span/1000) (r=1)     | 41,007              | 38,535                | 220,865   | 154,583 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 5,984               | 5,824                 | 30,360    | 3,520   |
| Half (size≈49.50x4.5x4.5)      | 6,726               | 6,746                 | 44,908    | 37,382  |
| Quarter (size≈24.75x2.25x2.25) | 6,872               | 6,861                 | 171,154   | 120,484 |
| Unit (size=1)                  | 6,975               | 6,978                 | 316,258   | 159,502 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,217              | 11,024                | 126       | 183     |
| 100 neighbors                 | 54,312              | 67,653                | 1,216     | 2,210   |
| 10 neighbors                  | 454,088             | 460,058               | 15,039    | 12,773  |
| 1 neighbor                    | 700,464             | 614,514               | 43,615    | 21,499  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,798 (0.000s)      | 7,183 (0.000s)        | 3,815 (0.000s) | 1,857 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,743              | 15,245                | 24,412    | 20,994  |
| Half (~span/4) (r=2.25)    | 54,347              | 64,787                | 121,913   | 141,760 |
| Quarter (~span/8) (r=1.13) | 62,541              | 66,178                | 331,467   | 206,858 |
| Tiny (~span/1000) (r=1)    | 63,643              | 65,010                | 326,519   | 217,193 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 54,728              | 59,230                | 314,105   | 35,721  |
| Half (size≈4.5x4.5x4.5)       | 58,902              | 63,968                | 198,327   | 175,894 |
| Quarter (size≈2.25x2.25x2.25) | 59,465              | 66,300                | 496,838   | 226,776 |
| Unit (size=1)                 | 59,460              | 67,215                | 494,691   | 230,671 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 16,469              | 15,727                | 377       | 607     |
| 100 neighbors                 | 74,709              | 69,231                | 3,004     | 4,078   |
| 10 neighbors                  | 443,206             | 426,510               | 60,309    | 33,059  |
| 1 neighbor                    | 678,323             | 679,283               | 84,129    | 44,601  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 31,152 (0.000s)     | 30,395 (0.000s)       | 22,371 (0.000s) | 18,484 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 137,415             | 134,043               | 288,546   | 191,688 |
| Half (~span/4) (r=2.25)    | 162,348             | 161,315               | 313,191   | 302,451 |
| Quarter (~span/8) (r=1.13) | 160,543             | 164,837               | 383,278   | 416,588 |
| Tiny (~span/1000) (r=1)    | 164,666             | 165,872               | 387,377   | 401,995 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 504,640             | 501,574               | 1,719,909 | 298,705 |
| Half (size≈4.5x2x1)     | 519,193             | 533,894               | 482,230   | 447,197 |
| Quarter (size≈2.25x1x1) | 530,632             | 528,283               | 756,579   | 704,470 |
| Unit (size=1)           | 530,780             | 494,562               | 757,111   | 709,926 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 94,656              | 92,132                | 58,054    | 63,603  |
| 10 neighbors                  | 620,762             | 564,286               | 87,514    | 99,780  |
| 1 neighbor                    | 866,558             | 661,672               | 176,821   | 219,884 |

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
