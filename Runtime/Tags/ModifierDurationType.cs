namespace WallstopStudios.UnityHelpers.Tags
{
    using System;

    public enum ModifierDurationType
    {
        [Obsolete("Please use a valid value.")]
        None = 0,
        Instant = 1,
        Duration = 2,
        Infinite = 3,
    }
}
