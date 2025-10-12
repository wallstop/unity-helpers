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
<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

| Construction      | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D  | RTree3D    |
| ----------------- | ------------------- | --------------------- | ---------- | ---------- |
| 1,000,000 entries | 2 (0.405s)          | 3 (0.321s)            | 4 (0.238s) | 2 (0.369s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 33        | 15      |
| Half (~span/4) (r=24.75)    | 131                 | 160                   | 242       | 153     |
| Quarter (~span/8) (r=12.38) | 937                 | 1,229                 | 1,653     | 1,520   |
| Tiny (~span/1000) (r=1)     | 23,510              | 23,699                | 97,513    | 76,591  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 33                  | 39                    | 225       | 23      |
| Half (size≈49.50x49.50x49.50)    | 39                  | 46                    | 1,298     | 276     |
| Quarter (size≈24.75x24.75x24.75) | 40                  | 47                    | 4,050     | 2,531   |
| Unit (size=1)                    | 40                  | 48                    | 182,963   | 77,858  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,895               | 10,188                | 2,309     | 304     |
| 100 neighbors                 | 65,303              | 74,548                | 10,926    | 3,387   |
| 10 neighbors                  | 408,553             | 419,729               | 16,046    | 7,914   |
| 1 neighbor                    | 552,534             | 484,025               | 19,874    | 8,610   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 47 (0.021s)           | 65 (0.015s) | 44 (0.022s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 335                 | 485                   | 776       | 213     |
| Half (~span/4) (r=24.75)    | 1,021               | 1,434                 | 2,047     | 857     |
| Quarter (~span/8) (r=12.38) | 2,568               | 3,994                 | 6,043     | 3,441   |
| Tiny (~span/1000) (r=1)     | 26,786              | 29,821                | 177,795   | 101,341 |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 636                 | 778                   | 2,716     | 354     |
| Half (size≈49.50x49.50x4.5)     | 740                 | 909                   | 9,663     | 3,518   |
| Quarter (size≈24.75x24.75x2.25) | 752                 | 934                   | 46,764    | 24,351  |
| Unit (size=1)                   | 755                 | 942                   | 245,087   | 103,425 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,738               | 12,074                | 1,626     | 271     |
| 100 neighbors                 | 38,663              | 44,099                | 9,271     | 2,266   |
| 10 neighbors                  | 424,563             | 310,952               | 19,261    | 7,732   |
| 1 neighbor                    | 460,856             | 332,893               | 30,086    | 12,328  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 450 (0.002s)        | 146 (0.007s)          | 601 (0.002s) | 435 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,778               | 4,607                 | 9,136     | 1,904   |
| Half (~span/4) (r=24.75)    | 5,951               | 6,495                 | 8,914     | 3,800   |
| Quarter (~span/8) (r=12.38) | 5,924               | 6,936                 | 11,190    | 7,081   |
| Tiny (~span/1000) (r=1)     | 41,368              | 38,706                | 220,785   | 157,777 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,536               | 6,552                 | 26,350    | 3,597   |
| Half (size≈49.50x4.5x4.5)      | 7,376               | 7,413                 | 46,213    | 37,593  |
| Quarter (size≈24.75x2.25x2.25) | 7,520               | 7,551                 | 168,253   | 121,961 |
| Unit (size=1)                  | 7,612               | 7,590                 | 323,189   | 163,462 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,768               | 10,516                | 636       | 184     |
| 100 neighbors                 | 50,714              | 66,402                | 5,931     | 2,247   |
| 10 neighbors                  | 441,906             | 453,023               | 27,115    | 13,149  |
| 1 neighbor                    | 503,317             | 596,546               | 45,358    | 22,171  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D      |
| ------------- | ------------------- | --------------------- | -------------- | ------------ |
| 1,000 entries | 1,970 (0.001s)      | 5,010 (0.000s)        | 4,001 (0.000s) | 661 (0.002s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 12,016              | 14,151                | 24,554    | 19,616  |
| Half (~span/4) (r=2.25)    | 52,024              | 62,524                | 124,541   | 148,200 |
| Quarter (~span/8) (r=1.13) | 60,415              | 64,618                | 338,784   | 226,687 |
| Tiny (~span/1000) (r=1)    | 62,656              | 64,645                | 338,789   | 226,826 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 59,709              | 64,685                | 323,836   | 35,735  |
| Half (size≈4.5x4.5x4.5)       | 65,153              | 71,612                | 198,320   | 179,787 |
| Quarter (size≈2.25x2.25x2.25) | 65,827              | 73,267                | 496,693   | 237,774 |
| Unit (size=1)                 | 65,842              | 74,314                | 496,960   | 238,324 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,885              | 14,863                | 3,257     | 617     |
| 100 neighbors                 | 69,896              | 65,777                | 15,639    | 4,137   |
| 10 neighbors                  | 473,839             | 394,340               | 73,941    | 33,359  |
| 1 neighbor                    | 668,802             | 659,922               | 83,425    | 44,659  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 26,666 (0.000s)     | 34,013 (0.000s)       | 25,188 (0.000s) | 21,834 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 126,202             | 127,160               | 294,794   | 195,711 |
| Half (~span/4) (r=2.25)    | 146,998             | 149,086               | 318,059   | 307,466 |
| Quarter (~span/8) (r=1.13) | 147,360             | 150,616               | 393,125   | 403,724 |
| Tiny (~span/1000) (r=1)    | 147,348             | 150,680               | 394,470   | 423,009 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 533,149             | 533,863               | 1,745,022 | 329,398 |
| Half (size≈4.5x2x1)     | 557,962             | 567,439               | 514,854   | 460,328 |
| Quarter (size≈2.25x1x1) | 542,713             | 566,118               | 765,601   | 729,872 |
| Unit (size=1)           | 572,783             | 566,230               | 764,660   | 692,164 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 91,290              | 87,287                | 69,665    | 65,698  |
| 10 neighbors                  | 608,256             | 510,875               | 105,249   | 101,525 |
| 1 neighbor                    | 850,358             | 628,202               | 173,277   | 224,265 |

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
