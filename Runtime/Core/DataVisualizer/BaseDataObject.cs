namespace UnityHelpers.Core.DataVisualizer
{
    using System;
    using System.Collections.Generic;
    using Attributes;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.Serialization;

    public abstract class BaseDataObject :
#if ODIN_INSPECTOR
        SerializedScriptableObject
#else
        ScriptableObject
#endif
    {
        public virtual string Id => _assetGuid;
        public virtual string Title => _title;
        public virtual string Description => _description;
        public virtual IReadOnlyList<string> Tags => _tags;

        [Header("Base Data")]
        [FormerlySerializedAs("initialGuid")]
        [DxReadOnly]
        [SerializeField]
        protected string _assetGuid = Guid.NewGuid().ToString();

        [FormerlySerializedAs("title")]
        [SerializeField]
        protected string _title = string.Empty;

        [FormerlySerializedAs("description")]
        [SerializeField]
        [TextArea]
        protected string _description = string.Empty;

        [FormerlySerializedAs("tags")]
        [HideInInspector]
        protected List<string> _tags = new();

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_assetGuid))
            {
                _assetGuid = Guid.NewGuid().ToString();
            }
        }
    }
}
