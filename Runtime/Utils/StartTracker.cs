// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System.Collections;
    using UnityEngine;

    [DisallowMultipleComponent]
    public sealed class StartTracker : MonoBehaviour
    {
        public bool Started { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            Started = true;
        }
    }
}
