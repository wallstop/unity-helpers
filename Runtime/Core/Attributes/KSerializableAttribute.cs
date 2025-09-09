namespace WallstopStudios.UnityHelpers.Core.Attributes
{
    using System;

    /// <summary>
    ///     If specified on a field or property, will automatically attempt to serialize that property
    ///     on MonoBehaviors when SerializedWorld is being constructed.
    ///     If specified on a class, will automatically attempt to serialize all fields and properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
    public sealed class KSerializableAttribute : Attribute { }

    /// <summary>
    ///     For classes where KSerializableAttribute is used, specifying this on fields or properties
    ///     will ignore them for the purpose of serialization into SerializedWorld.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class KNonSerializableAttribute : Attribute { }
}
