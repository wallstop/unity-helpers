namespace UnityHelpers.Tests.Performance
{
    using System;
    using System.Diagnostics;
    using Components;
    using Components.UnityHelpers.Tests.Components;
    using Core.Attributes;
    using NUnit.Framework;
    using UnityEngine;

    public sealed class RelationComponentPerformanceTests
    {
        [Test]
        public void RelationalPerformanceComplexTest()
        {
            int count = 0;

            GameObject go = new("Test", typeof(RelationalComponentTesterComplex));
            RelationalComponentTesterComplex tester =
                go.GetComponent<RelationalComponentTesterComplex>();
            // Pre-warm
            tester.AssignRelationalComponents();

            TimeSpan timeout = TimeSpan.FromSeconds(10);
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                tester.AssignRelationalComponents();
                ++count;
            } while (timer.Elapsed < timeout);

            UnityEngine.Debug.Log($"Averaged {count / timeout.TotalSeconds} operations / second.");

            Assert.AreNotEqual(0, tester._childColliders.Length);
            Assert.AreNotEqual(0, tester._parentColliders.Length);
            Assert.AreNotEqual(0, tester._siblingColliders.Length);
        }

        [Test]
        public void RelationalPerformanceSimpleTest()
        {
            int count = 0;

            GameObject go = new("Test", typeof(RelationalComponentTesterSimple));
            RelationalComponentTesterSimple tester =
                go.GetComponent<RelationalComponentTesterSimple>();
            // Pre-warm
            tester.AssignRelationalComponents();

            TimeSpan timeout = TimeSpan.FromSeconds(10);
            Stopwatch timer = Stopwatch.StartNew();
            do
            {
                tester.AssignRelationalComponents();
                ++count;
            } while (timer.Elapsed < timeout);

            UnityEngine.Debug.Log($"Averaged {count / timeout.TotalSeconds} operations / second.");
        }
    }
}
