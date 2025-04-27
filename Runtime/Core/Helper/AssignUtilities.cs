namespace WallstopStudios.UnityHelpers.Core.Helper
{
    public static class AssignUtilities
    {
        // Exchanges the assignTo to have the value of assignFrom.
        // Returns the old value of AssignTo.
        public static T Exchange<T>(ref T assignTo, T assignFrom)
        {
            T oldValue = assignTo;
            assignTo = assignFrom;
            return oldValue;
        }
    }
}
