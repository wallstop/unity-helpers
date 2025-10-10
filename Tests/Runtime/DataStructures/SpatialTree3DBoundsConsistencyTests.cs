namespace WallstopStudios.UnityHelpers.Tests.DataStructures
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.DataStructure;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;
    using Vector3 = UnityEngine.Vector3;

    public sealed class SpatialTree3DBoundsConsistencyTests
    {
        private static Vector3[] CreateGridPoints(Vector3Int gridSize)
        {
            int total = gridSize.x * gridSize.y * gridSize.z;
            Vector3[] points = new Vector3[total];
            int width = gridSize.x;
            int height = gridSize.y;
            int depth = gridSize.z;
            int index = 0;
            for (int z = 0; z < depth; ++z)
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        points[index++] = new Vector3(x, y, z);
                    }
                }
            }
            return points;
        }

        private static Bounds[] BuildBoundsSpecs(Vector3Int gridSize)
        {
            Vector3 span = new Vector3(
                Mathf.Max(gridSize.x - 1, 1),
                Mathf.Max(gridSize.y - 1, 1),
                Mathf.Max(gridSize.z - 1, 1)
            );
            Vector3 center = new Vector3(
                (gridSize.x - 1) * 0.5f,
                (gridSize.y - 1) * 0.5f,
                (gridSize.z - 1) * 0.5f
            );

            Bounds Scale(Vector3 ratio)
            {
                Vector3 size = new Vector3(
                    Mathf.Max(span.x * ratio.x, 1f),
                    Mathf.Max(span.y * ratio.y, 1f),
                    Mathf.Max(span.z * ratio.z, 1f)
                );
                return new Bounds(center, size);
            }

            var specs = new List<Bounds>
            {
                Scale(new Vector3(1f, 1f, 1f)),
                Scale(new Vector3(0.5f, 0.5f, 0.5f)),
                Scale(new Vector3(0.25f, 0.25f, 0.25f)),
                new Bounds(center, new Vector3(1f, 1f, 1f)),
            };
            return specs.ToArray();
        }

        [Test]
        public void BoundsDefinitionsOnTenAndTwentyGridsMatchAcrossTrees()
        {
            Vector3Int[] sizes = { new Vector3Int(10, 10, 10), new Vector3Int(20, 20, 20) };
            foreach (Vector3Int size in sizes)
            {
                Vector3[] points = CreateGridPoints(size);
                KdTree3D<Vector3> kd = new(points, p => p);
                OctTree3D<Vector3> oct = new(points, p => p);
                Bounds[] boundsSpecs = BuildBoundsSpecs(size);

                foreach (Bounds b in boundsSpecs)
                {
                    List<Vector3> kdResults = new();
                    kd.GetElementsInBounds(b, kdResults);

                    List<Vector3> octResults = new();
                    oct.GetElementsInBounds(b, octResults);

                    Assert.AreEqual(
                        kdResults.Count,
                        octResults.Count,
                        "Bounds mismatch on grid {0}: center={1}, size={2}. KD={3}, Oct={4}",
                        size,
                        b.center,
                        b.size,
                        kdResults.Count,
                        octResults.Count
                    );

                    if (kdResults.Count <= 20000)
                    {
                        CollectionAssert.AreEquivalent(
                            kdResults,
                            octResults,
                            "Element mismatch on grid {0}: center={1}, size={2}",
                            size,
                            b.center,
                            b.size
                        );
                    }
                }
            }
        }

        [Test]
        [Timeout(15000)]
        public void FullBoundsOnHundredGridCountsMatchAcrossTrees()
        {
            Vector3Int size = new(100, 100, 100);
            Vector3[] points = CreateGridPoints(size);
            KdTree3D<Vector3> kd = new(points, p => p);
            OctTree3D<Vector3> oct = new(points, p => p);

            Bounds fullBounds = new Bounds(
                new Vector3(49.5f, 49.5f, 49.5f),
                new Vector3(99f, 99f, 99f)
            );

            List<Vector3> kdResults = new();
            kd.GetElementsInBounds(fullBounds, kdResults);
            List<Vector3> octResults = new();
            oct.GetElementsInBounds(fullBounds, octResults);

            Assert.AreEqual(
                kdResults.Count,
                octResults.Count,
                "KDTree3D and OctTree3D returned different counts for full bounds: KD={0}, Oct={1}",
                kdResults.Count,
                octResults.Count
            );

            Assert.AreEqual(
                1_000_000,
                kdResults.Count,
                "Expected full dataset bounds to return all elements, but KD returned {0}.",
                kdResults.Count
            );

            Assert.AreEqual(
                1_000_000,
                octResults.Count,
                "Expected full dataset bounds to return all elements, but Oct returned {0}.",
                octResults.Count
            );
        }

        [Test]
        public void VariousCentersAndSizesProduceMatchingResults()
        {
            Vector3Int size = new(16, 16, 16);
            Vector3[] points = CreateGridPoints(size);
            KdTree3D<Vector3> kd = new(points, p => p);
            OctTree3D<Vector3> oct = new(points, p => p);

            List<(Vector3 Center, Vector3 Size)> cases = new()
            {
                // Unit at grid center
                (new Vector3(7.5f, 7.5f, 7.5f), new Vector3(1f, 1f, 1f)),
                // Unit aligned to axes faces
                (new Vector3(0.5f, 7.5f, 7.5f), new Vector3(1f, 1f, 1f)),
                (new Vector3(15.5f, 7.5f, 7.5f), new Vector3(1f, 1f, 1f)),
                (new Vector3(7.5f, 0.5f, 7.5f), new Vector3(1f, 1f, 1f)),
                (new Vector3(7.5f, 15.5f, 7.5f), new Vector3(1f, 1f, 1f)),
                (new Vector3(7.5f, 7.5f, 0.5f), new Vector3(1f, 1f, 1f)),
                (new Vector3(7.5f, 7.5f, 15.5f), new Vector3(1f, 1f, 1f)),
                // Non-uniform sizes
                (new Vector3(7.5f, 7.5f, 7.5f), new Vector3(3f, 1f, 2f)),
                (new Vector3(7.5f, 7.5f, 7.5f), new Vector3(5f, 2f, 1f)),
                // Off-center fractional
                (new Vector3(6.25f, 8.75f, 7.5f), new Vector3(2f, 2f, 2f)),
                (new Vector3(6.25f, 8.75f, 7.5f), new Vector3(1f, 3f, 5f)),
            };

            foreach ((Vector3 center, Vector3 sizeVec) in cases)
            {
                Vector3 clampedSize = new(
                    Mathf.Max(sizeVec.x, 1f),
                    Mathf.Max(sizeVec.y, 1f),
                    Mathf.Max(sizeVec.z, 1f)
                );
                Bounds b = new Bounds(center, clampedSize);

                List<Vector3> kdResults = new();
                kd.GetElementsInBounds(b, kdResults);
                List<Vector3> octResults = new();
                oct.GetElementsInBounds(b, octResults);

                Assert.AreEqual(
                    kdResults.Count,
                    octResults.Count,
                    "Bounds mismatch at center={0}, size={1}. KD={2}, Oct={3}",
                    center,
                    clampedSize,
                    kdResults.Count,
                    octResults.Count
                );

                if (kdResults.Count <= 20000)
                {
                    CollectionAssert.AreEquivalent(
                        kdResults,
                        octResults,
                        "Element mismatch at center={0}, size={1}",
                        center,
                        clampedSize
                    );
                }
            }
        }

        [Test]
        public void SlidingWindowAlongAxesMatchesAcrossTrees()
        {
            Vector3Int grid = new(12, 12, 12);
            Vector3[] points = CreateGridPoints(grid);
            KdTree3D<Vector3> kd = new(points, p => p);
            OctTree3D<Vector3> oct = new(points, p => p);

            Vector3 baseSize = new Vector3(3f, 3f, 3f);
            for (int i = 0; i <= 9; ++i)
            {
                float c = i + 1.5f; // slides centers from 1.5 to 10.5
                Bounds bx = new Bounds(new Vector3(c, 5.5f, 5.5f), baseSize);
                Bounds by = new Bounds(new Vector3(5.5f, c, 5.5f), baseSize);
                Bounds bz = new Bounds(new Vector3(5.5f, 5.5f, c), baseSize);

                foreach (Bounds b in new[] { bx, by, bz })
                {
                    List<Vector3> kdResults = new();
                    kd.GetElementsInBounds(b, kdResults);
                    List<Vector3> octResults = new();
                    oct.GetElementsInBounds(b, octResults);

                    Assert.AreEqual(
                        kdResults.Count,
                        octResults.Count,
                        "Sliding mismatch at center={0}, size={1}. KD={2}, Oct={3}",
                        b.center,
                        b.size,
                        kdResults.Count,
                        octResults.Count
                    );

                    if (kdResults.Count <= 20000)
                    {
                        CollectionAssert.AreEquivalent(
                            kdResults,
                            octResults,
                            "Sliding elements mismatch at center={0}, size={1}",
                            b.center,
                            b.size
                        );
                    }
                }
            }
        }

        [Test]
        public void UnitBoundsAtGridCenterOnTenGridConsistentWithKDTree()
        {
            Vector3Int size = new(10, 10, 10);
            Vector3[] points = CreateGridPoints(size);
            KdTree3D<Vector3> kd = new(points, p => p);
            OctTree3D<Vector3> oct = new(points, p => p);

            Bounds b = new Bounds(new Vector3(4.5f, 4.5f, 4.5f), new Vector3(1f, 1f, 1f));
            List<Vector3> kdResults = new();
            kd.GetElementsInBounds(b, kdResults);
            List<Vector3> octResults = new();
            oct.GetElementsInBounds(b, octResults);

            Assert.AreEqual(
                kdResults.Count,
                octResults.Count,
                "Unit bounds at grid center mismatch. KD={0}, Oct={1}",
                kdResults.Count,
                octResults.Count
            );

            if (kdResults.Count <= 20000)
            {
                CollectionAssert.AreEquivalent(kdResults, octResults);
            }

            Assert.AreEqual(
                5,
                kdResults.Count,
                "Expected KD to return 5 points for unit bounds at grid center, got {0}.",
                kdResults.Count
            );
        }

        [Test]
        public void EdgeTouchingBoundsConsistentAcrossTrees()
        {
            Vector3Int size = new(10, 10, 10);
            Vector3[] points = CreateGridPoints(size);
            KdTree3D<Vector3> kd = new(points, p => p);
            OctTree3D<Vector3> oct = new(points, p => p);

            Bounds[] cases =
            {
                new Bounds(new Vector3(0.5f, 4.5f, 4.5f), new Vector3(1f, 1f, 1f)),
                new Bounds(new Vector3(9.5f, 4.5f, 4.5f), new Vector3(1f, 1f, 1f)),
                new Bounds(new Vector3(4.5f, 0.5f, 4.5f), new Vector3(1f, 1f, 1f)),
                new Bounds(new Vector3(4.5f, 9.5f, 4.5f), new Vector3(1f, 1f, 1f)),
                new Bounds(new Vector3(4.5f, 4.5f, 0.5f), new Vector3(1f, 1f, 1f)),
                new Bounds(new Vector3(4.5f, 4.5f, 9.5f), new Vector3(1f, 1f, 1f)),
            };

            foreach (Bounds b in cases)
            {
                List<Vector3> kdResults = new();
                kd.GetElementsInBounds(b, kdResults);
                List<Vector3> octResults = new();
                oct.GetElementsInBounds(b, octResults);

                Assert.AreEqual(
                    kdResults.Count,
                    octResults.Count,
                    "Edge touching bounds mismatch at center={0}, size={1}. KD={2}, Oct={3}",
                    b.center,
                    b.size,
                    kdResults.Count,
                    octResults.Count
                );

                if (kdResults.Count <= 20000)
                {
                    CollectionAssert.AreEquivalent(
                        kdResults,
                        octResults,
                        "Edge touching elements mismatch at center={0}, size={1}",
                        b.center,
                        b.size
                    );
                }
            }
        }
    }
}
