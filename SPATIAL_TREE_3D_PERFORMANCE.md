# 3D Spatial Tree Performance Benchmarks

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
| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 1,000,000 entries | 2 (0.394s) | 3 (0.325s) | 3 (0.325s) | 2 (0.385s) |

##### Elements In Range
| Elements In Range | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 17 | 20 | 31 | 14 |
| Half (~span/4) (r=24.75) | 129 | 162 | 237 | 135 |
| Quarter (~span/8) (r=12.38) | 891 | 1,223 | 1,607 | 1,481 |
| Tiny (~span/1000) (r=1) | 21,731 | 23,529 | 137,000 | 73,813 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (size≈99.00x99.00x99.00) | 32 | 38 | 198 | 19 |
| Half (size≈49.50x49.50x49.50) | 37 | 45 | 1,246 | 256 |
| Quarter (size≈24.75x24.75x24.75) | 36 | 46 | 4,033 | 2,539 |
| Unit (size=1) | 36 | 47 | 183,434 | 77,848 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 5,447 | 10,119 | 2,308 | 302 |
| 100 neighbors | 64,562 | 68,128 | 10,908 | 3,240 |
| 10 neighbors | 390,697 | 406,818 | 16,018 | 7,156 |
| 1 neighbor | 516,616 | 386,312 | 19,327 | 7,469 |

#### **100,000 entries**

##### Construction
| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 33 (0.030s) | 17 (0.058s) | 61 (0.016s) | 44 (0.022s) |

##### Elements In Range
| Elements In Range | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 319 | 467 | 765 | 210 |
| Half (~span/4) (r=24.75) | 976 | 1,387 | 2,046 | 842 |
| Quarter (~span/8) (r=12.38) | 2,551 | 3,813 | 5,977 | 3,349 |
| Tiny (~span/1000) (r=1) | 26,018 | 28,143 | 174,232 | 101,400 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (size≈99.00x99.00x9) | 597 | 732 | 2,692 | 350 |
| Half (size≈49.50x49.50x4.5) | 720 | 899 | 9,292 | 3,522 |
| Quarter (size≈24.75x24.75x2.25) | 734 | 933 | 45,669 | 23,588 |
| Unit (size=1) | 751 | 941 | 227,868 | 102,949 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 6,688 | 12,102 | 1,568 | 271 |
| 100 neighbors | 38,110 | 41,521 | 8,897 | 2,281 |
| 10 neighbors | 408,678 | 300,596 | 19,066 | 7,729 |
| 1 neighbor | 406,342 | 325,825 | 29,927 | 12,058 |

#### **10,000 entries**

##### Construction
| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 450 (0.002s) | 253 (0.004s) | 594 (0.002s) | 433 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 4,743 | 4,409 | 8,967 | 2,208 |
| Half (~span/4) (r=24.75) | 5,830 | 6,231 | 8,858 | 4,282 |
| Quarter (~span/8) (r=12.38) | 5,853 | 6,811 | 11,152 | 7,529 |
| Tiny (~span/1000) (r=1) | 41,040 | 37,398 | 217,696 | 158,423 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (size≈99.00x9x9) | 6,447 | 6,212 | 26,517 | 3,495 |
| Half (size≈49.50x4.5x4.5) | 7,317 | 7,078 | 44,725 | 37,707 |
| Quarter (size≈24.75x2.25x2.25) | 7,475 | 7,231 | 162,137 | 120,542 |
| Unit (size=1) | 7,556 | 7,260 | 317,933 | 155,604 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 9,652 | 10,501 | 634 | 185 |
| 100 neighbors | 49,473 | 66,393 | 5,919 | 2,264 |
| 10 neighbors | 417,662 | 445,097 | 27,030 | 13,387 |
| 1 neighbor | 627,733 | 581,761 | 45,150 | 22,538 |

#### **1,000 entries**

##### Construction
| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 3,933 (0.000s) | 5,241 (0.000s) | 4,233 (0.000s) | 4,061 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 11,490 | 14,069 | 23,711 | 20,852 |
| Half (~span/4) (r=2.25) | 50,016 | 62,135 | 122,201 | 143,419 |
| Quarter (~span/8) (r=1.13) | 59,901 | 63,986 | 338,247 | 223,558 |
| Tiny (~span/1000) (r=1) | 59,873 | 64,107 | 339,907 | 225,062 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (size≈9x9x9) | 56,539 | 63,374 | 256,995 | 36,075 |
| Half (size≈4.5x4.5x4.5) | 63,467 | 68,471 | 190,999 | 180,038 |
| Quarter (size≈2.25x2.25x2.25) | 65,548 | 69,820 | 484,174 | 228,739 |
| Unit (size=1) | 65,568 | 70,774 | 490,475 | 229,448 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 15,438 | 14,674 | 3,196 | 596 |
| 100 neighbors | 70,051 | 61,290 | 14,653 | 3,989 |
| 10 neighbors | 469,916 | 396,255 | 71,457 | 32,400 |
| 1 neighbor | 670,080 | 625,986 | 81,710 | 43,585 |

#### **100 entries**

##### Construction
| Construction | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 100 entries | 41,493 (0.000s) | 34,843 (0.000s) | 26,385 (0.000s) | 20,876 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 125,460 | 126,170 | 281,626 | 197,345 |
| Half (~span/4) (r=2.25) | 146,451 | 148,409 | 299,012 | 314,020 |
| Quarter (~span/8) (r=1.13) | 146,694 | 149,869 | 373,044 | 429,270 |
| Tiny (~span/1000) (r=1) | 146,600 | 149,961 | 391,603 | 428,215 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| Full (size≈9x4x1) | 523,481 | 537,986 | 1,831,870 | 328,086 |
| Half (size≈4.5x2x1) | 544,006 | 569,477 | 499,023 | 442,692 |
| Quarter (size≈2.25x1x1) | 576,023 | 531,772 | 747,302 | 740,334 |
| Unit (size=1) | 575,269 | 549,412 | 744,102 | 741,094 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree3D (Balanced) | KDTree3D (Unbalanced) | OctTree3D | RTree3D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 92,129 | 85,122 | 64,123 | 64,932 |
| 10 neighbors | 603,891 | 516,340 | 105,474 | 98,414 |
| 1 neighbor | 860,501 | 627,865 | 173,332 | 217,122 |
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
