---
---

# 2D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Fast range/bounds/nearest‑neighbor queries on 2D data without scanning everything.
- Quick picks: QuadTree2D for broad‑phase; KdTree2D (Balanced) for NN; KdTree2D (Unbalanced) for fast rebuilds; RTree2D for bounds‑based data.

This document contains performance benchmarks for the 2D spatial tree implementations in Unity Helpers.

## Available 2D Spatial Trees

- **QuadTree2D** - Easiest to use, good all-around performance
- **KdTree2D** - Balanced and unbalanced variants available
- **RTree2D** - Optimized for bounding box queries

### Correctness & Semantics

- QuadTree2D and KdTree2D (balanced and unbalanced) guarantee the same results for the same input data and the same queries. They are both point-based trees and differ only in construction/query performance characteristics.
- RTree2D is bounds-based (stores rectangles/AABBs), not points. Its spatial knowledge and query semantics operate on rectangles, so its results will intentionally differ for sized objects and bounds intersection queries.

## Performance Benchmarks

<!-- SPATIAL_TREE_BENCHMARKS_START -->

### Datasets

<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">1,000,000 entries</td><td align="right">4 (0.247s)</td><td align="right">2 (0.346s)</td><td align="right">4 (0.223s)</td><td align="right">2 (0.379s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=499.5)</td><td align="right">58</td><td align="right">56</td><td align="right">56</td><td align="right">7</td></tr>
    <tr><td align="left">Half (~span/4) (r=249.8)</td><td align="right">234</td><td align="right">215</td><td align="right">214</td><td align="right">28</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=124.9)</td><td align="right">909</td><td align="right">795</td><td align="right">785</td><td align="right">117</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">8,280</td><td align="right">5,565</td><td align="right">6,973</td><td align="right">6,724</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size=999.0x999.0)</td><td align="right">334</td><td align="right">355</td><td align="right">314</td><td align="right">16</td></tr>
    <tr><td align="left">Half (size=499.5x499.5)</td><td align="right">1,356</td><td align="right">1,367</td><td align="right">1,005</td><td align="right">66</td></tr>
    <tr><td align="left">Quarter (size=249.8x249.8)</td><td align="right">3,148</td><td align="right">3,183</td><td align="right">2,281</td><td align="right">322</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,612</td><td align="right">5,606</td><td align="right">5,577</td><td align="right">2,834</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">3,023</td><td align="right">1,733</td><td align="right">2,148</td><td align="right">1,855</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">2,828</td><td align="right">1,863</td><td align="right">2,308</td><td align="right">1,802</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,958</td><td align="right">1,899</td><td align="right">1,641</td><td align="right">1,692</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,961</td><td align="right">1,957</td><td align="right">1,464</td><td align="right">1,465</td></tr>
  </tbody>
</table>

#### **100,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100,000 entries</td><td align="right">49 (0.020s)</td><td align="right">77 (0.013s)</td><td align="right">48 (0.021s)</td><td align="right">43 (0.023s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=199.5)</td><td align="right">539</td><td align="right">527</td><td align="right">536</td><td align="right">71</td></tr>
    <tr><td align="left">Half (~span/4) (r=99.75)</td><td align="right">1,065</td><td align="right">1,073</td><td align="right">1,012</td><td align="right">171</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=49.88)</td><td align="right">2,494</td><td align="right">2,710</td><td align="right">2,448</td><td align="right">577</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,607</td><td align="right">5,616</td><td align="right">5,660</td><td align="right">2,872</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size=399.0x249.0)</td><td align="right">2,462</td><td align="right">2,524</td><td align="right">2,549</td><td align="right">209</td></tr>
    <tr><td align="left">Half (size=199.5x124.5)</td><td align="right">3,563</td><td align="right">3,901</td><td align="right">3,304</td><td align="right">674</td></tr>
    <tr><td align="left">Quarter (size=99.75x62.25)</td><td align="right">4,726</td><td align="right">4,934</td><td align="right">4,519</td><td align="right">1,550</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,625</td><td align="right">5,658</td><td align="right">5,661</td><td align="right">2,850</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,562</td><td align="right">1,612</td><td align="right">1,236</td><td align="right">1,407</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,861</td><td align="right">1,870</td><td align="right">1,397</td><td align="right">1,439</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,942</td><td align="right">1,904</td><td align="right">1,456</td><td align="right">1,464</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,911</td><td align="right">1,912</td><td align="right">1,461</td><td align="right">1,462</td></tr>
  </tbody>
</table>

#### **10,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">10,000 entries</td><td align="right">541 (0.002s)</td><td align="right">818 (0.001s)</td><td align="right">541 (0.002s)</td><td align="right">322 (0.003s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">2,950</td><td align="right">2,951</td><td align="right">2,873</td><td align="right">579</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">4,631</td><td align="right">4,548</td><td align="right">4,092</td><td align="right">1,428</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">5,146</td><td align="right">5,164</td><td align="right">5,032</td><td align="right">2,339</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,581</td><td align="right">5,646</td><td align="right">5,680</td><td align="right">2,865</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size=99.00x99.00)</td><td align="right">5,087</td><td align="right">5,167</td><td align="right">5,181</td><td align="right">1,288</td></tr>
    <tr><td align="left">Half (size=49.50x49.50)</td><td align="right">5,622</td><td align="right">5,652</td><td align="right">5,010</td><td align="right">2,185</td></tr>
    <tr><td align="left">Quarter (size=24.75x24.75)</td><td align="right">5,435</td><td align="right">5,561</td><td align="right">5,414</td><td align="right">2,687</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,699</td><td align="right">5,709</td><td align="right">5,749</td><td align="right">2,817</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,681</td><td align="right">1,696</td><td align="right">1,304</td><td align="right">1,369</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,893</td><td align="right">1,888</td><td align="right">1,387</td><td align="right">1,445</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,961</td><td align="right">1,955</td><td align="right">1,413</td><td align="right">1,470</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,971</td><td align="right">1,917</td><td align="right">1,422</td><td align="right">1,471</td></tr>
  </tbody>
</table>

#### **1,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">1,000 entries</td><td align="right">5,376 (0.000s)</td><td align="right">7,429 (0.000s)</td><td align="right">4,940 (0.000s)</td><td align="right">889 (0.001s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=24.50)</td><td align="right">5,370</td><td align="right">5,367</td><td align="right">5,348</td><td align="right">2,032</td></tr>
    <tr><td align="left">Half (~span/4) (r=12.25)</td><td align="right">5,333</td><td align="right">5,421</td><td align="right">5,199</td><td align="right">2,425</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=6.13)</td><td align="right">5,541</td><td align="right">5,567</td><td align="right">5,433</td><td align="right">2,713</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,702</td><td align="right">5,619</td><td align="right">5,740</td><td align="right">2,874</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size=49.00x19.00)</td><td align="right">5,747</td><td align="right">5,690</td><td align="right">5,800</td><td align="right">2,590</td></tr>
    <tr><td align="left">Half (size=24.50x9.5)</td><td align="right">5,567</td><td align="right">5,775</td><td align="right">5,629</td><td align="right">2,806</td></tr>
    <tr><td align="left">Quarter (size=12.25x4.75)</td><td align="right">5,629</td><td align="right">5,739</td><td align="right">5,698</td><td align="right">2,842</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,729</td><td align="right">5,753</td><td align="right">5,751</td><td align="right">2,868</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,862</td><td align="right">1,871</td><td align="right">1,386</td><td align="right">1,412</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,900</td><td align="right">1,893</td><td align="right">1,418</td><td align="right">1,401</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,959</td><td align="right">1,965</td><td align="right">1,468</td><td align="right">1,428</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,970</td><td align="right">1,949</td><td align="right">1,434</td><td align="right">1,434</td></tr>
  </tbody>
</table>

#### **100 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100 entries</td><td align="right">43,859 (0.000s)</td><td align="right">40,650 (0.000s)</td><td align="right">26,954 (0.000s)</td><td align="right">1,682 (0.001s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">5,862</td><td align="right">5,829</td><td align="right">5,811</td><td align="right">2,816</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">5,776</td><td align="right">5,734</td><td align="right">5,688</td><td align="right">2,886</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">5,757</td><td align="right">5,723</td><td align="right">5,766</td><td align="right">2,904</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,727</td><td align="right">5,583</td><td align="right">5,765</td><td align="right">2,904</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size=9x9)</td><td align="right">5,846</td><td align="right">5,821</td><td align="right">5,910</td><td align="right">2,817</td></tr>
    <tr><td align="left">Half (size=4.5x4.5)</td><td align="right">5,742</td><td align="right">5,791</td><td align="right">5,709</td><td align="right">2,848</td></tr>
    <tr><td align="left">Quarter (size=2.25x2.25)</td><td align="right">5,753</td><td align="right">5,686</td><td align="right">5,762</td><td align="right">2,907</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,684</td><td align="right">5,702</td><td align="right">5,835</td><td align="right">2,898</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree2D (Balanced)</th>
      <th align="right">KDTree2D (Unbalanced)</th>
      <th align="right">QuadTree2D</th>
      <th align="right">RTree2D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100 neighbors (max)</td><td align="right">1,869</td><td align="right">1,918</td><td align="right">1,447</td><td align="right">1,447</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,950</td><td align="right">1,960</td><td align="right">1,476</td><td align="right">1,480</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,968</td><td align="right">1,955</td><td align="right">1,475</td><td align="right">1,478</td></tr>
  </tbody>
</table>
<!-- tabs:end -->
<!-- SPATIAL_TREE_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**QuadTree2D**:

- Best for: General-purpose 2D spatial queries
- Strengths: Balanced performance across all operation types, simple to use
- Weaknesses: Slightly slower than KdTree for point queries

**KdTree2D (Balanced)**:

- Best for: When you need consistent query performance
- Strengths: Fast nearest-neighbor queries, good for smaller datasets
- Weaknesses: Slower construction time

**KdTree2D (Unbalanced)**:

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
