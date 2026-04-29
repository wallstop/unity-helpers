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
    <tr><td align="left">1,000,000 entries</td><td align="right">3 (0.312s)</td><td align="right">6 (0.151s)</td><td align="right">4 (0.221s)</td><td align="right">3 (0.314s)</td></tr>
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
    <tr><td align="left">Half (~span/4) (r=249.8)</td><td align="right">238</td><td align="right">238</td><td align="right">217</td><td align="right">28</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=124.9)</td><td align="right">946</td><td align="right">946</td><td align="right">813</td><td align="right">119</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">99,724</td><td align="right">101,966</td><td align="right">137,096</td><td align="right">99,994</td></tr>
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
    <tr><td align="left">Full (size=999.0x999.0)</td><td align="right">407</td><td align="right">411</td><td align="right">366</td><td align="right">19</td></tr>
    <tr><td align="left">Half (size=499.5x499.5)</td><td align="right">1,795</td><td align="right">1,802</td><td align="right">1,221</td><td align="right">88</td></tr>
    <tr><td align="left">Quarter (size=249.8x249.8)</td><td align="right">7,115</td><td align="right">6,980</td><td align="right">3,798</td><td align="right">380</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">141,664</td><td align="right">144,487</td><td align="right">183,791</td><td align="right">104,753</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">8,206</td><td align="right">16,253</td><td align="right">12,254</td><td align="right">59,120</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">67,908</td><td align="right">66,022</td><td align="right">66,565</td><td align="right">123,474</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">202,943</td><td align="right">190,315</td><td align="right">149,013</td><td align="right">170,797</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">267,876</td><td align="right">241,162</td><td align="right">175,228</td><td align="right">177,513</td></tr>
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
    <tr><td align="left">100,000 entries</td><td align="right">50 (0.020s)</td><td align="right">83 (0.012s)</td><td align="right">50 (0.020s)</td><td align="right">9 (0.109s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=199.5)</td><td align="right">600</td><td align="right">601</td><td align="right">599</td><td align="right">74</td></tr>
    <tr><td align="left">Half (~span/4) (r=99.75)</td><td align="right">1,351</td><td align="right">1,356</td><td align="right">1,246</td><td align="right">185</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=49.88)</td><td align="right">4,642</td><td align="right">5,154</td><td align="right">4,293</td><td align="right">722</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">122,543</td><td align="right">122,801</td><td align="right">168,775</td><td align="right">132,560</td></tr>
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
    <tr><td align="left">Full (size=399.0x249.0)</td><td align="right">4,453</td><td align="right">4,549</td><td align="right">4,563</td><td align="right">238</td></tr>
    <tr><td align="left">Half (size=199.5x124.5)</td><td align="right">9,453</td><td align="right">11,770</td><td align="right">7,930</td><td align="right">964</td></tr>
    <tr><td align="left">Quarter (size=99.75x62.25)</td><td align="right">26,044</td><td align="right">31,978</td><td align="right">19,472</td><td align="right">3,795</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">172,836</td><td align="right">173,333</td><td align="right">224,605</td><td align="right">140,946</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">9,740</td><td align="right">9,685</td><td align="right">11,231</td><td align="right">59,001</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">44,921</td><td align="right">77,959</td><td align="right">48,783</td><td align="right">147,429</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">225,534</td><td align="right">200,018</td><td align="right">163,465</td><td align="right">192,974</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">225,465</td><td align="right">276,002</td><td align="right">161,053</td><td align="right">201,342</td></tr>
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
    <tr><td align="left">10,000 entries</td><td align="right">233 (0.004s)</td><td align="right">835 (0.001s)</td><td align="right">541 (0.002s)</td><td align="right">508 (0.002s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=49.50)</td><td align="right">5,875</td><td align="right">5,924</td><td align="right">5,920</td><td align="right">734</td></tr>
    <tr><td align="left">Half (~span/4) (r=24.75)</td><td align="right">22,210</td><td align="right">22,289</td><td align="right">13,798</td><td align="right">2,918</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=12.38)</td><td align="right">43,481</td><td align="right">50,510</td><td align="right">37,612</td><td align="right">12,063</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">158,679</td><td align="right">152,879</td><td align="right">217,222</td><td align="right">151,558</td></tr>
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
    <tr><td align="left">Full (size=99.00x99.00)</td><td align="right">43,343</td><td align="right">44,704</td><td align="right">45,372</td><td align="right">2,406</td></tr>
    <tr><td align="left">Half (size=49.50x49.50)</td><td align="right">158,570</td><td align="right">140,495</td><td align="right">36,370</td><td align="right">9,239</td></tr>
    <tr><td align="left">Quarter (size=24.75x24.75)</td><td align="right">73,307</td><td align="right">98,718</td><td align="right">73,266</td><td align="right">34,653</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">222,078</td><td align="right">213,762</td><td align="right">286,948</td><td align="right">162,138</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">12,580</td><td align="right">12,647</td><td align="right">13,889</td><td align="right">55,797</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">46,902</td><td align="right">50,902</td><td align="right">78,242</td><td align="right">148,788</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">184,894</td><td align="right">207,390</td><td align="right">178,180</td><td align="right">211,248</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">282,350</td><td align="right">285,939</td><td align="right">207,301</td><td align="right">224,225</td></tr>
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
    <tr><td align="left">1,000 entries</td><td align="right">5,291 (0.000s)</td><td align="right">7,668 (0.000s)</td><td align="right">4,816 (0.000s)</td><td align="right">4,595 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=24.50)</td><td align="right">57,007</td><td align="right">56,693</td><td align="right">56,124</td><td align="right">7,330</td></tr>
    <tr><td align="left">Half (~span/4) (r=12.25)</td><td align="right">59,134</td><td align="right">74,131</td><td align="right">56,058</td><td align="right">14,528</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=6.13)</td><td align="right">92,781</td><td align="right">104,908</td><td align="right">92,734</td><td align="right">36,895</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">222,860</td><td align="right">220,260</td><td align="right">303,674</td><td align="right">216,071</td></tr>
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
    <tr><td align="left">Full (size=49.00x19.00)</td><td align="right">430,126</td><td align="right">429,961</td><td align="right">459,090</td><td align="right">23,704</td></tr>
    <tr><td align="left">Half (size=24.50x9.5)</td><td align="right">157,744</td><td align="right">263,111</td><td align="right">120,855</td><td align="right">71,562</td></tr>
    <tr><td align="left">Quarter (size=12.25x4.75)</td><td align="right">247,926</td><td align="right">262,495</td><td align="right">181,191</td><td align="right">158,034</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">305,499</td><td align="right">302,558</td><td align="right">400,384</td><td align="right">237,879</td></tr>
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
    <tr><td align="left">500 neighbors</td><td align="right">42,223</td><td align="right">42,920</td><td align="right">36,490</td><td align="right">59,756</td></tr>
    <tr><td align="left">100 neighbors</td><td align="right">69,426</td><td align="right">67,325</td><td align="right">75,633</td><td align="right">164,039</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">222,227</td><td align="right">243,970</td><td align="right">195,434</td><td align="right">236,157</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">278,527</td><td align="right">245,828</td><td align="right">195,817</td><td align="right">250,082</td></tr>
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
    <tr><td align="left">100 entries</td><td align="right">41,322 (0.000s)</td><td align="right">32,362 (0.000s)</td><td align="right">27,322 (0.000s)</td><td align="right">20,703 (0.000s)</td></tr>
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
    <tr><td align="left">Full (~span/2) (r=4.5)</td><td align="right">438,910</td><td align="right">439,298</td><td align="right">438,875</td><td align="right">69,290</td></tr>
    <tr><td align="left">Half (~span/4) (r=2.25)</td><td align="right">378,963</td><td align="right">382,697</td><td align="right">237,452</td><td align="right">205,575</td></tr>
    <tr><td align="left">Quarter (~span/8) (r=1.13)</td><td align="right">379,230</td><td align="right">382,620</td><td align="right">494,646</td><td align="right">277,996</td></tr>
    <tr><td align="left">Tiny (~span/1000) (r=1)</td><td align="right">379,206</td><td align="right">382,740</td><td align="right">496,709</td><td align="right">278,197</td></tr>
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
    <tr><td align="left">Full (size=9x9)</td><td align="right">1,272,354</td><td align="right">1,354,621</td><td align="right">1,394,343</td><td align="right">195,271</td></tr>
    <tr><td align="left">Half (size=4.5x4.5)</td><td align="right">476,706</td><td align="right">472,738</td><td align="right">323,833</td><td align="right">299,443</td></tr>
    <tr><td align="left">Quarter (size=2.25x2.25)</td><td align="right">493,145</td><td align="right">500,843</td><td align="right">622,143</td><td align="right">314,123</td></tr>
    <tr><td align="left">Unit (size=1)</td><td align="right">493,109</td><td align="right">499,483</td><td align="right">620,950</td><td align="right">314,175</td></tr>
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
    <tr><td align="left">100 neighbors (max)</td><td align="right">123,698</td><td align="right">123,197</td><td align="right">135,888</td><td align="right">180,194</td></tr>
    <tr><td align="left">10 neighbors</td><td align="right">250,243</td><td align="right">208,673</td><td align="right">255,381</td><td align="right">271,081</td></tr>
    <tr><td align="left">1 neighbor</td><td align="right">244,327</td><td align="right">312,085</td><td align="right">267,884</td><td align="right">290,000</td></tr>
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
