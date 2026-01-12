---
---

# 3D Spatial Tree Performance Benchmarks

## TL;DR — What Problem This Solves

- Need fast “what’s near X?” or “what’s inside this volume?” in 3D.
- These structures avoid scanning every object; queries touch only nearby data.
- Quick picks: OctTree3D for general 3D queries; KdTree3D for nearest‑neighbor on points; RTree3D for volumetric bounds.

Note: KdTree3D, OctTree3D, and RTree3D are under active development and their APIs/performance may evolve. SpatialHash3D is stable and recommended for broad‑phase neighbor queries with many moving objects.

For boundary and result semantics across structures, see [Spatial Tree Semantics](../features/spatial/spatial-tree-semantics.md)

This document contains performance benchmarks for the 3D spatial tree implementations in Unity Helpers.

## Available 3D Spatial Trees

- **OctTree3D** - Easiest to use, good all-around performance for 3D
- **KdTree3D** - Balanced and unbalanced variants available
- **RTree3D** - Optimized for 3D bounding box queries
- **SpatialHash3D** - Efficient for uniformly distributed moving objects (stable)

## Performance Benchmarks

<!-- SPATIAL_TREE_3D_BENCHMARKS_START -->

### Datasets

<!-- tabs:start -->

#### **1,000,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">1,000,000 entries</td><td align="right">3 (0.260s)</td><td align="right">5 (0.168s)</td><td align="right">1 (0.515s)</td><td align="right">3 (0.314s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">20</td><td align="right">22</td><td align="right">32</td><td align="right">14</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">149</td><td align="right">152</td><td align="right">250</td><td align="right">140</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">975</td><td align="right">1,096</td><td align="right">1,615</td><td align="right">1,095</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">7,064</td><td align="right">4,733</td><td align="right">7,888</td><td align="right">4,196</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size≈99.00x99.00x99.00)</td><td align="right">33</td><td align="right">37</td><td align="right">199</td><td align="right">20</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x49.50)</td><td align="right">44</td><td align="right">49</td><td align="right">1,078</td><td align="right">242</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x24.75)</td><td align="right">47</td><td align="right">53</td><td align="right">2,103</td><td align="right">1,303</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">48</td><td align="right">53</td><td align="right">5,523</td><td align="right">2,789</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">2,717</td><td align="right">1,636</td><td align="right">1,585</td><td align="right">289</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">2,980</td><td align="right">1,849</td><td align="right">2,968</td><td align="right">1,958</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,944</td><td align="right">1,896</td><td align="right">1,895</td><td align="right">2,512</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,952</td><td align="right">1,946</td><td align="right">1,770</td><td align="right">1,659</td></tr>
  </tbody>
</table>

#### **100,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100,000 entries</td><td align="right">49 (0.020s)</td><td align="right">97 (0.010s)</td><td align="right">61 (0.016s)</td><td align="right">42 (0.024s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">358</td><td align="right">498</td><td align="right">688</td><td align="right">173</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">892</td><td align="right">1,320</td><td align="right">1,509</td><td align="right">573</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">1,804</td><td align="right">2,589</td><td align="right">2,958</td><td align="right">1,466</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">4,843</td><td align="right">4,953</td><td align="right">5,673</td><td align="right">2,832</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size≈99.00x99.00x9)</td><td align="right">545</td><td align="right">637</td><td align="right">1,908</td><td align="right">303</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x4.5)</td><td align="right">625</td><td align="right">734</td><td align="right">3,493</td><td align="right">1,480</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x2.25)</td><td align="right">635</td><td align="right">750</td><td align="right">5,106</td><td align="right">2,543</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">642</td><td align="right">752</td><td align="right">5,590</td><td align="right">2,829</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,496</td><td align="right">1,666</td><td align="right">871</td><td align="right">239</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,867</td><td align="right">1,819</td><td align="right">1,607</td><td align="right">1,044</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,934</td><td align="right">1,874</td><td align="right">1,770</td><td align="right">1,530</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,893</td><td align="right">1,899</td><td align="right">1,830</td><td align="right">1,660</td></tr>
  </tbody>
</table>

#### **10,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">10,000 entries</td><td align="right">602 (0.002s)</td><td align="right">742 (0.001s)</td><td align="right">577 (0.002s)</td><td align="right">447 (0.002s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">2,752</td><td align="right">2,682</td><td align="right">3,342</td><td align="right">1,113</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">3,010</td><td align="right">3,073</td><td align="right">3,458</td><td align="right">1,576</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">3,005</td><td align="right">3,251</td><td align="right">3,744</td><td align="right">1,993</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,039</td><td align="right">5,134</td><td align="right">5,528</td><td align="right">2,789</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size≈99.00x9x9)</td><td align="right">2,912</td><td align="right">2,988</td><td align="right">4,799</td><td align="right">1,516</td></tr>
    <tr><td align="left">Half (size≈49.50x4.5x4.5)</td><td align="right">3,175</td><td align="right">3,166</td><td align="right">5,028</td><td align="right">2,635</td></tr>
    <tr><td align="left">Quarter (size≈24.75x2.25x2.25)</td><td align="right">3,170</td><td align="right">3,186</td><td align="right">5,357</td><td align="right">2,722</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">3,214</td><td align="right">3,197</td><td align="right">5,702</td><td align="right">2,771</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,614</td><td align="right">1,629</td><td align="right">456</td><td align="right">161</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,871</td><td align="right">1,862</td><td align="right">1,400</td><td align="right">1,015</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,917</td><td align="right">1,900</td><td align="right">1,721</td><td align="right">1,638</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,942</td><td align="right">1,890</td><td align="right">1,820</td><td align="right">1,751</td></tr>
  </tbody>
</table>

#### **1,000 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">1,000 entries</td><td align="right">5,192 (0.000s)</td><td align="right">6,939 (0.000s)</td><td align="right">2,725 (0.000s)</td><td align="right">3,868 (0.000s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">4,008</td><td align="right">4,204</td><td align="right">4,687</td><td align="right">2,445</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">5,146</td><td align="right">5,341</td><td align="right">5,497</td><td align="right">2,805</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">5,310</td><td align="right">5,333</td><td align="right">5,647</td><td align="right">2,851</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,216</td><td align="right">5,245</td><td align="right">5,694</td><td align="right">2,866</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size≈9x9x9)</td><td align="right">5,234</td><td align="right">5,222</td><td align="right">5,675</td><td align="right">2,649</td></tr>
    <tr><td align="left">Half (size≈4.5x4.5x4.5)</td><td align="right">5,135</td><td align="right">5,344</td><td align="right">5,584</td><td align="right">2,832</td></tr>
    <tr><td align="left">Quarter (size≈2.25x2.25x2.25)</td><td align="right">5,162</td><td align="right">5,343</td><td align="right">5,745</td><td align="right">2,877</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,240</td><td align="right">5,381</td><td align="right">5,766</td><td align="right">2,879</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">500 neighbors</td><td align="right">1,708</td><td align="right">1,697</td><td align="right">1,174</td><td align="right">462</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">1,859</td><td align="right">1,868</td><td align="right">1,705</td><td align="right">1,263</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,919</td><td align="right">1,924</td><td align="right">1,862</td><td align="right">1,765</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,936</td><td align="right">1,939</td><td align="right">1,850</td><td align="right">1,829</td></tr>
  </tbody>
</table>

#### **100 entries**

##### Construction

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Construction</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100 entries</td><td align="right">42,016 (0.000s)</td><td align="right">37,593 (0.000s)</td><td align="right">14,662 (0.000s)</td><td align="right">13,927 (0.000s)</td></tr>
  </tbody>
</table>

##### Elements In Range

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Elements In Range</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">5,587</td><td align="right">5,601</td><td align="right">5,702</td><td align="right">2,837</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">5,623</td><td align="right">5,625</td><td align="right">5,707</td><td align="right">2,873</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">5,615</td><td align="right">5,618</td><td align="right">5,704</td><td align="right">2,889</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">5,592</td><td align="right">5,623</td><td align="right">5,712</td><td align="right">2,886</td></tr>
  </tbody>
</table>

##### Get Elements In Bounds

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Get Elements In Bounds</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">Full (size≈9x4x1)</td><td align="right">5,765</td><td align="right">5,781</td><td align="right">5,812</td><td align="right">2,853</td></tr>
    <tr><td align="left">Half (size≈4.5x2x1)</td><td align="right">5,780</td><td align="right">5,777</td><td align="right">5,688</td><td align="right">2,868</td></tr>
    <tr><td align="left">Quarter (size≈2.25x1x1)</td><td align="right">5,763</td><td align="right">5,751</td><td align="right">5,748</td><td align="right">2,911</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">5,752</td><td align="right">5,763</td><td align="right">5,798</td><td align="right">2,916</td></tr>
  </tbody>
</table>

##### Approximate Nearest Neighbors

<table data-sortable>
  <thead>
    <tr>
      <th align="left">Approximate Nearest Neighbors</th>
      <th align="right">KDTree3D (Balanced)</th>
      <th align="right">KDTree3D (Unbalanced)</th>
      <th align="right">OctTree3D</th>
      <th align="right">RTree3D</th>
    </tr>
  </thead>
  <tbody>
    <tr><td align="left">100 neighbors (max)</td><td align="right">1,866</td><td align="right">1,883</td><td align="right">1,865</td><td align="right">1,869</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">1,945</td><td align="right">1,921</td><td align="right">1,900</td><td align="right">1,880</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">1,939</td><td align="right">1,939</td><td align="right">1,899</td><td align="right">1,898</td></tr>
  </tbody>
</table>
<!-- tabs:end -->
<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**OctTree3D**:

- Best for: General-purpose 3D spatial queries
- Strengths: Balanced performance, easy to use, good spatial locality
- Use cases: 3D collision detection, visibility culling, spatial audio

**KdTree3D (Balanced)**:

- Best for: Nearest-neighbor queries in 3D space
- Strengths: Fast point queries, good for smaller datasets
- Use cases: Pathfinding, AI spatial awareness, particle systems

**KdTree3D (Unbalanced)**:

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
