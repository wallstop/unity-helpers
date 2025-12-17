namespace WallstopStudios.UnityHelpers.Tests.CustomDrawers.TestTypes
{
#if UNITY_EDITOR
    using System;

    internal static class StringOptionsProvider
    {
        internal static string[] GetOptions()
        {
            return new[] { "Static1", "Static2", "Static3" };
        }
    }

    internal static class WValueDropDownSource
    {
        internal static float[] GetFloatValues()
        {
            return new[] { 1f, 2.5f, 5f };
        }

        internal static double[] GetDoubleValues()
        {
            return new[] { 2d, 4.5d, 5.25d };
        }
    }

    internal static class WValueDropDownEmptySource
    {
        internal static int[] GetEmptyOptions()
        {
            return Array.Empty<int>();
        }
    }

    internal static class IntDropDownSource
    {
        internal static int[] GetStaticOptions()
        {
            return new[] { 100, 200, 300 };
        }
    }

    internal static class IntDropDownEmptySource
    {
        internal static int[] GetEmptyOptions()
        {
            return Array.Empty<int>();
        }
    }

    internal static class IntDropDownLargeSource
    {
        internal static int[] GetLargeOptions()
        {
            // Returns more than the default page size (25) to trigger popup path
            int[] options = new int[50];
            for (int i = 0; i < 50; i++)
            {
                options[i] = (i + 1) * 10;
            }
            return options;
        }
    }
#endif
}
