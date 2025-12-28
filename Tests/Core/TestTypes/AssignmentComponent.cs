// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

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
