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
| 1,000,000 entries | 4 (0.247s) | 5 (0.187s) | 3 (0.285s) | 2 (0.348s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 59 | 58 | 56 | 7 |
| Half (~span/4) (r=249.8) | 237 | 235 | 215 | 27 |
| Quarter (~span/8) (r=124.9) | 946 | 939 | 806 | 117 |
| Tiny (~span/1000) (r=1) | 103,107 | 104,622 | 141,862 | 106,276 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=999.0x999.0) | 359 | 387 | 329 | 16 |
| Half (size=499.5x499.5) | 1,854 | 1,848 | 1,217 | 66 |
| Quarter (size=249.8x249.8) | 7,308 | 7,271 | 3,801 | 376 |
| Unit (size=1) | 146,762 | 151,751 | 196,413 | 112,248 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 2,172 | 4,358 | 3,194 | 65,204 |
| 100 neighbors | 24,835 | 23,163 | 24,385 | 157,811 |
| 10 neighbors | 288,446 | 240,205 | 190,653 | 216,205 |
| 1 neighbor | 465,012 | 500,096 | 176,138 | 235,963 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 50 (0.020s) | 82 (0.012s) | 42 (0.023s) | 46 (0.021s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 601 | 602 | 593 | 71 |
| Half (~span/4) (r=99.75) | 1,356 | 1,352 | 1,235 | 183 |
| Quarter (~span/8) (r=49.88) | 4,673 | 5,127 | 4,260 | 718 |
| Tiny (~span/1000) (r=1) | 127,735 | 126,876 | 174,736 | 145,721 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=399.0x249.0) | 4,561 | 4,626 | 4,566 | 228 |
| Half (size=199.5x124.5) | 9,741 | 11,911 | 7,997 | 970 |
| Quarter (size=99.75x62.25) | 25,768 | 32,226 | 19,597 | 3,800 |
| Unit (size=1) | 184,335 | 183,492 | 238,824 | 154,088 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 2,718 | 2,722 | 2,832 | 65,344 |
| 100 neighbors | 15,190 | 31,000 | 15,124 | 193,287 |
| 10 neighbors | 274,987 | 240,540 | 222,541 | 283,630 |
| 1 neighbor | 325,808 | 489,646 | 254,666 | 287,294 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 530 (0.002s) | 804 (0.001s) | 546 (0.002s) | 472 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 5,939 | 5,947 | 5,868 | 728 |
| Half (~span/4) (r=24.75) | 22,245 | 22,145 | 13,743 | 2,895 |
| Quarter (~span/8) (r=12.38) | 44,307 | 51,052 | 38,022 | 12,095 |
| Tiny (~span/1000) (r=1) | 166,163 | 161,160 | 233,931 | 166,851 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=99.00x99.00) | 45,781 | 46,133 | 46,523 | 2,388 |
| Half (size=49.50x49.50) | 166,089 | 165,865 | 37,609 | 9,233 |
| Quarter (size=24.75x24.75) | 75,042 | 103,207 | 75,726 | 35,182 |
| Unit (size=1) | 239,139 | 231,814 | 318,370 | 176,978 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 3,639 | 3,472 | 3,739 | 59,062 |
| 100 neighbors | 18,238 | 17,125 | 29,690 | 211,150 |
| 10 neighbors | 266,230 | 261,326 | 186,181 | 336,525 |
| 1 neighbor | 481,720 | 556,128 | 283,814 | 384,134 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 5,336 (0.000s) | 7,246 (0.000s) | 4,835 (0.000s) | 4,426 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 57,414 | 58,063 | 57,299 | 7,367 |
| Half (~span/4) (r=12.25) | 59,828 | 75,859 | 57,017 | 14,660 |
| Quarter (~span/8) (r=6.13) | 94,968 | 107,976 | 95,260 | 37,894 |
| Tiny (~span/1000) (r=1) | 237,548 | 226,698 | 335,919 | 248,125 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=49.00x19.00) | 494,282 | 491,324 | 514,053 | 23,938 |
| Half (size=24.50x9.5) | 165,024 | 288,620 | 126,952 | 74,185 |
| Quarter (size=12.25x4.75) | 260,825 | 286,115 | 194,106 | 171,388 |
| Unit (size=1) | 339,194 | 335,318 | 463,648 | 267,899 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 77,160 | 77,849 | 56,308 | 67,167 |
| 100 neighbors | 23,928 | 22,347 | 27,114 | 247,440 |
| 10 neighbors | 432,011 | 422,060 | 236,533 | 427,681 |
| 1 neighbor | 627,691 | 453,725 | 251,426 | 379,354 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 11,273 (0.000s) | 36,630 (0.000s) | 29,850 (0.000s) | 21,551 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 475,823 | 500,595 | 498,910 | 72,771 |
| Half (~span/4) (r=2.25) | 430,457 | 431,471 | 254,456 | 236,793 |
| Quarter (~span/8) (r=1.13) | 430,419 | 431,062 | 589,525 | 339,089 |
| Tiny (~span/1000) (r=1) | 430,842 | 428,426 | 579,176 | 338,411 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=9x9) | 2,468,755 | 2,389,191 | 2,490,357 | 222,956 |
| Half (size=4.5x4.5) | 563,312 | 558,511 | 368,042 | 368,222 |
| Quarter (size=2.25x2.25) | 566,003 | 591,958 | 790,906 | 389,098 |
| Unit (size=1) | 586,686 | 594,412 | 788,921 | 368,923 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 224,263 | 222,333 | 273,070 | 296,841 |
| 10 neighbors | 379,243 | 343,914 | 592,798 | 601,512 |
| 1 neighbor | 378,896 | 633,078 | 530,761 | 665,095 |
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
