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
| 1,000,000 entries | 4 (0.248s) | 6 (0.159s) | 4 (0.225s) | 1 (0.612s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 58 | 58 | 56 | 7 |
| Half (~span/4) (r=249.8) | 237 | 236 | 215 | 28 |
| Quarter (~span/8) (r=124.9) | 945 | 946 | 812 | 119 |
| Tiny (~span/1000) (r=1) | 103,288 | 105,810 | 143,517 | 107,355 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=999.0x999.0) | 352 | 357 | 310 | 17 |
| Half (size=499.5x499.5) | 1,773 | 1,807 | 1,216 | 72 |
| Quarter (size=249.8x249.8) | 7,094 | 7,117 | 3,790 | 376 |
| Unit (size=1) | 149,575 | 153,213 | 197,467 | 112,734 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 8,407 | 16,890 | 12,751 | 69,179 |
| 100 neighbors | 78,974 | 76,111 | 78,890 | 176,697 |
| 10 neighbors | 374,257 | 351,870 | 235,238 | 283,168 |
| 1 neighbor | 517,619 | 506,054 | 293,278 | 279,859 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 50 (0.020s) | 82 (0.012s) | 17 (0.058s) | 48 (0.021s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 602 | 601 | 599 | 70 |
| Half (~span/4) (r=99.75) | 1,355 | 1,360 | 1,246 | 185 |
| Quarter (~span/8) (r=49.88) | 4,668 | 5,174 | 4,298 | 717 |
| Tiny (~span/1000) (r=1) | 127,823 | 128,261 | 178,848 | 146,001 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=399.0x249.0) | 4,562 | 4,524 | 4,660 | 228 |
| Half (size=199.5x124.5) | 9,536 | 11,913 | 7,972 | 963 |
| Quarter (size=99.75x62.25) | 25,364 | 32,543 | 19,652 | 3,786 |
| Unit (size=1) | 184,361 | 185,582 | 245,033 | 155,050 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 9,975 | 9,908 | 11,613 | 69,795 |
| 100 neighbors | 49,879 | 93,287 | 54,578 | 228,180 |
| 10 neighbors | 493,850 | 367,811 | 297,557 | 348,737 |
| 1 neighbor | 471,522 | 565,528 | 317,822 | 339,909 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 541 (0.002s) | 811 (0.001s) | 542 (0.002s) | 506 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 5,934 | 5,940 | 5,941 | 734 |
| Half (~span/4) (r=24.75) | 22,283 | 22,219 | 13,880 | 2,924 |
| Quarter (~span/8) (r=12.38) | 44,244 | 51,321 | 38,094 | 12,231 |
| Tiny (~span/1000) (r=1) | 167,828 | 162,958 | 234,403 | 169,010 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=99.00x99.00) | 45,173 | 45,133 | 46,154 | 2,408 |
| Half (size=49.50x49.50) | 145,708 | 167,355 | 37,303 | 9,307 |
| Quarter (size=24.75x24.75) | 75,949 | 104,140 | 75,671 | 35,522 |
| Unit (size=1) | 241,281 | 234,274 | 319,360 | 181,643 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 13,228 | 13,080 | 14,445 | 65,401 |
| 100 neighbors | 62,356 | 57,270 | 96,529 | 235,170 |
| 10 neighbors | 417,694 | 397,027 | 313,977 | 391,857 |
| 1 neighbor | 604,597 | 609,415 | 374,153 | 460,980 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 4,681 (0.000s) | 7,880 (0.000s) | 4,940 (0.000s) | 4,746 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 58,119 | 58,222 | 57,346 | 7,427 |
| Half (~span/4) (r=12.25) | 60,432 | 76,555 | 57,084 | 14,720 |
| Quarter (~span/8) (r=6.13) | 95,835 | 108,793 | 95,512 | 38,058 |
| Tiny (~span/1000) (r=1) | 240,153 | 237,629 | 337,820 | 252,998 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=49.00x19.00) | 500,457 | 500,403 | 535,474 | 24,121 |
| Half (size=24.50x9.5) | 167,144 | 290,602 | 126,605 | 75,046 |
| Quarter (size=12.25x4.75) | 271,422 | 289,356 | 193,934 | 177,216 |
| Unit (size=1) | 342,155 | 338,785 | 462,406 | 282,204 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 46,439 | 47,115 | 40,244 | 70,325 |
| 100 neighbors | 78,088 | 78,033 | 91,927 | 273,414 |
| 10 neighbors | 487,128 | 571,885 | 404,234 | 504,993 |
| 1 neighbor | 713,191 | 583,436 | 405,391 | 594,313 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 41,322 (0.000s) | 36,363 (0.000s) | 28,985 (0.000s) | 20,746 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 505,017 | 505,807 | 503,274 | 72,941 |
| Half (~span/4) (r=2.25) | 431,130 | 436,156 | 257,530 | 238,440 |
| Quarter (~span/8) (r=1.13) | 431,153 | 435,989 | 600,147 | 340,937 |
| Tiny (~span/1000) (r=1) | 431,093 | 436,102 | 599,615 | 340,442 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=9x9) | 2,319,854 | 2,457,116 | 2,406,395 | 223,138 |
| Half (size=4.5x4.5) | 572,813 | 532,923 | 365,360 | 350,989 |
| Quarter (size=2.25x2.25) | 594,580 | 601,272 | 789,386 | 395,345 |
| Unit (size=1) | 593,752 | 601,715 | 788,295 | 395,360 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 166,111 | 165,476 | 202,015 | 323,752 |
| 10 neighbors | 442,805 | 493,913 | 604,111 | 719,179 |
| 1 neighbor | 628,927 | 777,014 | 677,684 | 848,106 |
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
