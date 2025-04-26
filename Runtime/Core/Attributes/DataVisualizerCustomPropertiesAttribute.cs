namespace UnityHelpers.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DataVisualizerCustomPropertiesAttribute : Attribute
    {
        public string Namespace { get; set; }
    }
}
