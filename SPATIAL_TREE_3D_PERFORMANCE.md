# 3D Spatial Tree Performance Benchmarks

This document contains performance benchmarks for the 3D spatial tree implementations in Unity Helpers.

## Available 3D Spatial Trees

- **OctTree3D** - Easiest to use, good all-around performance for 3D
- **KDTree3D** - Balanced and unbalanced variants available
- **RTree3D** - Optimized for 3D bounding box queries

## Performance Benchmarks

<!-- SPATIAL_TREE_3D_BENCHMARKS_START -->

<!-- tabs:start -->



#### **1,000,000 entries**



_Run the performance tests to populate these tables._



#### **100,000 entries**



_Run the performance tests to populate these tables._



#### **10,000 entries**



_Run the performance tests to populate these tables._



#### **1,000 entries**



_Run the performance tests to populate these tables._



#### **100 entries**



_Run the performance tests to populate these tables._



<!-- tabs:end -->

<!-- SPATIAL_TREE_3D_BENCHMARKS_END -->

## Interpreting the Results

All numbers represent **operations per second** (higher is better), except for construction times which show operations per second and absolute time.

### Choosing the Right Tree

**OctTree3D**:
- Best for: General-purpose 3D spatial queries
- Strengths: Balanced performance, easy to use, good spatial locality
- Use cases: 3D collision detection, visibility culling, spatial audio

**KDTree3D (Balanced)**:
- Best for: Nearest-neighbor queries in 3D space
- Strengths: Fast point queries, good for smaller datasets
- Use cases: Pathfinding, AI spatial awareness, particle systems

**KDTree3D (Unbalanced)**:
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
