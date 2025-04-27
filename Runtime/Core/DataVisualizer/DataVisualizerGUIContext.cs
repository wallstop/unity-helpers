namespace WallstopStudios.UnityHelpers.Core.DataVisualizer
{
#if UNITY_EDITOR


    using UnityEditor;
#endif

    public sealed class DataVisualizerGUIContext
    {
#if UNITY_EDITOR
        public readonly SerializedObject serializedObject;
#endif

        public DataVisualizerGUIContext(
#if UNITY_EDITOR
            SerializedObject serializedObject
#endif
        )
        {
#if UNITY_EDITOR
            this.serializedObject = serializedObject;
#endif
        }
    }
}
