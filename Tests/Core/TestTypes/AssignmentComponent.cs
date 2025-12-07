using System;
using System.Collections.Generic;
using UnityEngine;
using WallstopStudios.UnityHelpers.Core.Attributes;

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    public sealed class AssignmentComponent : MonoBehaviour
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
