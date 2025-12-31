---
---

# Performance Baseline Tests

> Auto-generated via PerformanceBaselineTests.GeneratePerformanceBaselineReport. Run the test explicitly to refresh these tables.

These tests serve as automated CI regression guards. They verify that critical operations complete within acceptable time bounds, detecting performance regressions before they reach production.

## Baseline Philosophy

Baselines are set generously (2-3x expected typical performance) to account for CI environment variability while still catching significant regressions. A test failure indicates a performance regression that needs investigation.

## Test Categories

- **Spatial Trees**: QuadTree2D, KdTree2D, KdTree3D, OctTree3D, RTree2D construction and query performance
- **PRNG**: Random number generation throughput for PcgRandom, XoroShiroRandom, SplitMix64, RomuDuo
- **Pooling**: Collection pool rent/return overhead for List, HashSet, Dictionary, StringBuilder, SystemArrayPool
- **Serialization**: JSON and Protobuf serialization/deserialization throughput

<!-- BASELINE_PERFORMANCE_START -->

## Performance Baseline Report

Run `PerformanceBaselineTests.GeneratePerformanceBaselineReport` explicitly to generate results.

### Spatial Trees

| Test                    | Iterations | Baseline (ms) | Description                               |
| ----------------------- | ---------- | ------------- | ----------------------------------------- |
| QuadTree2DRangeQuery    | 1,000      | 200           | Range queries on 10K elements             |
| QuadTree2DBoundsQuery   | 1,000      | 200           | Bounds queries on 10K elements            |
| KdTree2DRangeQuery      | 1,000      | 200           | Range queries on 10K elements             |
| KdTree2DNearestNeighbor | 1,000      | 200           | Nearest neighbor queries on 10K elements  |
| RTree2DRangeQuery       | 1,000      | 200           | Range queries on 10K elements             |
| OctTree3DRangeQuery     | 1,000      | 200           | 3D range queries on 10K elements          |
| KdTree3DRangeQuery      | 1,000      | 200           | 3D range queries on 10K elements          |
| QuadTree2DConstruction  | 1          | 500           | Construct tree with 10K elements          |
| KdTree2DConstruction    | 1          | 500           | Construct balanced tree with 10K elements |
| RTree2DConstruction     | 1          | 500           | Construct tree with 10K elements          |

### PRNG

| Test                   | Iterations | Baseline (ms) | Description                   |
| ---------------------- | ---------- | ------------- | ----------------------------- |
| PcgRandomNextInt       | 1,000,000  | 500           | Integer generation throughput |
| PcgRandomNextFloat     | 1,000,000  | 500           | Float generation throughput   |
| XoroShiroRandomNextInt | 1,000,000  | 500           | Integer generation throughput |
| SplitMix64NextInt      | 1,000,000  | 500           | Integer generation throughput |
| RomuDuoNextInt         | 1,000,000  | 500           | Integer generation throughput |

### Pooling

| Test                 | Iterations | Baseline (ms) | Description                      |
| -------------------- | ---------- | ------------- | -------------------------------- |
| ListPooling          | 100,000    | 200           | List rent/return cycles          |
| HashSetPooling       | 100,000    | 200           | HashSet rent/return cycles       |
| DictionaryPooling    | 100,000    | 200           | Dictionary rent/return cycles    |
| SystemArrayPool      | 100,000    | 200           | Array rent/return cycles         |
| StringBuilderPooling | 100,000    | 200           | StringBuilder rent/return cycles |

### Serialization

| Test                | Iterations | Baseline (ms) | Description                         |
| ------------------- | ---------- | ------------- | ----------------------------------- |
| JsonSerialize       | 10,000     | 500           | JSON serialization operations       |
| JsonDeserialize     | 10,000     | 500           | JSON deserialization operations     |
| JsonRoundTrip       | 10,000     | 1,000         | JSON serialize + deserialize        |
| ProtobufSerialize   | 10,000     | 500           | Protobuf serialization operations   |
| ProtobufDeserialize | 10,000     | 500           | Protobuf deserialization operations |
| ProtobufRoundTrip   | 10,000     | 1,000         | Protobuf serialize + deserialize    |

<!-- BASELINE_PERFORMANCE_END -->

## Running the Tests

These tests run automatically during CI to catch regressions. To generate fresh benchmark results:

1. Open Unity Test Runner
2. Navigate to `PerformanceBaselineTests`
3. Run `GeneratePerformanceBaselineReport` explicitly (it is marked `[Explicit]`)
4. Results will be output to the console and can be copied to this document

## Interpreting Results

- **Time (ms)**: Actual measured time for the operation
- **Baseline (ms)**: Maximum allowed time before test failure
- **% of Baseline**: How much of the baseline budget was used (lower is better)
- **Status**: Pass if within baseline, Fail if exceeded
