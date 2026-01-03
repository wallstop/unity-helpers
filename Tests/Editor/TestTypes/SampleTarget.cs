// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class SampleTarget : ScriptableObject
    {
        [WButton(drawOrder: 2)]
        public void NoParamsVoid() { }

        [WButton(drawOrder: 5)]
        public async Task<int> TaskMethodAsync(int value)
        {
            await Task.Delay(10);
            return value;
        }

        [WButton(drawOrder: -2)]
        public IEnumerator<object> EnumeratorMethod()
        {
            yield return null;
        }

        [WButton]
        public void MethodWithCancellation(CancellationToken cancellationToken) { }

        [WButton]
        public void MethodWithDefaults(int count = 7, string label = "hello") { }

        [WButton(colorKey: "Critical")]
        public void PriorityMethod() { }

        [WButton(drawOrder: -3, groupName: "Utilities")]
        public void NamedGroupMethod() { }
    }
}
