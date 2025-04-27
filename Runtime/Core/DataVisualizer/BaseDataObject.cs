using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(assemblyName: "WallstopStudios.UnityHelpers.Editor")]

namespace WallstopStudios.UnityHelpers.Core.DataVisualizer
{
    using System;
    using System.Collections.Generic;
    using Attributes;
    using Sirenix.OdinInspector;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UIElements;

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
        [DxReadOnly]
        [SerializeField]
        protected string _assetGuid = Guid.NewGuid().ToString();

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
            if (string.IsNullOrWhiteSpace(_assetGuid))
            {
                _assetGuid = Guid.NewGuid().ToString();
            }
        }

#if UNITY_EDITOR
        public virtual VisualElement BuildGUI(SerializedObject scriptableObject)
        {
            return null;
        }
#endif
    }
}
