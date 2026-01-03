// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Tests.Editor.TestTypes
{
    using System.Collections;
    using System.Threading.Tasks;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;

    public sealed class InvocationTarget : ScriptableObject
    {
        [WButton]
        public async Task<string> AsyncTaskButton()
        {
            await Task.Delay(50);
            return "Task Complete";
        }

        [WButton]
        public IEnumerator EnumeratorButton()
        {
            yield return null;
            yield return null;
        }
    }
}
