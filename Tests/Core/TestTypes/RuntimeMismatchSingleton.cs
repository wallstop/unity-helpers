namespace WallstopStudios.UnityHelpers.Tests.Core.TestTypes
{
    using WallstopStudios.UnityHelpers.Utils;

    public sealed class RuntimeMismatchSingleton : RuntimeSingleton<RuntimeMismatchSingleton>
    {
        public static int AwakeCount;

        protected override void Awake()
        {
            base.Awake();
            AwakeCount++;
        }

        public static void ClearForTests()
        {
            AwakeCount = 0;
            if (HasInstance)
            {
                DestroyImmediate(_instance.gameObject);
            }
            _instance = null;
        }
    }
}
