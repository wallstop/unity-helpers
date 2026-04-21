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
    <tr><td align="left">1,000,000 entries</td><td align="right">2 (0.359s)</td><td align="right">6 (0.152s)</td><td align="right">4 (0.219s)</td><td align="right">2 (0.389s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=499.5)</td><td align="right">59</td><td align="right">59</td><td align="right">58</td><td align="right">7</td></tr>
    <tr><td align="left">Half (~span/4) (r=249.8)</td><td align="right">238</td><td align="right">238</td><td align="right">220</td><td align="right">29</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=124.9)</td><td align="right">947</td><td align="right">947</td><td align="right">816</td><td align="right">118</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">99,670</td><td align="right">101,892</td><td align="right">136,684</td><td align="right">96,713</td></tr>
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
    <tr><td align="left">Full (size=999.0x999.0)</td><td align="right">433</td><td align="right">405</td><td align="right">360</td><td align="right">18</td></tr>
    <tr><td align="left">Half (size=499.5x499.5)</td><td align="right">1,826</td><td align="right">1,829</td><td align="right">1,217</td><td align="right">88</td></tr>
    <tr><td align="left">Quarter (size=249.8x249.8)</td><td align="right">7,101</td><td align="right">7,061</td><td align="right">3,773</td><td align="right">380</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">141,553</td><td align="right">142,396</td><td align="right">184,035</td><td align="right">104,669</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">8,278</td><td align="right">16,262</td><td align="right">12,321</td><td align="right">59,125</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">67,944</td><td align="right">66,256</td><td align="right">66,443</td><td align="right">123,278</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">205,384</td><td align="right">175,412</td><td align="right">147,026</td><td align="right">169,773</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">265,309</td><td align="right">264,824</td><td align="right">173,504</td><td align="right">175,770</td></tr>
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
    <tr><td align="left">100,000 entries</td><td align="right">50 (0.020s)</td><td align="right">84 (0.012s)</td><td align="right">10 (0.092s)</td><td align="right">51 (0.019s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=199.5)</td><td align="right">600</td><td align="right">601</td><td align="right">598</td><td align="right">75</td></tr>
    <tr><td align="left">Half (~span/4) (r=99.75)</td><td align="right">1,352</td><td align="right">1,357</td><td align="right">1,246</td><td align="right">185</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=49.88)</td><td align="right">4,644</td><td align="right">5,161</td><td align="right">4,295</td><td align="right">724</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">122,350</td><td align="right">122,654</td><td align="right">168,403</td><td align="right">132,231</td></tr>
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
    <tr><td align="left">Full (size=399.0x249.0)</td><td align="right">4,561</td><td align="right">4,608</td><td align="right">4,403</td><td align="right">233</td></tr>
    <tr><td align="left">Half (size=199.5x124.5)</td><td align="right">9,475</td><td align="right">11,753</td><td align="right">7,853</td><td align="right">964</td></tr>
    <tr><td align="left">Quarter (size=99.75x62.25)</td><td align="right">24,906</td><td align="right">31,545</td><td align="right">20,178</td><td align="right">3,755</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">172,319</td><td align="right">173,024</td><td align="right">225,047</td><td align="right">140,423</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">9,721</td><td align="right">9,662</td><td align="right">11,206</td><td align="right">59,108</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">45,047</td><td align="right">77,749</td><td align="right">44,819</td><td align="right">145,560</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">225,024</td><td align="right">173,728</td><td align="right">162,370</td><td align="right">191,394</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">226,497</td><td align="right">210,737</td><td align="right">189,588</td><td align="right">198,053</td></tr>
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
    <tr><td align="left">10,000 entries</td><td align="right">543 (0.002s)</td><td align="right">813 (0.001s)</td><td align="right">544 (0.002s)</td><td align="right">510 (0.002s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">5,895</td><td align="right">5,928</td><td align="right">5,879</td><td align="right">734</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">22,259</td><td align="right">22,293</td><td align="right">13,797</td><td align="right">2,892</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">43,468</td><td align="right">50,041</td><td align="right">37,449</td><td align="right">12,064</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">156,856</td><td align="right">154,166</td><td align="right">216,619</td><td align="right">151,008</td></tr>
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
    <tr><td align="left">Full (size=99.00x99.00)</td><td align="right">44,870</td><td align="right">44,431</td><td align="right">46,015</td><td align="right">2,341</td></tr>
    <tr><td align="left">Half (size=49.50x49.50)</td><td align="right">158,404</td><td align="right">158,408</td><td align="right">36,718</td><td align="right">8,951</td></tr>
    <tr><td align="left">Quarter (size=24.75x24.75)</td><td align="right">73,174</td><td align="right">100,027</td><td align="right">73,199</td><td align="right">34,155</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">221,985</td><td align="right">215,869</td><td align="right">286,168</td><td align="right">161,133</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">12,710</td><td align="right">12,540</td><td align="right">13,860</td><td align="right">55,727</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">55,295</td><td align="right">50,910</td><td align="right">76,613</td><td align="right">148,713</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">224,136</td><td align="right">205,429</td><td align="right">174,339</td><td align="right">210,086</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">281,463</td><td align="right">249,197</td><td align="right">204,354</td><td align="right">220,972</td></tr>
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
    <tr><td align="left">1,000 entries</td><td align="right">4,852 (0.000s)</td><td align="right">7,961 (0.000s)</td><td align="right">4,686 (0.000s)</td><td align="right">4,557 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=24.50)</td><td align="right">55,893</td><td align="right">57,102</td><td align="right">56,097</td><td align="right">7,342</td></tr>
    <tr><td align="left">Half (~span/4) (r=12.25)</td><td align="right">59,063</td><td align="right">74,694</td><td align="right">55,921</td><td align="right">14,534</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=6.13)</td><td align="right">92,939</td><td align="right">104,886</td><td align="right">91,839</td><td align="right">36,930</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">222,827</td><td align="right">220,241</td><td align="right">299,788</td><td align="right">212,909</td></tr>
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
    <tr><td align="left">Full (size=49.00x19.00)</td><td align="right">430,814</td><td align="right">430,852</td><td align="right">452,791</td><td align="right">23,445</td></tr>
    <tr><td align="left">Half (size=24.50x9.5)</td><td align="right">156,729</td><td align="right">263,248</td><td align="right">120,573</td><td align="right">70,856</td></tr>
    <tr><td align="left">Quarter (size=12.25x4.75)</td><td align="right">246,056</td><td align="right">260,412</td><td align="right">181,261</td><td align="right">158,165</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">305,340</td><td align="right">300,846</td><td align="right">395,180</td><td align="right">237,579</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">42,240</td><td align="right">42,911</td><td align="right">35,795</td><td align="right">59,620</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">69,561</td><td align="right">67,320</td><td align="right">75,420</td><td align="right">163,855</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">231,731</td><td align="right">244,310</td><td align="right">193,392</td><td align="right">234,017</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">307,506</td><td align="right">219,267</td><td align="right">193,132</td><td align="right">247,700</td></tr>
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
    <tr><td align="left">100 entries</td><td align="right">39,215 (0.000s)</td><td align="right">35,460 (0.000s)</td><td align="right">21,008 (0.000s)</td><td align="right">20,576 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">438,163</td><td align="right">438,426</td><td align="right">436,257</td><td align="right">69,261</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">379,510</td><td align="right">382,512</td><td align="right">236,397</td><td align="right">202,932</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">375,449</td><td align="right">382,951</td><td align="right">493,448</td><td align="right">276,634</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">379,149</td><td align="right">383,140</td><td align="right">492,984</td><td align="right">274,349</td></tr>
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
    <tr><td align="left">Full (size=9x9)</td><td align="right">1,353,163</td><td align="right">1,359,911</td><td align="right">1,307,730</td><td align="right">195,074</td></tr>
    <tr><td align="left">Half (size=4.5x4.5)</td><td align="right">469,727</td><td align="right">472,652</td><td align="right">323,903</td><td align="right">299,135</td></tr>
    <tr><td align="left">Quarter (size=2.25x2.25)</td><td align="right">492,315</td><td align="right">502,632</td><td align="right">619,837</td><td align="right">313,482</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">492,918</td><td align="right">496,724</td><td align="right">617,756</td><td align="right">311,829</td></tr>
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
    <tr><td align="left">100 neighbors (max)</td><td align="right">123,336</td><td align="right">122,168</td><td align="right">133,695</td><td align="right">179,415</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">250,408</td><td align="right">233,758</td><td align="right">253,757</td><td align="right">270,169</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">252,054</td><td align="right">303,982</td><td align="right">244,361</td><td align="right">285,711</td></tr>
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
