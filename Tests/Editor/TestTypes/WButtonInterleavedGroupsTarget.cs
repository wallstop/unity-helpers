// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with interleaved group declarations to verify proper grouping.
    /// Groups are declared in order: Alpha, Beta, Alpha, Gamma, Beta
    /// Expected order: Alpha (first seen), Beta (second seen), Gamma (third seen)
    /// </summary>
    public sealed class WButtonInterleavedGroupsTarget : ScriptableObject
    {
        [WButton("Alpha 1", drawOrder: 0, groupName: "Alpha")]
        public void Alpha1() { }

        [WButton("Beta 1", drawOrder: 0, groupName: "Beta")]
        public void Beta1() { }

        [WButton("Alpha 2", drawOrder: 0, groupName: "Alpha")]
        public void Alpha2() { }

        [WButton("Gamma 1", drawOrder: 0, groupName: "Gamma")]
        public void Gamma1() { }

        [WButton("Beta 2", drawOrder: 0, groupName: "Beta")]
        public void Beta2() { }

        [WButton("Alpha 3", drawOrder: 0, groupName: "Alpha")]
        public void Alpha3() { }
    }
}
