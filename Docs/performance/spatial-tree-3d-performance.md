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
| 1,000,000 entries | 3 (0.271s)          | 6 (0.163s)            | 4 (0.249s) | 2 (0.448s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 20                  | 22                    | 32        | 15      |
| Half (~span/4) (r=24.75)    | 139                 | 173                   | 223       | 131     |
| Quarter (~span/8) (r=12.38) | 1,014               | 1,376                 | 1,642     | 1,451   |
| Tiny (~span/1000) (r=1)     | 23,894              | 24,349                | 138,049   | 75,880  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 32                  | 35                    | 142       | 20      |
| Half (size≈49.50x49.50x49.50)    | 43                  | 48                    | 1,121     | 198     |
| Quarter (size≈24.75x24.75x24.75) | 45                  | 51                    | 3,640     | 2,476   |
| Unit (size=1)                    | 43                  | 54                    | 172,407   | 77,507  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,142               | 10,460                | 2,277     | 301     |
| 100 neighbors                 | 68,499              | 78,757                | 10,972    | 3,356   |
| 10 neighbors                  | 421,957             | 438,861               | 16,110    | 7,622   |
| 1 neighbor                    | 605,576             | 549,495               | 19,951    | 8,349   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 49 (0.020s)         | 95 (0.011s)           | 17 (0.059s) | 13 (0.075s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 353                 | 574                   | 784       | 160     |
| Half (~span/4) (r=24.75)    | 1,103               | 1,722                 | 2,065     | 810     |
| Quarter (~span/8) (r=12.38) | 2,752               | 4,647                 | 6,047     | 3,222   |
| Tiny (~span/1000) (r=1)     | 27,097              | 31,645                | 176,652   | 99,813  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 589                 | 722                   | 3,127     | 223     |
| Half (size≈49.50x49.50x4.5)     | 700                 | 843                   | 9,226     | 3,388   |
| Quarter (size≈24.75x24.75x2.25) | 701                 | 859                   | 44,316    | 23,955  |
| Unit (size=1)                   | 710                 | 884                   | 227,567   | 104,784 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,922               | 12,002                | 1,591     | 269     |
| 100 neighbors                 | 41,124              | 46,685                | 9,259     | 2,229   |
| 10 neighbors                  | 501,696             | 313,258               | 18,873    | 7,570   |
| 1 neighbor                    | 530,497             | 249,329               | 29,696    | 12,113  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 597 (0.002s)        | 757 (0.001s)          | 585 (0.002s) | 445 (0.002s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 5,262               | 5,030                 | 9,080     | 1,993   |
| Half (~span/4) (r=24.75)    | 6,413               | 6,837                 | 8,769     | 3,902   |
| Quarter (~span/8) (r=12.38) | 6,215               | 7,237                 | 11,015    | 6,784   |
| Tiny (~span/1000) (r=1)     | 42,249              | 40,496                | 222,054   | 159,612 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,180               | 6,221                 | 26,888    | 3,596   |
| Half (size≈49.50x4.5x4.5)      | 7,002               | 7,056                 | 42,907    | 37,731  |
| Quarter (size≈24.75x2.25x2.25) | 7,136               | 7,113                 | 158,244   | 122,000 |
| Unit (size=1)                  | 7,205               | 7,081                 | 306,541   | 165,579 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 10,107              | 11,236                | 630       | 181     |
| 100 neighbors                 | 54,746              | 72,949                | 5,932     | 2,231   |
| 10 neighbors                  | 511,989             | 444,650               | 27,169    | 12,772  |
| 1 neighbor                    | 774,338             | 722,762               | 46,144    | 21,760  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 4,990 (0.000s)      | 7,122 (0.000s)        | 4,175 (0.000s) | 1,313 (0.001s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 13,368              | 16,093                | 25,028    | 20,673  |
| Half (~span/4) (r=2.25)    | 55,618              | 66,354                | 125,539   | 141,115 |
| Quarter (~span/8) (r=1.13) | 65,473              | 68,523                | 343,899   | 231,365 |
| Tiny (~span/1000) (r=1)    | 64,884              | 67,843                | 346,237   | 231,952 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 57,250              | 61,263                | 320,662   | 36,119  |
| Half (size≈4.5x4.5x4.5)       | 61,709              | 67,642                | 181,824   | 184,912 |
| Quarter (size≈2.25x2.25x2.25) | 60,877              | 69,497                | 475,576   | 246,068 |
| Unit (size=1)                 | 61,202              | 70,790                | 479,456   | 246,846 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 16,925              | 15,572                | 3,215     | 610     |
| 100 neighbors                 | 74,947              | 71,538                | 15,612    | 3,990   |
| 10 neighbors                  | 510,542             | 454,829               | 74,723    | 29,764  |
| 1 neighbor                    | 763,904             | 768,537               | 84,866    | 39,058  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 39,840 (0.000s)     | 37,453 (0.000s)       | 27,700 (0.000s) | 13,513 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 141,978             | 139,537               | 302,468   | 179,380 |
| Half (~span/4) (r=2.25)    | 168,368             | 165,953               | 323,192   | 293,657 |
| Quarter (~span/8) (r=1.13) | 168,652             | 170,762               | 399,984   | 440,774 |
| Tiny (~span/1000) (r=1)    | 167,043             | 170,410               | 406,480   | 423,705 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 530,596             | 523,389               | 1,854,271 | 337,188 |
| Half (size≈4.5x2x1)     | 551,750             | 554,808               | 474,856   | 481,576 |
| Quarter (size≈2.25x1x1) | 563,382             | 556,654               | 716,863   | 797,480 |
| Unit (size=1)           | 556,056             | 558,362               | 719,181   | 812,017 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 97,640              | 94,093                | 67,621    | 70,152  |
| 10 neighbors                  | 797,063             | 618,875               | 107,613   | 90,858  |
| 1 neighbor                    | 1,058,863           | 752,441               | 177,637   | 193,474 |

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
