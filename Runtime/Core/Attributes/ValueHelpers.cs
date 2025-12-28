// MIT License - Copyright (c) 2023 Eli Pinkerton
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    public static class ValueHelpers
    {
        public static bool IsAssigned(object value)
        {
            return !ValidateAssignmentExtensions.IsValueInvalid(value);
        }
    }
}
