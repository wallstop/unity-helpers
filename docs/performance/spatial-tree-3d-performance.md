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
    <tr><td align="left">1,000,000 entries</td><td align="right">3 (0.256s)</td><td align="right">6 (0.153s)</td><td align="right">4 (0.238s)</td><td align="right">2 (0.382s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">20</td><td align="right">24</td><td align="right">34</td><td align="right">16</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">151</td><td align="right">188</td><td align="right">260</td><td align="right">156</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">1,028</td><td align="right">1,374</td><td align="right">1,682</td><td align="right">1,467</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">23,737</td><td align="right">23,914</td><td align="right">133,257</td><td align="right">72,647</td></tr>
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
    <tr><td align="left">Full (size≈99.00x99.00x99.00)</td><td align="right">35</td><td align="right">42</td><td align="right">228</td><td align="right">24</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x49.50)</td><td align="right">49</td><td align="right">57</td><td align="right">1,209</td><td align="right">284</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x24.75)</td><td align="right">51</td><td align="right">58</td><td align="right">3,618</td><td align="right">2,523</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">52</td><td align="right">60</td><td align="right">162,557</td><td align="right">73,981</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">6,154</td><td align="right">10,636</td><td align="right">2,320</td><td align="right">304</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">65,120</td><td align="right">72,381</td><td align="right">10,891</td><td align="right">3,340</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">261,874</td><td align="right">302,986</td><td align="right">15,976</td><td align="right">7,609</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">381,614</td><td align="right">313,660</td><td align="right">19,656</td><td align="right">8,296</td></tr>
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
    <tr><td align="left">100,000 entries</td><td align="right">50 (0.020s)</td><td align="right">98 (0.010s)</td><td align="right">64 (0.015s)</td><td align="right">8 (0.122s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">386</td><td align="right">597</td><td align="right">796</td><td align="right">217</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">1,144</td><td align="right">1,749</td><td align="right">2,086</td><td align="right">850</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">2,842</td><td align="right">4,709</td><td align="right">6,047</td><td align="right">3,435</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">27,496</td><td align="right">31,671</td><td align="right">168,945</td><td align="right">94,784</td></tr>
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
    <tr><td align="left">Full (size≈99.00x99.00x9)</td><td align="right">623</td><td align="right">736</td><td align="right">2,630</td><td align="right">359</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x4.5)</td><td align="right">717</td><td align="right">855</td><td align="right">9,027</td><td align="right">3,487</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x2.25)</td><td align="right">728</td><td align="right">876</td><td align="right">43,670</td><td align="right">23,965</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">740</td><td align="right">881</td><td align="right">212,758</td><td align="right">96,799</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">6,992</td><td align="right">12,494</td><td align="right">1,655</td><td align="right">272</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">39,589</td><td align="right">45,270</td><td align="right">9,207</td><td align="right">2,236</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">317,480</td><td align="right">256,693</td><td align="right">18,699</td><td align="right">7,360</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">327,660</td><td align="right">262,031</td><td align="right">29,513</td><td align="right">11,781</td></tr>
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
    <tr><td align="left">10,000 entries</td><td align="right">289 (0.003s)</td><td align="right">787 (0.001s)</td><td align="right">610 (0.002s)</td><td align="right">452 (0.002s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">5,217</td><td align="right">5,130</td><td align="right">9,221</td><td align="right">2,175</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">6,217</td><td align="right">7,080</td><td align="right">8,945</td><td align="right">4,209</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">6,364</td><td align="right">7,488</td><td align="right">11,223</td><td align="right">7,465</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">42,363</td><td align="right">39,974</td><td align="right">207,069</td><td align="right">143,938</td></tr>
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
    <tr><td align="left">Full (size≈99.00x9x9)</td><td align="right">6,245</td><td align="right">6,229</td><td align="right">27,073</td><td align="right">3,589</td></tr>
    <tr><td align="left">Half (size≈49.50x4.5x4.5)</td><td align="right">6,994</td><td align="right">7,036</td><td align="right">42,332</td><td align="right">36,924</td></tr>
    <tr><td align="left">Quarter (size≈24.75x2.25x2.25)</td><td align="right">7,134</td><td align="right">7,165</td><td align="right">148,393</td><td align="right">113,215</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">7,211</td><td align="right">7,204</td><td align="right">275,229</td><td align="right">148,701</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">10,497</td><td align="right">11,245</td><td align="right">638</td><td align="right">185</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">48,703</td><td align="right">68,760</td><td align="right">5,974</td><td align="right">2,209</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">246,093</td><td align="right">328,128</td><td align="right">26,618</td><td align="right">12,395</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">420,534</td><td align="right">408,499</td><td align="right">43,894</td><td align="right">20,887</td></tr>
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
    <tr><td align="left">1,000 entries</td><td align="right">5,107 (0.000s)</td><td align="right">7,117 (0.000s)</td><td align="right">3,891 (0.000s)</td><td align="right">3,972 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">13,378</td><td align="right">15,815</td><td align="right">24,439</td><td align="right">20,143</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">54,916</td><td align="right">63,837</td><td align="right">119,238</td><td align="right">128,411</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">64,266</td><td align="right">66,774</td><td align="right">306,084</td><td align="right">195,836</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">64,239</td><td align="right">66,833</td><td align="right">306,092</td><td align="right">196,339</td></tr>
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
    <tr><td align="left">Full (size≈9x9x9)</td><td align="right">56,558</td><td align="right">61,009</td><td align="right">288,591</td><td align="right">35,120</td></tr>
    <tr><td align="left">Half (size≈4.5x4.5x4.5)</td><td align="right">60,663</td><td align="right">66,569</td><td align="right">173,235</td><td align="right">161,208</td></tr>
    <tr><td align="left">Quarter (size≈2.25x2.25x2.25)</td><td align="right">61,430</td><td align="right">67,553</td><td align="right">410,060</td><td align="right">205,823</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">61,371</td><td align="right">69,008</td><td align="right">408,577</td><td align="right">206,353</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">16,331</td><td align="right">15,307</td><td align="right">3,271</td><td align="right">618</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">67,919</td><td align="right">65,395</td><td align="right">15,590</td><td align="right">4,017</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">299,103</td><td align="right">302,785</td><td align="right">70,394</td><td align="right">29,508</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">426,607</td><td align="right">422,916</td><td align="right">79,102</td><td align="right">38,349</td></tr>
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
    <tr><td align="left">100 entries</td><td align="right">38,610 (0.000s)</td><td align="right">36,101 (0.000s)</td><td align="right">26,737 (0.000s)</td><td align="right">19,646 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">133,468</td><td align="right">134,274</td><td align="right">270,334</td><td align="right">166,407</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">157,259</td><td align="right">157,078</td><td align="right">288,266</td><td align="right">259,133</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">156,534</td><td align="right">158,490</td><td align="right">346,499</td><td align="right">329,501</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">155,973</td><td align="right">158,478</td><td align="right">347,165</td><td align="right">330,562</td></tr>
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
    <tr><td align="left">Full (size≈9x4x1)</td><td align="right">447,338</td><td align="right">447,555</td><td align="right">1,136,124</td><td align="right">273,185</td></tr>
    <tr><td align="left">Half (size≈4.5x2x1)</td><td align="right">463,043</td><td align="right">467,574</td><td align="right">412,620</td><td align="right">355,479</td></tr>
    <tr><td align="left">Quarter (size≈2.25x1x1)</td><td align="right">472,263</td><td align="right">467,929</td><td align="right">582,307</td><td align="right">497,201</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">472,033</td><td align="right">468,178</td><td align="right">583,276</td><td align="right">497,977</td></tr>
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
    <tr><td align="left">100 neighbors (max)</td><td align="right">89,495</td><td align="right">86,128</td><td align="right">65,050</td><td align="right">65,207</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">374,582</td><td align="right">359,364</td><td align="right">96,849</td><td align="right">81,157</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">508,620</td><td align="right">412,021</td><td align="right">150,941</td><td align="right">156,727</td></tr>
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
