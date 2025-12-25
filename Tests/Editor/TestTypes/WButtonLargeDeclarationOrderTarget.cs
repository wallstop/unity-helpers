namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    /// <summary>
    /// Test target with many buttons to verify declaration order is stable with larger counts.
    /// Buttons are declared in a specific order that alphabetical sorting would scramble.
    /// </summary>
    public sealed class WButtonLargeDeclarationOrderTarget : ScriptableObject
    {
        [WButton("Ninth", drawOrder: 0)]
        public void Method09() { }

        [WButton("First", drawOrder: 0)]
        public void Method01() { }

        [WButton("Fifth", drawOrder: 0)]
        public void Method05() { }

        [WButton("Third", drawOrder: 0)]
        public void Method03() { }

        [WButton("Seventh", drawOrder: 0)]
        public void Method07() { }

        [WButton("Second", drawOrder: 0)]
        public void Method02() { }

        [WButton("Tenth", drawOrder: 0)]
        public void Method10() { }

        [WButton("Fourth", drawOrder: 0)]
        public void Method04() { }

        [WButton("Eighth", drawOrder: 0)]
        public void Method08() { }

        [WButton("Sixth", drawOrder: 0)]
        public void Method06() { }
    }
}
