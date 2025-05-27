namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using UnityEngine;

    public sealed class IntDropdownAttribute : PropertyAttribute
    {
        public int[] Options { get; }

        public IntDropdownAttribute(params int[] options)
        {
            Options = options;
        }
    }
}
