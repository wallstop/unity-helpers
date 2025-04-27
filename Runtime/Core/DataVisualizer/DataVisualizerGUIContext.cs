namespace WallstopStudios.UnityHelpers.Core.DataVisualizer
{
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public sealed class DataVisualizerGUIContext
    {
#if UNITY_EDITOR
        public readonly SerializedObject serializedObject;

        internal DataVisualizerGUIContext(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
        }
#else
        internal DataVisualizerGUIContext() { }
#endif
    }
}
