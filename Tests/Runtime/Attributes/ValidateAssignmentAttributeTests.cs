namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using Object = UnityEngine.Object;

    [TestFixture]
    public sealed class ValidateAssignmentAttributeTests
    {
        private readonly List<Object> _spawned = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.Destroy(spawned);
                    yield return null;
                }
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator AreAnyAssignmentsInvalidDetectsMissingValues()
        {
            GameObject go = new("ValidateAssignments", typeof(AssignmentComponent));
            _spawned.Add(go);
            AssignmentComponent component = go.GetComponent<AssignmentComponent>();

            Assert.IsTrue(component.AreAnyAssignmentsInvalid());

            component.requiredObject = new GameObject("Assigned");
            _spawned.Add(component.requiredObject);
            component.requiredString = "value";
            component.requiredList.Add(1);
            component.requiredCollection.Enqueue(2);
            component.requiredEnumerable = new[] { 3 };

            Assert.IsFalse(component.AreAnyAssignmentsInvalid());
            yield break;
        }

        [UnityTest]
        public IEnumerator ValidateAssignmentsLogsWarningsForMissingFields()
        {
            GameObject go = new("ValidateAssignmentsLogs", typeof(AssignmentComponent));
            _spawned.Add(go);
            AssignmentComponent component = go.GetComponent<AssignmentComponent>();

            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ValidateAssignmentsLogs\[AssignmentComponent\]\|requiredObject not found\.$"
                )
            );
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ValidateAssignmentsLogs\[AssignmentComponent\]\|requiredString not found\.$"
                )
            );
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ValidateAssignmentsLogs\[AssignmentComponent\]\|requiredList not found\.$"
                )
            );
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ValidateAssignmentsLogs\[AssignmentComponent\]\|requiredCollection not found\.$"
                )
            );
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex(
                    @"^\d+(\.\d+)?\|ValidateAssignmentsLogs\[AssignmentComponent\]\|requiredEnumerable not found\.$"
                )
            );

            component.ValidateAssignments();
            yield break;
        }
    }

    internal sealed class AssignmentComponent : MonoBehaviour
    {
        [ValidateAssignment]
        public GameObject requiredObject;

        [ValidateAssignment]
        public string requiredString;

        [ValidateAssignment]
        public List<int> requiredList = new();

        [ValidateAssignment]
        public Queue<int> requiredCollection = new();

        [ValidateAssignment]
        public IEnumerable<int> requiredEnumerable = Array.Empty<int>();
    }
}
