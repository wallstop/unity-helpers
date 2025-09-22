namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ScriptableSingletonPathAttribute : Attribute
    {
        public readonly string resourcesPath;

        public ScriptableSingletonPathAttribute(string resourcesPath)
        {
            this.resourcesPath = resourcesPath ?? string.Empty;
        }
    }
}
