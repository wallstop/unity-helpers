namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class EnumDisplayNameAttribute : Attribute
    {
        public string DisplayName { get; }

        public EnumDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
