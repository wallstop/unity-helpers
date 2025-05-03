namespace WallstopStudios.UnityHelpers.Core.Helper.Partials
{
    using Extension;
    using UnityEngine;

    public static partial class Helpers
    {
        public static void LogNotAssigned(this Object component, string name)
        {
            component.LogWarn($"{name} not found.");
        }
    }
}
