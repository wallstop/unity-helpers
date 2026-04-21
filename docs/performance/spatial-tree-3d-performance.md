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
    <tr><td align="left">1,000,000 entries</td><td align="right">3 (0.262s)</td><td align="right">6 (0.153s)</td><td align="right">2 (0.352s)</td><td align="right">3 (0.294s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">20</td><td align="right">24</td><td align="right">33</td><td align="right">17</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">152</td><td align="right">187</td><td align="right">263</td><td align="right">169</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">1,032</td><td align="right">1,358</td><td align="right">1,668</td><td align="right">1,502</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">23,725</td><td align="right">23,819</td><td align="right">133,282</td><td align="right">71,954</td></tr>
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
    <tr><td align="left">Full (size≈99.00x99.00x99.00)</td><td align="right">35</td><td align="right">40</td><td align="right">233</td><td align="right">24</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x49.50)</td><td align="right">48</td><td align="right">56</td><td align="right">1,241</td><td align="right">278</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x24.75)</td><td align="right">49</td><td align="right">56</td><td align="right">3,651</td><td align="right">2,511</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">51</td><td align="right">52</td><td align="right">163,170</td><td align="right">73,047</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">6,242</td><td align="right">10,607</td><td align="right">2,321</td><td align="right">303</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">63,305</td><td align="right">72,469</td><td align="right">10,872</td><td align="right">3,283</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">295,644</td><td align="right">307,373</td><td align="right">15,979</td><td align="right">7,236</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">336,117</td><td align="right">352,619</td><td align="right">19,661</td><td align="right">8,156</td></tr>
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
    <tr><td align="left">100,000 entries</td><td align="right">13 (0.072s)</td><td align="right">94 (0.011s)</td><td align="right">64 (0.016s)</td><td align="right">44 (0.023s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">385</td><td align="right">591</td><td align="right">774</td><td align="right">211</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">1,141</td><td align="right">1,732</td><td align="right">1,991</td><td align="right">838</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">2,862</td><td align="right">4,731</td><td align="right">6,020</td><td align="right">3,407</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">27,681</td><td align="right">31,623</td><td align="right">167,236</td><td align="right">94,401</td></tr>
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
    <tr><td align="left">Full (size≈99.00x99.00x9)</td><td align="right">624</td><td align="right">731</td><td align="right">2,794</td><td align="right">356</td></tr>
    <tr><td align="left">Half (size≈49.50x49.50x4.5)</td><td align="right">717</td><td align="right">854</td><td align="right">8,996</td><td align="right">3,486</td></tr>
    <tr><td align="left">Quarter (size≈24.75x24.75x2.25)</td><td align="right">727</td><td align="right">875</td><td align="right">43,443</td><td align="right">23,687</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">733</td><td align="right">884</td><td align="right">211,599</td><td align="right">94,890</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">6,943</td><td align="right">12,516</td><td align="right">1,654</td><td align="right">269</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">39,660</td><td align="right">45,252</td><td align="right">9,316</td><td align="right">2,180</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">323,237</td><td align="right">260,287</td><td align="right">19,047</td><td align="right">7,206</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">332,074</td><td align="right">263,010</td><td align="right">29,490</td><td align="right">11,252</td></tr>
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
    <tr><td align="left">10,000 entries</td><td align="right">123 (0.008s)</td><td align="right">773 (0.001s)</td><td align="right">597 (0.002s)</td><td align="right">433 (0.002s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">4,887</td><td align="right">5,106</td><td align="right">9,173</td><td align="right">2,188</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">6,293</td><td align="right">7,075</td><td align="right">9,001</td><td align="right">4,230</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">6,291</td><td align="right">7,455</td><td align="right">11,198</td><td align="right">7,437</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">41,933</td><td align="right">39,762</td><td align="right">207,044</td><td align="right">143,782</td></tr>
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
    <tr><td align="left">Full (size≈99.00x9x9)</td><td align="right">6,244</td><td align="right">6,165</td><td align="right">28,264</td><td align="right">3,509</td></tr>
    <tr><td align="left">Half (size≈49.50x4.5x4.5)</td><td align="right">7,000</td><td align="right">6,986</td><td align="right">42,239</td><td align="right">35,660</td></tr>
    <tr><td align="left">Quarter (size≈24.75x2.25x2.25)</td><td align="right">7,125</td><td align="right">7,161</td><td align="right">149,019</td><td align="right">111,164</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">7,204</td><td align="right">7,177</td><td align="right">274,570</td><td align="right">146,522</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">10,178</td><td align="right">11,176</td><td align="right">636</td><td align="right">185</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">48,653</td><td align="right">69,038</td><td align="right">5,954</td><td align="right">2,208</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">228,792</td><td align="right">322,319</td><td align="right">26,632</td><td align="right">12,390</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">346,732</td><td align="right">403,633</td><td align="right">43,798</td><td align="right">20,882</td></tr>
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
    <tr><td align="left">1,000 entries</td><td align="right">5,027 (0.000s)</td><td align="right">7,342 (0.000s)</td><td align="right">4,194 (0.000s)</td><td align="right">3,855 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">13,216</td><td align="right">15,836</td><td align="right">24,528</td><td align="right">21,168</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">54,776</td><td align="right">65,071</td><td align="right">119,223</td><td align="right">134,932</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">63,672</td><td align="right">66,530</td><td align="right">305,884</td><td align="right">197,776</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">63,668</td><td align="right">66,291</td><td align="right">306,143</td><td align="right">196,875</td></tr>
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
    <tr><td align="left">Full (size≈9x9x9)</td><td align="right">56,539</td><td align="right">60,422</td><td align="right">289,848</td><td align="right">34,779</td></tr>
    <tr><td align="left">Half (size≈4.5x4.5x4.5)</td><td align="right">59,917</td><td align="right">65,887</td><td align="right">172,693</td><td align="right">158,343</td></tr>
    <tr><td align="left">Quarter (size≈2.25x2.25x2.25)</td><td align="right">60,874</td><td align="right">67,913</td><td align="right">408,245</td><td align="right">202,987</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">60,594</td><td align="right">68,914</td><td align="right">408,355</td><td align="right">202,835</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">16,594</td><td align="right">15,570</td><td align="right">3,265</td><td align="right">616</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">69,991</td><td align="right">66,103</td><td align="right">15,462</td><td align="right">4,020</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">324,837</td><td align="right">305,601</td><td align="right">70,301</td><td align="right">29,522</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">399,134</td><td align="right">428,885</td><td align="right">78,576</td><td align="right">38,531</td></tr>
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
    <tr><td align="left">100 entries</td><td align="right">36,363 (0.000s)</td><td align="right">39,062 (0.000s)</td><td align="right">28,409 (0.000s)</td><td align="right">11,389 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">134,264</td><td align="right">133,125</td><td align="right">268,794</td><td align="right">173,242</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">155,435</td><td align="right">157,119</td><td align="right">287,851</td><td align="right">244,592</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">156,564</td><td align="right">158,685</td><td align="right">347,661</td><td align="right">328,932</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">155,612</td><td align="right">158,148</td><td align="right">350,184</td><td align="right">329,295</td></tr>
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
    <tr><td align="left">Full (size≈9x4x1)</td><td align="right">445,137</td><td align="right">442,162</td><td align="right">1,133,386</td><td align="right">269,238</td></tr>
    <tr><td align="left">Half (size≈4.5x2x1)</td><td align="right">456,470</td><td align="right">460,381</td><td align="right">413,163</td><td align="right">351,246</td></tr>
    <tr><td align="left">Quarter (size≈2.25x1x1)</td><td align="right">470,424</td><td align="right">465,685</td><td align="right">581,178</td><td align="right">493,257</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">469,798</td><td align="right">465,875</td><td align="right">578,881</td><td align="right">473,813</td></tr>
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
    <tr><td align="left">100 neighbors (max)</td><td align="right">87,276</td><td align="right">85,347</td><td align="right">63,437</td><td align="right">65,384</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">361,756</td><td align="right">369,204</td><td align="right">94,888</td><td align="right">82,476</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">504,598</td><td align="right">421,747</td><td align="right">150,382</td><td align="right">156,721</td></tr>
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
