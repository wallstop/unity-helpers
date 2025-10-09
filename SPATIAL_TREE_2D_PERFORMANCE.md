# 2D Spatial Tree Performance Benchmarks

This document contains performance benchmarks for the 2D spatial tree implementations in Unity Helpers.

## Available 2D Spatial Trees

- **QuadTree2D** - Easiest to use, good all-around performance
- **KDTree2D** - Balanced and unbalanced variants available
- **RTree2D** - Optimized for bounding box queries

## Performance Benchmarks

<!-- SPATIAL_TREE_BENCHMARKS_START -->
<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000,000 entries | 3 (0.252s) | 6 (0.158s) | 2 (0.403s) | 2 (0.375s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 58 | 58 | 55 | 7 |
| Half (~span/4) (r=249.8) | 236 | 236 | 208 | 27 |
| Quarter (~span/8) (r=124.9) | 940 | 946 | 811 | 117 |
| Tiny (~span/1000) (r=1) | 101,802 | 105,698 | 143,121 | 107,620 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=999.0x999.0) | 267 | 317 | 283 | 16 |
| Half (size=499.5x499.5) | 1,808 | 1,817 | 1,206 | 62 |
| Quarter (size=249.8x249.8) | 7,114 | 7,124 | 3,797 | 373 |
| Unit (size=1) | 149,343 | 153,221 | 197,061 | 112,686 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 8,459 | 16,886 | 12,586 | 68,375 |
| 100 neighbors | 78,945 | 76,550 | 77,646 | 173,804 |
| 10 neighbors | 370,219 | 355,485 | 217,800 | 273,516 |
| 1 neighbor | 517,682 | 453,164 | 267,902 | 276,845 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 21 (0.047s) | 82 (0.012s) | 49 (0.020s) | 47 (0.021s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 600 | 601 | 598 | 67 |
| Half (~span/4) (r=99.75) | 1,354 | 1,358 | 1,244 | 179 |
| Quarter (~span/8) (r=49.88) | 4,664 | 5,175 | 4,303 | 716 |
| Tiny (~span/1000) (r=1) | 124,732 | 128,239 | 178,002 | 145,520 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=399.0x249.0) | 4,507 | 4,548 | 4,617 | 189 |
| Half (size=199.5x124.5) | 9,540 | 11,927 | 7,976 | 962 |
| Quarter (size=99.75x62.25) | 26,137 | 32,237 | 19,409 | 3,792 |
| Unit (size=1) | 176,893 | 185,549 | 240,383 | 154,675 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 9,971 | 9,798 | 11,218 | 68,301 |
| 100 neighbors | 47,484 | 92,935 | 54,871 | 225,120 |
| 10 neighbors | 450,532 | 371,690 | 276,389 | 335,128 |
| 1 neighbor | 477,713 | 579,590 | 293,557 | 334,766 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 521 (0.002s) | 773 (0.001s) | 549 (0.002s) | 504 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 5,944 | 5,931 | 5,931 | 732 |
| Half (~span/4) (r=24.75) | 22,243 | 22,227 | 13,863 | 2,913 |
| Quarter (~span/8) (r=12.38) | 44,258 | 51,489 | 38,021 | 12,184 |
| Tiny (~span/1000) (r=1) | 167,645 | 162,897 | 233,094 | 167,050 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=99.00x99.00) | 44,813 | 45,066 | 46,628 | 2,407 |
| Half (size=49.50x49.50) | 165,549 | 166,038 | 35,932 | 9,289 |
| Quarter (size=24.75x24.75) | 75,930 | 104,475 | 74,818 | 35,545 |
| Unit (size=1) | 241,479 | 234,430 | 318,334 | 179,407 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 13,188 | 12,952 | 14,444 | 63,048 |
| 100 neighbors | 62,306 | 56,729 | 94,863 | 214,219 |
| 10 neighbors | 421,369 | 402,729 | 278,563 | 405,801 |
| 1 neighbor | 606,064 | 610,139 | 365,916 | 448,831 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 5,109 (0.000s) | 7,710 (0.000s) | 4,889 (0.000s) | 4,520 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 58,099 | 58,159 | 56,962 | 7,377 |
| Half (~span/4) (r=12.25) | 60,248 | 76,335 | 56,792 | 14,663 |
| Quarter (~span/8) (r=6.13) | 96,020 | 107,961 | 95,166 | 37,897 |
| Tiny (~span/1000) (r=1) | 240,078 | 226,697 | 335,293 | 250,738 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=49.00x19.00) | 478,991 | 499,136 | 530,660 | 24,069 |
| Half (size=24.50x9.5) | 165,589 | 291,584 | 125,454 | 74,481 |
| Quarter (size=12.25x4.75) | 265,602 | 290,114 | 192,442 | 166,719 |
| Unit (size=1) | 332,322 | 338,726 | 457,727 | 279,247 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 46,339 | 47,104 | 40,093 | 68,980 |
| 100 neighbors | 81,051 | 77,674 | 90,355 | 266,308 |
| 10 neighbors | 521,324 | 575,074 | 377,057 | 509,481 |
| 1 neighbor | 725,581 | 583,838 | 408,620 | 519,405 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 42,372 (0.000s) | 37,037 (0.000s) | 26,666 (0.000s) | 21,691 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 505,422 | 492,303 | 501,612 | 72,703 |
| Half (~span/4) (r=2.25) | 431,070 | 418,985 | 256,136 | 236,593 |
| Quarter (~span/8) (r=1.13) | 430,860 | 435,887 | 593,806 | 337,366 |
| Tiny (~span/1000) (r=1) | 430,791 | 435,768 | 593,255 | 336,779 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=9x9) | 2,314,006 | 2,442,747 | 2,404,908 | 222,111 |
| Half (size=4.5x4.5) | 561,171 | 560,301 | 361,478 | 367,071 |
| Quarter (size=2.25x2.25) | 584,411 | 566,869 | 787,810 | 386,934 |
| Unit (size=1) | 564,496 | 600,581 | 787,573 | 366,112 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 159,353 | 165,774 | 197,789 | 312,940 |
| 10 neighbors | 490,616 | 509,549 | 558,611 | 707,946 |
| 1 neighbor | 619,095 | 746,230 | 620,904 | 792,162 |
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
