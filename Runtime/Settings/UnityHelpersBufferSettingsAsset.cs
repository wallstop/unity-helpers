namespace WallstopStudios.UnityHelpers.Settings
{
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    /// <summary>
    /// ScriptableObject that stores default wait-instruction buffer settings. Serialized under Resources so the runtime can apply the defaults automatically.
    /// </summary>
    public sealed class UnityHelpersBufferSettingsAsset : ScriptableObject
    {
        /// <summary>
        /// Resource path (used with Resources.Load) where the settings asset is stored.
        /// </summary>
        public const string ResourcePath =
            "WallstopStudios/UnityHelpers/UnityHelpersBufferSettings";

        /// <summary>
        /// AssetDatabase path used by the editor to create/manage the asset.
        /// </summary>
        public const string AssetPath =
            "Assets/Resources/WallstopStudios/UnityHelpers/UnityHelpersBufferSettings.asset";

        internal const string ApplyOnLoadPropertyName = nameof(applyOnLoad);
        internal const string QuantizationStepSecondsPropertyName = nameof(
            waitInstructionQuantizationStepSeconds
        );
        internal const string MaxDistinctEntriesPropertyName = nameof(
            waitInstructionMaxDistinctEntries
        );
        internal const string UseLruEvictionPropertyName = nameof(waitInstructionUseLruEviction);

        [SerializeField]
        private bool applyOnLoad = true;

        [SerializeField]
        [Min(0f)]
        private float waitInstructionQuantizationStepSeconds = 0f;

        [SerializeField]
        [Min(0)]
        private int waitInstructionMaxDistinctEntries =
            Buffers.WaitInstructionDefaultMaxDistinctEntries;

        [SerializeField]
        private bool waitInstructionUseLruEviction;

        /// <summary>
        /// Gets whether the defaults should be applied automatically on domain/runtime load.
        /// </summary>
        public bool ApplyOnLoad => applyOnLoad;

        /// <summary>
        /// Gets the sanitized quantization step (0 disables quantization).
        /// </summary>
        public float QuantizationStepSeconds =>
            SanitizeQuantization(waitInstructionQuantizationStepSeconds);

        /// <summary>
        /// Gets the sanitized distinct entry limit (0 = unbounded).
        /// </summary>
        public int MaxDistinctEntries =>
            SanitizeMaxDistinctEntries(waitInstructionMaxDistinctEntries);

        /// <summary>
        /// Gets whether LRU eviction should be enabled when the cache hits the distinct entry limit.
        /// </summary>
        public bool UseLruEviction => waitInstructionUseLruEviction;

        /// <summary>
        /// Applies the stored defaults to the Buffers wait-instruction caches.
        /// </summary>
        public void ApplyToBuffers()
        {
            Buffers.WaitInstructionQuantizationStepSeconds = QuantizationStepSeconds;
            Buffers.WaitInstructionMaxDistinctEntries = MaxDistinctEntries;
            Buffers.WaitInstructionUseLruEviction = waitInstructionUseLruEviction;
        }

        /// <summary>
        /// Copies the current Buffers configuration into this asset. Useful when seeding defaults from code.
        /// </summary>
        public void SyncFromRuntime()
        {
            waitInstructionQuantizationStepSeconds = Buffers.WaitInstructionQuantizationStepSeconds;
            waitInstructionMaxDistinctEntries = Buffers.WaitInstructionMaxDistinctEntries;
            waitInstructionUseLruEviction = Buffers.WaitInstructionUseLruEviction;
        }

        private static float SanitizeQuantization(float step)
        {
            if (float.IsNaN(step) || float.IsInfinity(step) || step <= 0f)
            {
                return 0f;
            }

            return step;
        }

        private static int SanitizeMaxDistinctEntries(int value)
        {
            return value < 0 ? 0 : value;
        }
    }
}
