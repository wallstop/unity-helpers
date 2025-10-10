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
| 1,000,000 entries | 3 (0.304s) | 3 (0.319s) | 3 (0.290s) | 2 (0.340s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=499.5) | 59 | 58 | 54 | 7 |
| Half (~span/4) (r=249.8) | 228 | 237 | 216 | 28 |
| Quarter (~span/8) (r=124.9) | 927 | 946 | 812 | 119 |
| Tiny (~span/1000) (r=1) | 103,328 | 105,714 | 141,527 | 105,514 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=999.0x999.0) | 361 | 376 | 335 | 17 |
| Half (size=499.5x499.5) | 1,746 | 1,757 | 1,204 | 69 |
| Quarter (size=249.8x249.8) | 6,843 | 6,890 | 3,681 | 365 |
| Unit (size=1) | 143,864 | 147,338 | 185,632 | 108,229 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 8,204 | 16,259 | 12,310 | 67,580 |
| 100 neighbors | 75,895 | 73,402 | 76,297 | 172,100 |
| 10 neighbors | 367,976 | 343,614 | 245,086 | 283,006 |
| 1 neighbor | 486,137 | 530,620 | 275,296 | 292,762 |

#### **100,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100,000 entries | 18 (0.053s) | 81 (0.012s) | 49 (0.020s) | 48 (0.021s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=199.5) | 601 | 602 | 600 | 71 |
| Half (~span/4) (r=99.75) | 1,355 | 1,306 | 1,236 | 178 |
| Quarter (~span/8) (r=49.88) | 4,563 | 4,962 | 4,139 | 719 |
| Tiny (~span/1000) (r=1) | 122,570 | 124,811 | 171,320 | 145,776 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=399.0x249.0) | 4,368 | 4,403 | 4,513 | 225 |
| Half (size=199.5x124.5) | 9,237 | 11,904 | 7,705 | 967 |
| Quarter (size=99.75x62.25) | 26,086 | 33,120 | 18,923 | 3,817 |
| Unit (size=1) | 177,508 | 185,170 | 228,117 | 154,866 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 10,027 | 9,919 | 11,742 | 70,106 |
| 100 neighbors | 47,569 | 93,191 | 54,380 | 233,581 |
| 10 neighbors | 445,211 | 370,042 | 286,676 | 341,260 |
| 1 neighbor | 477,104 | 588,433 | 311,398 | 345,215 |

#### **10,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 10,000 entries | 517 (0.002s) | 824 (0.001s) | 548 (0.002s) | 499 (0.002s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=49.50) | 5,766 | 5,705 | 5,942 | 734 |
| Half (~span/4) (r=24.75) | 22,288 | 21,426 | 13,876 | 2,923 |
| Quarter (~span/8) (r=12.38) | 44,238 | 51,348 | 38,118 | 12,189 |
| Tiny (~span/1000) (r=1) | 167,590 | 156,737 | 234,407 | 166,382 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=99.00x99.00) | 44,857 | 42,925 | 45,216 | 2,401 |
| Half (size=49.50x49.50) | 139,618 | 157,543 | 34,969 | 8,955 |
| Quarter (size=24.75x24.75) | 72,925 | 100,082 | 72,430 | 34,018 |
| Unit (size=1) | 238,622 | 226,220 | 309,035 | 173,036 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 13,234 | 12,998 | 14,379 | 65,620 |
| 100 neighbors | 62,389 | 54,693 | 95,411 | 237,624 |
| 10 neighbors | 409,880 | 424,774 | 294,694 | 422,341 |
| 1 neighbor | 597,771 | 618,086 | 399,420 | 455,330 |

#### **1,000 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 1,000 entries | 1,558 (0.001s) | 7,412 (0.000s) | 4,859 (0.000s) | 4,595 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=24.50) | 54,633 | 58,222 | 54,796 | 7,191 |
| Half (~span/4) (r=12.25) | 58,119 | 76,530 | 54,905 | 14,143 |
| Quarter (~span/8) (r=6.13) | 94,382 | 108,724 | 91,587 | 36,696 |
| Tiny (~span/1000) (r=1) | 240,004 | 237,664 | 338,031 | 242,020 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=49.00x19.00) | 500,069 | 493,875 | 540,088 | 23,725 |
| Half (size=24.50x9.5) | 167,539 | 279,951 | 126,705 | 74,924 |
| Quarter (size=12.25x4.75) | 263,959 | 278,731 | 194,147 | 174,585 |
| Unit (size=1) | 328,587 | 325,380 | 455,292 | 280,584 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 500 neighbors | 44,316 | 45,060 | 38,697 | 70,717 |
| 100 neighbors | 78,461 | 75,168 | 88,715 | 276,117 |
| 10 neighbors | 489,638 | 560,651 | 392,910 | 500,805 |
| 1 neighbor | 734,837 | 571,829 | 395,078 | 575,851 |

#### **100 entries**

##### Construction
| Construction | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 entries | 40,485 (0.000s) | 36,900 (0.000s) | 29,940 (0.000s) | 11,820 (0.000s) |

##### Elements In Range
| Elements In Range | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (~span/2) (r=4.5) | 502,666 | 502,543 | 505,816 | 69,938 |
| Half (~span/4) (r=2.25) | 431,363 | 435,876 | 256,513 | 227,814 |
| Quarter (~span/8) (r=1.13) | 431,076 | 435,933 | 581,956 | 323,699 |
| Tiny (~span/1000) (r=1) | 423,543 | 430,410 | 576,186 | 323,280 |

##### Get Elements In Bounds
| Get Elements In Bounds | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| Full (size=9x9) | 2,318,296 | 2,274,895 | 2,296,320 | 219,822 |
| Half (size=4.5x4.5) | 573,571 | 541,650 | 351,980 | 357,343 |
| Quarter (size=2.25x2.25) | 595,726 | 578,730 | 757,699 | 391,860 |
| Unit (size=1) | 593,841 | 577,751 | 757,814 | 390,467 |

##### Approximate Nearest Neighbors
| Approximate Nearest Neighbors | KDTree2D (Balanced) | KDTree2D (Unbalanced) | QuadTree2D | RTree2D |
| --- | --- | --- | --- | --- |
| 100 neighbors (max) | 161,215 | 159,822 | 194,117 | 314,571 |
| 10 neighbors | 456,968 | 484,669 | 602,982 | 719,223 |
| 1 neighbor | 628,993 | 758,772 | 669,522 | 831,311 |
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
