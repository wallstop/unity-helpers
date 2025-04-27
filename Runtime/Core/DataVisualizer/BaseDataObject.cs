using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(assemblyName: "WallstopStudios.UnityHelpers.Editor")]

namespace WallstopStudios.UnityHelpers.Core.DataVisualizer
{
    using System;
    using System.Collections.Generic;
    using Attributes;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UIElements;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif

    public abstract class BaseDataObject :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
    {
        public virtual string Id => _assetGuid;
        public virtual string Title
        {
            get
            {
                string title = _title;
                return string.IsNullOrWhiteSpace(title) ? Id : title;
            }
        }

        public virtual string Description => _description;
        public virtual IReadOnlyList<string> Tags => _tags;

        [Header("Base Data")]
        [FormerlySerializedAs("initialGuid")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [DxReadOnly]
        [SerializeField]
        protected internal string _assetGuid = Guid.NewGuid().ToString();

        [FormerlySerializedAs("title")]
        [SerializeField]
        protected internal string _title = string.Empty;

        [FormerlySerializedAs("description")]
        [SerializeField]
        [TextArea]
        protected string _description = string.Empty;

        [FormerlySerializedAs("tags")]
        [SerializeField]
        [HideInInspector]
        protected List<string> _tags = new();

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        protected internal int _customOrder = -1;
#endif

        protected virtual void OnValidate()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                if (string.IsNullOrWhiteSpace(_assetGuid))
                {
                    _assetGuid = Guid.NewGuid().ToString();
                }
            }
        }

        public virtual VisualElement BuildGUI(DataVisualizerGUIContext context)
        {
            return null;
        }
    }
}
