# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KDTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](SPATIAL_TREE_SEMANTICS.md)

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
| 1,000,000 entries | 2 (0.467s)          | 3 (0.302s)            | 4 (0.239s) | 1 (0.649s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 33        | 14      |
| Half (~span/4) (r=24.75)    | 137                 | 153                   | 257       | 147     |
| Quarter (~span/8) (r=12.38) | 939                 | 1,232                 | 1,683     | 1,418   |
| Tiny (~span/1000) (r=1)     | 23,546              | 23,745                | 138,961   | 62,730  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 35                  | 41                    | 239       | 21      |
| Half (size≈49.50x49.50x49.50)    | 40                  | 49                    | 1,256     | 283     |
| Quarter (size≈24.75x24.75x24.75) | 41                  | 49                    | 4,002     | 2,535   |
| Unit (size=1)                    | 42                  | 52                    | 182,596   | 78,040  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,872               | 10,139                | 2,329     | 306     |
| 100 neighbors                 | 64,873              | 73,833                | 11,028    | 3,406   |
| 10 neighbors                  | 407,814             | 418,565               | 16,253    | 7,930   |
| 1 neighbor                    | 541,390             | 472,820               | 20,086    | 8,624   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 47 (0.021s)           | 66 (0.015s) | 41 (0.024s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 339                 | 488                   | 779       | 194     |
| Half (~span/4) (r=24.75)    | 1,023               | 1,451                 | 2,033     | 786     |
| Quarter (~span/8) (r=12.38) | 2,577               | 4,006                 | 6,019     | 3,220   |
| Tiny (~span/1000) (r=1)     | 26,845              | 29,895                | 177,902   | 83,661  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 636                 | 764                   | 2,734     | 358     |
| Half (size≈49.50x49.50x4.5)     | 743                 | 904                   | 9,676     | 3,528   |
| Quarter (size≈24.75x24.75x2.25) | 757                 | 935                   | 47,850    | 24,481  |
| Unit (size=1)                   | 761                 | 942                   | 245,299   | 103,571 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,670               | 12,037                | 1,634     | 273     |
| 100 neighbors                 | 38,763              | 44,152                | 9,365     | 2,273   |
| 10 neighbors                  | 449,952             | 325,635               | 19,449    | 7,747   |
| 1 neighbor                    | 443,907             | 318,539               | 30,301    | 12,344  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 457 (0.002s)        | 33 (0.030s)           | 611 (0.002s) | 436 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,795               | 4,624                 | 9,129     | 1,881   |
| Half (~span/4) (r=24.75)    | 5,971               | 6,513                 | 9,027     | 3,894   |
| Quarter (~span/8) (r=12.38) | 5,936               | 6,958                 | 11,280    | 6,967   |
| Tiny (~span/1000) (r=1)     | 41,457              | 39,251                | 221,396   | 134,310 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,470               | 6,478                 | 31,142    | 3,620   |
| Half (size≈49.50x4.5x4.5)      | 7,380               | 7,441                 | 46,362    | 37,826  |
| Quarter (size≈24.75x2.25x2.25) | 7,551               | 7,600                 | 168,200   | 122,157 |
| Unit (size=1)                  | 7,623               | 7,640                 | 324,741   | 165,133 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,753               | 10,654                | 636       | 186     |
| 100 neighbors                 | 51,165              | 69,520                | 5,955     | 2,264   |
| 10 neighbors                  | 441,232             | 457,303               | 27,352    | 13,233  |
| 1 neighbor                    | 653,744             | 567,949               | 45,720    | 22,302  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,501 (0.000s)      | 4,904 (0.000s)        | 4,347 (0.000s) | 4,152 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,054              | 14,184                | 24,936    | 19,382  |
| Half (~span/4) (r=2.25)    | 52,514              | 62,681                | 124,125   | 125,587 |
| Quarter (~span/8) (r=1.13) | 62,932              | 64,740                | 341,014   | 192,247 |
| Tiny (~span/1000) (r=1)    | 62,941              | 64,746                | 341,068   | 190,100 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 59,459              | 64,553                | 321,932   | 36,044  |
| Half (size≈4.5x4.5x4.5)       | 65,463              | 71,924                | 198,773   | 180,034 |
| Quarter (size≈2.25x2.25x2.25) | 66,156              | 73,587                | 498,307   | 237,649 |
| Unit (size=1)                 | 66,137              | 74,543                | 498,530   | 238,275 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,637              | 14,730                | 3,294     | 623     |
| 100 neighbors                 | 69,698              | 65,749                | 15,723    | 4,157   |
| 10 neighbors                  | 447,084             | 420,333               | 75,083    | 33,389  |
| 1 neighbor                    | 647,621             | 669,693               | 85,651    | 44,698  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 31,645 (0.000s)     | 32,467 (0.000s)       | 27,700 (0.000s) | 20,618 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 126,804             | 127,716               | 298,737   | 172,457 |
| Half (~span/4) (r=2.25)    | 147,553             | 149,691               | 319,936   | 271,674 |
| Quarter (~span/8) (r=1.13) | 147,881             | 151,241               | 395,318   | 381,136 |
| Tiny (~span/1000) (r=1)    | 147,954             | 151,280               | 397,415   | 381,459 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 538,883             | 538,873               | 1,798,350 | 324,124 |
| Half (size≈4.5x2x1)     | 567,657             | 577,047               | 494,469   | 442,474 |
| Quarter (size≈2.25x1x1) | 583,128             | 577,009               | 770,229   | 732,006 |
| Unit (size=1)           | 581,198             | 545,654               | 770,807   | 732,608 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 90,582              | 87,488                | 69,756    | 65,754  |
| 10 neighbors                  | 627,309             | 559,401               | 105,886   | 101,530 |
| 1 neighbor                    | 893,603             | 636,804               | 174,788   | 224,626 |

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
