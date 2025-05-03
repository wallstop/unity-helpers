namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using UnityEngine;

    public static class RelationalComponentExtensions
    {
        public static void AssignRelationalComponents(this Component component)
        {
            component.AssignParentComponents();
            component.AssignSiblingComponents();
            component.AssignChildComponents();
        }
    }
}
