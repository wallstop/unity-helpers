namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    public static class ValueHelpers
    {
        public static bool IsAssigned(object value)
        {
            return ValidateAssignmentExtensions.IsValueInvalid(value);
        }
    }
}
