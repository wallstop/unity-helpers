namespace WallstopStudios.UnityHelpers.Tests.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Helper;
    using WallstopStudios.UnityHelpers.Tests.TestUtils;

    public sealed class GeometryTests : CommonTestBase
    {
        [Test]
        public void AccumulateSingleRect()
        {
            Rect rect = new(1, 2, 3, 4);
            IEnumerable<Rect> rects = new[] { rect };
            Rect result = rects.Accumulate();
            Assert.AreEqual(rect, result, "Single rect should return itself.");
        }

        [Test]
        public void AccumulateTwoRects()
        {
            Rect rect1 = new(0, 0, 5, 5);
            Rect rect2 = new(3, 3, 5, 5);
            IEnumerable<Rect> rects = new[] { rect1, rect2 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(0, result.xMin, "xMin should be 0.");
            Assert.AreEqual(0, result.yMin, "yMin should be 0.");
            Assert.AreEqual(8, result.xMax, "xMax should be 8.");
            Assert.AreEqual(8, result.yMax, "yMax should be 8.");
        }

        [Test]
        public void AccumulateMultipleRects()
        {
            Rect rect1 = new(0, 0, 5, 5);
            Rect rect2 = new(10, 10, 5, 5);
            Rect rect3 = new(-5, -5, 3, 3);
            IEnumerable<Rect> rects = new[] { rect1, rect2, rect3 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(-5, result.xMin, "xMin should be -5.");
            Assert.AreEqual(-5, result.yMin, "yMin should be -5.");
            Assert.AreEqual(15, result.xMax, "xMax should be 15.");
            Assert.AreEqual(15, result.yMax, "yMax should be 15.");
        }

        [Test]
        public void AccumulateNegativeCoordinates()
        {
            Rect rect1 = new(-10, -10, 5, 5);
            Rect rect2 = new(-20, -20, 8, 8);
            IEnumerable<Rect> rects = new[] { rect1, rect2 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(-20, result.xMin, "xMin should be -20.");
            Assert.AreEqual(-20, result.yMin, "yMin should be -20.");
            Assert.AreEqual(-5, result.xMax, "xMax should be -5.");
            Assert.AreEqual(-5, result.yMax, "yMax should be -5.");
        }

        [Test]
        public void AccumulateZeroSizedRects()
        {
            Rect rect1 = new(5, 5, 0, 0);
            Rect rect2 = new(10, 10, 0, 0);
            IEnumerable<Rect> rects = new[] { rect1, rect2 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(5, result.xMin, "xMin should be 5.");
            Assert.AreEqual(5, result.yMin, "yMin should be 5.");
            Assert.AreEqual(10, result.xMax, "xMax should be 10.");
            Assert.AreEqual(10, result.yMax, "yMax should be 10.");
        }

        [Test]
        public void AccumulateMixedPositiveAndNegative()
        {
            Rect rect1 = new(-5, -5, 10, 10);
            Rect rect2 = new(3, 3, 7, 7);
            IEnumerable<Rect> rects = new[] { rect1, rect2 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(-5, result.xMin, "xMin should be -5.");
            Assert.AreEqual(-5, result.yMin, "yMin should be -5.");
            Assert.AreEqual(10, result.xMax, "xMax should be 10.");
            Assert.AreEqual(10, result.yMax, "yMax should be 10.");
        }

        [Test]
        public void AccumulateIdenticalRects()
        {
            Rect rect = new(1, 2, 3, 4);
            IEnumerable<Rect> rects = new[] { rect, rect, rect };
            Rect result = rects.Accumulate();
            Assert.AreEqual(rect, result, "Identical rects should return the same rect.");
        }

        [Test]
        public void AccumulateEmptyEnumerable()
        {
            IEnumerable<Rect> rects = Enumerable.Empty<Rect>();
            Assert.Throws<InvalidOperationException>(
                () => rects.Accumulate(),
                "Empty enumerable should throw."
            );
        }

        [Test]
        public void AccumulateOverlappingRects()
        {
            Rect rect1 = new(0, 0, 10, 10);
            Rect rect2 = new(5, 5, 10, 10);
            Rect rect3 = new(2, 2, 5, 5);
            IEnumerable<Rect> rects = new[] { rect1, rect2, rect3 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(0, result.xMin, "xMin should be 0.");
            Assert.AreEqual(0, result.yMin, "yMin should be 0.");
            Assert.AreEqual(15, result.xMax, "xMax should be 15.");
            Assert.AreEqual(15, result.yMax, "yMax should be 15.");
        }

        [Test]
        public void AccumulateVeryLargeCoordinates()
        {
            Rect rect1 = new(0, 0, 10, 10);
            Rect rect2 = new(100000, 100000, 5, 5);
            IEnumerable<Rect> rects = new[] { rect1, rect2 };
            Rect result = rects.Accumulate();

            Assert.AreEqual(0, result.xMin, "xMin should be 0.");
            Assert.AreEqual(0, result.yMin, "yMin should be 0.");
            Assert.AreEqual(100005, result.xMax, "xMax should be 100005.");
            Assert.AreEqual(100005, result.yMax, "yMax should be 100005.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2PointOnLeft()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);
            Vector2 p = new(5, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left of the vector.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2PointOnRight()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);
            Vector2 p = new(5, -5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result < 0, "Point should be to the right of the vector.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2PointOnLine()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);
            Vector2 p = new(5, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point on the line should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2PointIsA()
        {
            Vector2 a = new(3, 4);
            Vector2 b = new(10, 8);
            Vector2 p = new(3, 4);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'a' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2PointIsB()
        {
            Vector2 a = new(3, 4);
            Vector2 b = new(10, 8);
            Vector2 p = new(10, 8);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'b' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2AllSamePoint()
        {
            Vector2 a = new(5, 5);
            Vector2 b = new(5, 5);
            Vector2 p = new(5, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "All same points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2NegativeCoordinates()
        {
            Vector2 a = new(-10, -10);
            Vector2 b = new(-5, -5);
            Vector2 p = new(-8, -6);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left with negative coordinates.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2VerticalLine()
        {
            Vector2 a = new(5, 0);
            Vector2 b = new(5, 10);
            Vector2 p = new(3, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point to the left of vertical line should return positive.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2VerticalLinePointOnRight()
        {
            Vector2 a = new(5, 0);
            Vector2 b = new(5, 10);
            Vector2 p = new(8, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(
                result < 0,
                "Point to the right of vertical line should return negative."
            );
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2DiagonalLine()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 10);
            Vector2 p = new(5, 6);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point above diagonal should be to the left.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2DiagonalLineBelow()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 10);
            Vector2 p = new(6, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result < 0, "Point below diagonal should be to the right.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2CollinearPoints()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 10);
            Vector2 p = new(5, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Collinear points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2LargeCoordinates()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(100000, 0);
            Vector2 p = new(50000, 10000);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left with large coordinates.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3PointOnLeft()
        {
            Vector3 a = new(0, 0, 0);
            Vector3 b = new(10, 0, 0);
            Vector3 p = new(5, 5, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left of the vector (ignoring z).");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3PointOnRight()
        {
            Vector3 a = new(0, 0, 0);
            Vector3 b = new(10, 0, 0);
            Vector3 p = new(5, -5, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result < 0, "Point should be to the right of the vector (ignoring z).");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3PointOnLine()
        {
            Vector3 a = new(0, 0, 0);
            Vector3 b = new(10, 0, 0);
            Vector3 p = new(5, 0, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point on the line should return 0 (ignoring z).");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3IgnoresZ()
        {
            Vector3 a = new(0, 0, 5);
            Vector3 b = new(10, 0, 10);
            Vector3 p = new(5, 5, 100);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Z coordinate should be ignored in calculation.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3PointIsA()
        {
            Vector3 a = new(3, 4, 7);
            Vector3 b = new(10, 8, 2);
            Vector3 p = new(3, 4, 7);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'a' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3PointIsB()
        {
            Vector3 a = new(3, 4, 5);
            Vector3 b = new(10, 8, 1);
            Vector3 p = new(10, 8, 1);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'b' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3AllSamePoint()
        {
            Vector3 a = new(5, 5, 5);
            Vector3 b = new(5, 5, 5);
            Vector3 p = new(5, 5, 5);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "All same points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3NegativeCoordinates()
        {
            Vector3 a = new(-10, -10, -10);
            Vector3 b = new(-5, -5, -5);
            Vector3 p = new(-8, -6, -3);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left with negative coordinates.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3VerticalLine()
        {
            Vector3 a = new(5, 0, 0);
            Vector3 b = new(5, 10, 0);
            Vector3 p = new(3, 5, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point to the left of vertical line should return positive.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector3CollinearPoints()
        {
            Vector3 a = new(0, 0, 0);
            Vector3 b = new(10, 10, 0);
            Vector3 p = new(5, 5, 0);
            float result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Collinear points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntPointOnLeft()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 0);
            Vector2Int p = new(5, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left of the vector.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntPointOnRight()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 0);
            Vector2Int p = new(5, -5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result < 0, "Point should be to the right of the vector.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntPointOnLine()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 0);
            Vector2Int p = new(5, 0);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point on the line should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntPointIsA()
        {
            Vector2Int a = new(3, 4);
            Vector2Int b = new(10, 8);
            Vector2Int p = new(3, 4);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'a' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntPointIsB()
        {
            Vector2Int a = new(3, 4);
            Vector2Int b = new(10, 8);
            Vector2Int p = new(10, 8);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Point at position 'b' should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntAllSamePoint()
        {
            Vector2Int a = new(5, 5);
            Vector2Int b = new(5, 5);
            Vector2Int p = new(5, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "All same points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntNegativeCoordinates()
        {
            Vector2Int a = new(-10, -10);
            Vector2Int b = new(-5, -5);
            Vector2Int p = new(-8, -6);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left with negative coordinates.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntVerticalLine()
        {
            Vector2Int a = new(5, 0);
            Vector2Int b = new(5, 10);
            Vector2Int p = new(3, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point to the left of vertical line should return positive.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntVerticalLinePointOnRight()
        {
            Vector2Int a = new(5, 0);
            Vector2Int b = new(5, 10);
            Vector2Int p = new(8, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(
                result < 0,
                "Point to the right of vertical line should return negative."
            );
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntDiagonalLine()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 10);
            Vector2Int p = new(5, 6);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point above diagonal should be to the left.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntDiagonalLineBelow()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 10);
            Vector2Int p = new(6, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result < 0, "Point below diagonal should be to the right.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntCollinearPoints()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10, 10);
            Vector2Int p = new(5, 5);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.AreEqual(0, result, "Collinear points should return 0.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntLargeCoordinates()
        {
            Vector2Int a = new(0, 0);
            Vector2Int b = new(10000, 0);
            Vector2Int p = new(5000, 1000);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Point should be to the left with large coordinates.");
        }

        [Test]
        public void IsAPointLeftOfVectorOrOnTheLineVector2IntOverflowScenario()
        {
            // Test for potential integer overflow
            Vector2Int a = new(0, 0);
            Vector2Int b = new(30000, 30000);
            Vector2Int p = new(15000, 15001);
            int result = Geometry.IsAPointLeftOfVectorOrOnTheLine(a, b, p);
            Assert.IsTrue(result > 0, "Should handle large values without overflow.");
        }
    }
}
