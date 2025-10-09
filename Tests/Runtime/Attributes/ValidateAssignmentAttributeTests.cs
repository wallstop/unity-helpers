namespace WallstopStudios.UnityHelpers.Tests.Attributes
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    [TestFixture]
    public sealed class ValidateAssignmentAttributeTests : CommonTestBase
    {
        [UnityTest]
        public IEnumerator AreAnyAssignmentsInvalidDetectsMissingValues()
        {
            GameObject go = Track(
                new GameObject("ValidateAssignments", typeof(AssignmentComponent))
            );
            AssignmentComponent component = go.GetComponent<AssignmentComponent>();

            Assert.IsTrue(component.AreAnyAssignmentsInvalid());

            component.requiredObject = Track(new GameObject("Assigned"));
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
            GameObject go = Track(
                new GameObject("ValidateAssignmentsLogs", typeof(AssignmentComponent))
            );
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
