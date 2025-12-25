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
