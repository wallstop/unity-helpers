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
| 1,000,000 entries | 2 (0.391s)          | 3 (0.317s)            | 2 (0.403s) | 2 (0.368s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 17                  | 20                    | 33        | 14      |
| Half (~span/4) (r=24.75)    | 138                 | 161                   | 253       | 154     |
| Quarter (~span/8) (r=12.38) | 943                 | 1,225                 | 1,636     | 1,398   |
| Tiny (~span/1000) (r=1)     | 23,633              | 23,399                | 138,242   | 61,388  |

##### Get Elements In Bounds

| Get Elements In Bounds           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x99.00)    | 35                  | 38                    | 204       | 20      |
| Half (size≈49.50x49.50x49.50)    | 40                  | 46                    | 1,248     | 272     |
| Quarter (size≈24.75x24.75x24.75) | 41                  | 48                    | 4,043     | 2,459   |
| Unit (size=1)                    | 41                  | 49                    | 182,201   | 75,627  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 5,823               | 10,001                | 2,321     | 297     |
| 100 neighbors                 | 62,378              | 68,501                | 10,973    | 3,386   |
| 10 neighbors                  | 394,113             | 400,030               | 15,886    | 7,915   |
| 1 neighbor                    | 542,886             | 429,790               | 19,854    | 8,605   |

#### **100,000 entries**

##### Construction

| Construction    | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D   | RTree3D     |
| --------------- | ------------------- | --------------------- | ----------- | ----------- |
| 100,000 entries | 34 (0.029s)         | 48 (0.021s)           | 65 (0.015s) | 43 (0.023s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 339                 | 491                   | 792       | 181     |
| Half (~span/4) (r=24.75)    | 1,029               | 1,461                 | 2,046     | 760     |
| Quarter (~span/8) (r=12.38) | 2,543               | 4,039                 | 5,884     | 3,145   |
| Tiny (~span/1000) (r=1)     | 26,019              | 29,892                | 177,305   | 81,121  |

##### Get Elements In Bounds

| Get Elements In Bounds          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x99.00x9)       | 610                 | 762                   | 2,614     | 346     |
| Half (size≈49.50x49.50x4.5)     | 712                 | 902                   | 9,136     | 3,528   |
| Quarter (size≈24.75x24.75x2.25) | 729                 | 930                   | 46,338    | 23,782  |
| Unit (size=1)                   | 740                 | 939                   | 237,321   | 99,906  |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 6,626               | 11,994                | 1,586     | 262     |
| 100 neighbors                 | 37,626              | 44,284                | 9,081     | 2,200   |
| 10 neighbors                  | 417,112             | 231,243               | 19,133    | 7,515   |
| 1 neighbor                    | 449,374             | 324,373               | 30,223    | 11,948  |

#### **10,000 entries**

##### Construction

| Construction   | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D    | RTree3D      |
| -------------- | ------------------- | --------------------- | ------------ | ------------ |
| 10,000 entries | 451 (0.002s)        | 454 (0.002s)          | 605 (0.002s) | 196 (0.005s) |

##### Elements In Range

| Elements In Range           | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=49.50)    | 4,675               | 4,518                 | 9,099     | 2,034   |
| Half (~span/4) (r=24.75)    | 5,823               | 6,354                 | 8,794     | 3,922   |
| Quarter (~span/8) (r=12.38) | 5,866               | 6,767                 | 10,944    | 6,752   |
| Tiny (~span/1000) (r=1)     | 41,567              | 38,086                | 214,981   | 128,576 |

##### Get Elements In Bounds

| Get Elements In Bounds         | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ------------------------------ | ------------------- | --------------------- | --------- | ------- |
| Full (size≈99.00x9x9)          | 6,291               | 6,249                 | 25,591    | 3,619   |
| Half (size≈49.50x4.5x4.5)      | 7,097               | 7,141                 | 45,006    | 37,688  |
| Quarter (size≈24.75x2.25x2.25) | 7,245               | 7,264                 | 163,085   | 121,891 |
| Unit (size=1)                  | 7,307               | 7,337                 | 312,237   | 164,570 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 9,657               | 10,507                | 616       | 184     |
| 100 neighbors                 | 49,027              | 67,963                | 5,817     | 2,182   |
| 10 neighbors                  | 446,049             | 428,934               | 26,981    | 12,798  |
| 1 neighbor                    | 623,216             | 592,038               | 43,952    | 21,591  |

#### **1,000 entries**

##### Construction

| Construction  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D      | RTree3D        |
| ------------- | ------------------- | --------------------- | -------------- | -------------- |
| 1,000 entries | 3,913 (0.000s)      | 5,081 (0.000s)        | 4,323 (0.000s) | 3,658 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 11,696              | 13,823                | 24,969    | 19,971  |
| Half (~span/4) (r=2.25)    | 50,968              | 60,896                | 123,633   | 129,746 |
| Quarter (~span/8) (r=1.13) | 63,064              | 62,765                | 330,025   | 186,475 |
| Tiny (~span/1000) (r=1)    | 63,087              | 62,848                | 329,869   | 197,969 |

##### Get Elements In Bounds

| Get Elements In Bounds        | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x9x9)             | 58,861              | 61,905                | 309,570   | 36,019  |
| Half (size≈4.5x4.5x4.5)       | 64,527              | 69,802                | 192,755   | 179,536 |
| Quarter (size≈2.25x2.25x2.25) | 65,139              | 70,314                | 482,726   | 233,477 |
| Unit (size=1)                 | 63,144              | 71,425                | 483,025   | 229,874 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 500 neighbors                 | 15,710              | 14,734                | 3,184     | 600     |
| 100 neighbors                 | 66,966              | 62,730                | 15,247    | 4,005   |
| 10 neighbors                  | 458,678             | 398,089               | 72,823    | 32,318  |
| 1 neighbor                    | 683,259             | 662,761               | 82,887    | 43,767  |

#### **100 entries**

##### Construction

| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D       | RTree3D         |
| ------------ | ------------------- | --------------------- | --------------- | --------------- |
| 100 entries  | 33,222 (0.000s)     | 32,573 (0.000s)       | 20,491 (0.000s) | 19,841 (0.000s) |

##### Elements In Range

| Elements In Range          | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| -------------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (~span/2) (r=4.5)     | 127,317             | 128,629               | 291,676   | 172,914 |
| Half (~span/4) (r=2.25)    | 147,989             | 150,244               | 307,046   | 270,030 |
| Quarter (~span/8) (r=1.13) | 147,905             | 151,405               | 385,634   | 379,165 |
| Tiny (~span/1000) (r=1)    | 147,928             | 147,331               | 382,213   | 378,916 |

##### Get Elements In Bounds

| Get Elements In Bounds  | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------- | ------------------- | --------------------- | --------- | ------- |
| Full (size≈9x4x1)       | 522,824             | 497,894               | 1,759,234 | 320,145 |
| Half (size≈4.5x2x1)     | 537,503             | 544,668               | 498,113   | 445,151 |
| Quarter (size≈2.25x1x1) | 551,381             | 566,439               | 739,441   | 662,192 |
| Unit (size=1)           | 551,255             | 558,590               | 739,304   | 718,275 |

##### Approximate Nearest Neighbors

| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| ----------------------------- | ------------------- | --------------------- | --------- | ------- |
| 100 neighbors (max)           | 87,362              | 84,326                | 67,051    | 64,893  |
| 10 neighbors                  | 593,261             | 519,728               | 94,913    | 101,228 |
| 1 neighbor                    | 831,879             | 681,161               | 168,582   | 221,590 |

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
