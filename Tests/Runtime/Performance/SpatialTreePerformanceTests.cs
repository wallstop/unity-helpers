namespace WallstopStudios.UnityHelpers.Tests.Performance
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.DataStructure;

    public sealed class SpatialTreePerformanceTests
    {
        private const int PointsPerAxis = 1_000;
        private const float PointBoundsSize = 0.001f;
        private static readonly TimeSpan BenchmarkDuration = TimeSpan.FromSeconds(1);

        private static readonly (string Label, float Radius)[] RangeBenchmarks =
        {
            ("Full (r=500)", 500f),
            ("Half (r=250)", 250f),
            ("Quarter (r=125)", 125f),
            ("Tiny (r=1)", 1f),
        };

        private static readonly (string Label, int Count)[] NeighborBenchmarks =
        {
            ("500 neighbors", 500),
            ("100 neighbors", 100),
            ("10 neighbors", 10),
            ("1 neighbor", 1),
        };

        private readonly struct TreeSpec
        {
            public TreeSpec(string name, Func<IEnumerable<Vector2>, ISpatialTree<Vector2>> factory)
            {
                Name = name;
                Factory = factory;
            }

            public string Name { get; }

            public Func<IEnumerable<Vector2>, ISpatialTree<Vector2>> Factory { get; }
        }

        private readonly struct BoundsSpec
        {
            public BoundsSpec(string label, Bounds bounds)
            {
                Label = label;
                Bounds = bounds;
            }

            public string Label { get; }

            public Bounds Bounds { get; }
        }

        [UnityTest]
        [Timeout(0)]
        public IEnumerator Benchmark()
        {
            Vector2[] points = CreateGridPoints();
            BoundsSpec[] boundsSpecs = BuildBoundsSpecs();
            TreeSpec[] treeSpecs = BuildTreeSpecs();

            List<string> treeNames = treeSpecs.Select(spec => spec.Name).ToList();
            List<string> readmeLines = new();

            Dictionary<string, List<string>> groupRows = new();
            Dictionary<string, Dictionary<string, string>> rowValues = new();
            Dictionary<string, (string Group, string Label)> rowMetadata = new();
            Dictionary<string, int> expectedCounts = new();

            foreach (TreeSpec spec in treeSpecs)
            {
                Stopwatch timer = Stopwatch.StartNew();
                ISpatialTree<Vector2> tree = spec.Factory(points);
                timer.Stop();

                RecordRow(
                    groupRows,
                    rowValues,
                    rowMetadata,
                    "Construction",
                    "1 million points",
                    spec.Name,
                    FormatConstruction(timer.Elapsed)
                );

                Vector3 boundaryCenter = tree.Boundary.center;
                float minimumDepth = Mathf.Max(tree.Boundary.size.z, 1f);
                Vector2 rangeCenter = boundaryCenter;

                List<Vector2> rangeResults = new();
                foreach ((string label, float radius) in RangeBenchmarks)
                {
                    tree.GetElementsInRange(rangeCenter, radius, rangeResults);
                    ValidateCount(expectedCounts, "Elements In Range", label, rangeResults.Count);
                    int iterations = MeasureRange(tree, rangeCenter, radius, rangeResults);
                    RecordRow(
                        groupRows,
                        rowValues,
                        rowMetadata,
                        "Elements In Range",
                        label,
                        spec.Name,
                        FormatRate(iterations)
                    );
                }

                List<Vector2> boundsResults = new();
                foreach (BoundsSpec boundsSpec in boundsSpecs)
                {
                    Bounds queryBounds = TranslateBounds(
                        boundsSpec.Bounds,
                        boundaryCenter,
                        minimumDepth
                    );
                    tree.GetElementsInBounds(queryBounds, boundsResults);
                    ValidateCount(
                        expectedCounts,
                        "Get Elements In Bounds",
                        boundsSpec.Label,
                        boundsResults.Count
                    );
                    int iterations = MeasureBounds(tree, queryBounds, boundsResults);
                    RecordRow(
                        groupRows,
                        rowValues,
                        rowMetadata,
                        "Get Elements In Bounds",
                        boundsSpec.Label,
                        spec.Name,
                        FormatRate(iterations)
                    );
                }

                List<Vector2> nearestNeighbors = new();
                foreach ((string label, int count) in NeighborBenchmarks)
                {
                    tree.GetApproximateNearestNeighbors(rangeCenter, count, nearestNeighbors);
                    Assert.LessOrEqual(
                        nearestNeighbors.Count,
                        count,
                        $"Tree '{spec.Name}' returned more than {count} neighbors for '{label}'."
                    );
                    int iterations = MeasureApproximateNearestNeighbors(
                        tree,
                        rangeCenter,
                        count,
                        nearestNeighbors
                    );
                    RecordRow(
                        groupRows,
                        rowValues,
                        rowMetadata,
                        "Approximate Nearest Neighbors",
                        label,
                        spec.Name,
                        FormatRate(iterations)
                    );
                }
            }

            string[] groupOrder =
            {
                "Construction",
                "Elements In Range",
                "Get Elements In Bounds",
                "Approximate Nearest Neighbors",
            };

            foreach (string group in groupOrder)
            {
                if (!groupRows.TryGetValue(group, out List<string>? rows))
                {
                    continue;
                }

                LogTable(group, treeNames, rows, rowValues, rowMetadata, readmeLines);
                UnityEngine.Debug.Log(string.Empty);
            }

            if (readmeLines.Count > 0 && string.IsNullOrWhiteSpace(readmeLines[^1]))
            {
                readmeLines.RemoveAt(readmeLines.Count - 1);
            }

            BenchmarkReadmeUpdater.UpdateSection("SPATIAL_TREE_BENCHMARKS", readmeLines);

            yield break;
        }

        private static TreeSpec[] BuildTreeSpecs()
        {
            return new[]
            {
                new TreeSpec("KDTree (Balanced)", points => new KDTree<Vector2>(points, p => p)),
                new TreeSpec(
                    "KDTree (Unbalanced)",
                    points => new KDTree<Vector2>(points, p => p, balanced: false)
                ),
                new TreeSpec("QuadTree", points => new QuadTree<Vector2>(points, p => p)),
                new TreeSpec("RTree", points => new RTree<Vector2>(points, CreatePointBounds)),
            };
        }

        private static BoundsSpec[] BuildBoundsSpecs()
        {
            float span = PointsPerAxis - 1;
            Vector3 center = new(span * 0.5f, span * 0.5f, 0f);
            Vector3 fullSize = new(span, span, 1f);

            return new[]
            {
                new BoundsSpec("Full (size≈dataset)", new Bounds(center, fullSize)),
                new BoundsSpec("Half (size≈dataset/2)", new Bounds(center, fullSize * 0.5f)),
                new BoundsSpec("Quarter (size≈dataset/4)", new Bounds(center, fullSize * 0.25f)),
                new BoundsSpec("Unit (size=1)", new Bounds(center, new Vector3(1f, 1f, 1f))),
            };
        }

        private static Vector2[] CreateGridPoints()
        {
            Vector2[] points = new Vector2[PointsPerAxis * PointsPerAxis];
            Parallel.For(
                0,
                PointsPerAxis,
                y =>
                {
                    int rowOffset = y * PointsPerAxis;
                    for (int x = 0; x < PointsPerAxis; ++x)
                    {
                        points[rowOffset + x] = new Vector2(x, y);
                    }
                }
            );

            return points;
        }

        private static Bounds TranslateBounds(
            Bounds template,
            Vector3 newCenter,
            float minimumDepth
        )
        {
            Vector3 size = template.size;
            size.z = Mathf.Max(size.z, minimumDepth);
            return new Bounds(newCenter, size);
        }

        private static int MeasureRange(
            ISpatialTree<Vector2> tree,
            Vector2 center,
            float radius,
            List<Vector2> buffer
        )
        {
            Stopwatch timer = Stopwatch.StartNew();
            int iterations = 0;
            do
            {
                tree.GetElementsInRange(center, radius, buffer);
                ++iterations;
            } while (timer.Elapsed < BenchmarkDuration);

            return iterations;
        }

        private static int MeasureBounds(
            ISpatialTree<Vector2> tree,
            Bounds bounds,
            List<Vector2> buffer
        )
        {
            Stopwatch timer = Stopwatch.StartNew();
            int iterations = 0;
            do
            {
                tree.GetElementsInBounds(bounds, buffer);
                ++iterations;
            } while (timer.Elapsed < BenchmarkDuration);

            return iterations;
        }

        private static int MeasureApproximateNearestNeighbors(
            ISpatialTree<Vector2> tree,
            Vector2 center,
            int count,
            List<Vector2> buffer
        )
        {
            Stopwatch timer = Stopwatch.StartNew();
            int iterations = 0;
            do
            {
                tree.GetApproximateNearestNeighbors(center, count, buffer);
                ++iterations;
            } while (timer.Elapsed < BenchmarkDuration);

            return iterations;
        }

        private static void RecordRow(
            Dictionary<string, List<string>> groupRows,
            Dictionary<string, Dictionary<string, string>> rowValues,
            Dictionary<string, (string Group, string Label)> rowMetadata,
            string group,
            string label,
            string treeName,
            string value
        )
        {
            string key = $"{group}::{label}";

            if (!groupRows.TryGetValue(group, out List<string>? rows))
            {
                rows = new List<string>();
                groupRows[group] = rows;
            }

            if (!rowValues.TryGetValue(key, out Dictionary<string, string>? row))
            {
                row = new Dictionary<string, string>();
                rowValues[key] = row;
                rows.Add(key);
                rowMetadata[key] = (group, label);
            }

            row[treeName] = value;
        }

        private static void ValidateCount(
            IDictionary<string, int> expectedCounts,
            string group,
            string label,
            int actualCount
        )
        {
            string key = $"{group}::{label}";
            if (!expectedCounts.TryGetValue(key, out int expected))
            {
                expectedCounts[key] = actualCount;
                return;
            }

            Assert.AreEqual(
                expected,
                actualCount,
                $"Expected '{group}' -> '{label}' to return {expected} elements, but received {actualCount}."
            );
        }

        private static string FormatRate(int iterations)
        {
            double perSecond = iterations / BenchmarkDuration.TotalSeconds;
            return perSecond.ToString("N0");
        }

        private static string FormatConstruction(TimeSpan elapsed)
        {
            if (elapsed <= TimeSpan.Zero)
            {
                return "n/a";
            }

            double rate = Math.Floor(1d / elapsed.TotalSeconds);
            return $"{rate:N0} ({elapsed.TotalSeconds:F3}s)";
        }

        private static void LogTable(
            string header,
            IReadOnlyList<string> treeNames,
            IEnumerable<string> rowKeys,
            IReadOnlyDictionary<string, Dictionary<string, string>> rowValues,
            IReadOnlyDictionary<string, (string Group, string Label)> rowMetadata,
            ICollection<string>? readmeLines
        )
        {
            string headerLine = $"| {header} | {string.Join(" | ", treeNames)} |";
            string dividerLine =
                $"| {string.Join(" | ", Enumerable.Repeat("---", treeNames.Count + 1))} |";

            if (readmeLines != null)
            {
                readmeLines.Add($"#### {header}");
                readmeLines.Add(headerLine);
                readmeLines.Add(dividerLine);
            }

            UnityEngine.Debug.Log(headerLine);
            UnityEngine.Debug.Log(dividerLine);

            foreach (string rowKey in rowKeys)
            {
                string label = rowMetadata[rowKey].Label;
                Dictionary<string, string> values = rowValues[rowKey];
                string rowLine =
                    "| "
                    + label
                    + " | "
                    + string.Join(
                        " | ",
                        treeNames.Select(name =>
                            values.TryGetValue(name, out string value) ? value : string.Empty
                        )
                    )
                    + " |";
                UnityEngine.Debug.Log(rowLine);
                readmeLines?.Add(rowLine);
            }

            readmeLines?.Add(string.Empty);
        }

        private static Bounds CreatePointBounds(Vector2 point)
        {
            return new Bounds(
                new Vector3(point.x, point.y, 0f),
                new Vector3(PointBoundsSize, PointBoundsSize, 1f)
            );
        }
    }
}
