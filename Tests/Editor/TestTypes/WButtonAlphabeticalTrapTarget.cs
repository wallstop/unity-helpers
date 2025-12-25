namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target designed to catch alphabetical sorting bugs.
    /// Method names and display names are in reverse alphabetical order.
    /// If sorting is alphabetical, order would be: Alpha, Beta, Charlie, Delta
    /// If sorting is by declaration order, order would be: Delta, Charlie, Beta, Alpha
    /// </summary>
    public sealed class WButtonAlphabeticalTrapTarget : ScriptableObject
    {
        [WButton("Delta", drawOrder: 0)]
        public void ZZZDeltaMethod() { }

        [WButton("Charlie", drawOrder: 0)]
        public void YYYCharlieMethod() { }

        [WButton("Beta", drawOrder: 0)]
        public void XXXBetaMethod() { }

        [WButton("Alpha", drawOrder: 0)]
        public void WWWAlphaMethod() { }
    }
}
