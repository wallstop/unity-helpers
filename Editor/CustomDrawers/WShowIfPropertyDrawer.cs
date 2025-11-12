namespace WallstopStudios.UnityHelpers.Editor.CustomDrawers
{
#if UNITY_EDITOR
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using Extensions;
    using UnityEditor;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Core.Attributes;
    using WallstopStudios.UnityHelpers.Core.Extension;
    using WallstopStudios.UnityHelpers.Core.Helper;

    [CustomPropertyDrawer(typeof(WShowIfAttribute))]
    public sealed class WShowIfPropertyDrawer : PropertyDrawer
    {
        private static readonly Dictionary<
            Type,
            Dictionary<string, Func<object, object>>
        > CachedAccessors = new();
        private static readonly object[] EmptyParameters = Array.Empty<object>();
        private WShowIfAttribute _overrideAttribute;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return !ShouldShow(property) ? 0f : EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        internal void InitializeForTesting(WShowIfAttribute attributeOverride)
        {
            _overrideAttribute = attributeOverride;
        }

        private WShowIfAttribute ResolveAttribute()
        {
            if (_overrideAttribute != null)
            {
                return _overrideAttribute;
            }

            return attribute as WShowIfAttribute;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            WShowIfAttribute showIf = ResolveAttribute();
            if (showIf == null)
            {
                return true;
            }

            if (
                TryGetConditionProperty(
                    property,
                    showIf.conditionField,
                    out SerializedProperty conditionProperty
                )
            )
            {
                if (TryEvaluateCondition(conditionProperty, showIf, out bool serializedResult))
                {
                    return serializedResult;
                }

                return true;
            }

            object enclosingObject = property.GetEnclosingObject(out _);
            if (enclosingObject == null)
            {
                return true;
            }

            Type ownerType = enclosingObject.GetType();
            Func<object, object> accessor = GetAccessor(ownerType, showIf.conditionField);
            object fieldValue = accessor(enclosingObject);
            return !TryEvaluateCondition(fieldValue, showIf, out bool reflectedResult)
                || reflectedResult;
        }

        private static bool TryEvaluateCondition(
            SerializedProperty conditionProperty,
            WShowIfAttribute showIf,
            out bool shouldShow
        )
        {
            if (conditionProperty == null)
            {
                shouldShow = true;
                return false;
            }

            object conditionValue;
            if (conditionProperty.propertyType == SerializedPropertyType.Boolean)
            {
                conditionValue = conditionProperty.boolValue;
            }
            else
            {
                conditionValue = conditionProperty.GetTargetObjectWithField(out _);
            }

            return TryEvaluateCondition(conditionValue, showIf, out shouldShow);
        }

        private static bool TryEvaluateCondition(
            object conditionValue,
            WShowIfAttribute showIf,
            out bool shouldShow
        )
        {
            bool? evaluation = EvaluateCondition(conditionValue, showIf);
            if (!evaluation.HasValue)
            {
                shouldShow = true;
                return false;
            }

            bool matched = evaluation.Value;
            shouldShow = showIf.inverse ? !matched : matched;
            return true;
        }

        private static bool ValuesEqual(object actual, object expected)
        {
            if (ReferenceEquals(actual, expected))
            {
                return true;
            }

            if (actual == null || expected == null)
            {
                return false;
            }

            if (actual.Equals(expected))
            {
                return true;
            }

            Type actualType = actual.GetType();
            Type expectedType = expected.GetType();

            try
            {
                if (actualType.IsEnum || expectedType.IsEnum)
                {
                    long actualValue = Convert.ToInt64(actual);
                    long expectedValue = Convert.ToInt64(expected);
                    return actualValue == expectedValue;
                }
            }
            catch
            {
                return false;
            }

            if (actual is not IConvertible || expected is not IConvertible)
            {
                return false;
            }

            try
            {
                double actualValue = Convert.ToDouble(actual);
                double expectedValue = Convert.ToDouble(expected);
                return Math.Abs(actualValue - expectedValue) < double.Epsilon;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetConditionProperty(
            SerializedProperty property,
            string conditionField,
            out SerializedProperty conditionProperty
        )
        {
            SerializedObject serializedObject = property.serializedObject;
            conditionProperty = serializedObject.FindProperty(conditionField);
            if (conditionProperty != null)
            {
                return true;
            }

            string propertyPath = property.propertyPath;
            if (!string.IsNullOrEmpty(propertyPath))
            {
                int separatorIndex = propertyPath.LastIndexOf('.');
                string siblingPath =
                    separatorIndex == -1
                        ? conditionField
                        : propertyPath.Substring(0, separatorIndex + 1) + conditionField;
                conditionProperty = serializedObject.FindProperty(siblingPath);
                if (conditionProperty != null)
                {
                    return true;
                }
            }

            conditionProperty = null;
            return false;
        }

        private static Func<object, object> GetAccessor(Type ownerType, string memberPath)
        {
            Dictionary<string, Func<object, object>> cachedForType = CachedAccessors.GetOrAdd(
                ownerType
            );
            if (!cachedForType.TryGetValue(memberPath, out Func<object, object> accessor))
            {
                accessor = BuildAccessor(ownerType, memberPath);
                cachedForType[memberPath] = accessor;
            }

            return accessor;
        }

        private static Func<object, object> BuildAccessor(Type ownerType, string memberPath)
        {
            if (string.IsNullOrEmpty(memberPath))
            {
                return static _ => null;
            }

            List<MemberPathSegment> segments = ParseMemberPath(memberPath);
            if (segments.Count == 0)
            {
                return static _ => null;
            }

            List<Func<object, object>> steps = new(segments.Count);
            Type currentType = ownerType;

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex += 1)
            {
                MemberPathSegment segment = segments[segmentIndex];
                MemberAccessor memberAccessor = ResolveMemberAccessor(
                    currentType,
                    segment.MemberName
                );
                if (!memberAccessor.IsValid)
                {
                    Debug.LogError(
                        $"Failed to resolve conditional member '{segment.MemberName}' on {currentType.FullName} while evaluating '{memberPath}'."
                    );
                    return static _ => null;
                }

                steps.Add(memberAccessor.Getter);
                currentType =
                    memberAccessor.ValueType != null ? memberAccessor.ValueType : typeof(object);

                if (segment.Indices.Length == 0)
                {
                    continue;
                }

                for (
                    int indexPosition = 0;
                    indexPosition < segment.Indices.Length;
                    indexPosition += 1
                )
                {
                    IndexAccessor indexAccessor = CreateIndexAccessor(
                        currentType,
                        segment.Indices[indexPosition]
                    );
                    if (!indexAccessor.IsValid)
                    {
                        Debug.LogError(
                            $"Failed to resolve index accessor for '{segment.MemberName}' on {currentType.FullName} while evaluating '{memberPath}'."
                        );
                        return static _ => null;
                    }

                    steps.Add(indexAccessor.Getter);
                    currentType =
                        indexAccessor.ElementType != null
                            ? indexAccessor.ElementType
                            : typeof(object);
                }
            }

            return instance =>
            {
                object current = instance;
                for (int stepIndex = 0; stepIndex < steps.Count; stepIndex += 1)
                {
                    if (current == null)
                    {
                        return null;
                    }

                    current = steps[stepIndex](current);
                }

                return current;
            };
        }

        private static MemberAccessor ResolveMemberAccessor(Type type, string memberName)
        {
            BindingFlags flags =
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.FlattenHierarchy;

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
            {
                return new MemberAccessor(ReflectionHelpers.GetFieldGetter(field), field.FieldType);
            }

            PropertyInfo propertyInfo = type.GetProperty(memberName, flags);
            if (propertyInfo != null && propertyInfo.CanRead)
            {
                return new MemberAccessor(
                    ReflectionHelpers.GetPropertyGetter(propertyInfo),
                    propertyInfo.PropertyType
                );
            }

            MethodInfo methodInfo = type.GetMethod(memberName, flags, null, Type.EmptyTypes, null);
            if (methodInfo != null)
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    Debug.LogWarning(
                        $"WShowIf member '{memberName}' on {type.FullName} returns void and cannot be used as a condition."
                    );
                    return MemberAccessor.Invalid;
                }

                Func<object, object[], object> invoker = ReflectionHelpers.GetMethodInvoker(
                    methodInfo
                );
                return new MemberAccessor(
                    instance => invoker(instance, EmptyParameters),
                    methodInfo.ReturnType
                );
            }

            return MemberAccessor.Invalid;
        }

        private static IndexAccessor CreateIndexAccessor(Type collectionType, int index)
        {
            if (collectionType == null)
            {
                return IndexAccessor.Invalid;
            }

            if (collectionType.IsArray)
            {
                Type elementType = collectionType.GetElementType() ?? typeof(object);
                return new IndexAccessor(
                    value =>
                    {
                        Array array = value as Array;
                        if (array == null || index < 0 || index >= array.Length)
                        {
                            return null;
                        }

                        return array.GetValue(index);
                    },
                    elementType
                );
            }

            if (typeof(IList).IsAssignableFrom(collectionType))
            {
                Type elementType = ResolveListElementType(collectionType);
                return new IndexAccessor(
                    value =>
                    {
                        IList list = value as IList;
                        if (list == null || index < 0 || index >= list.Count)
                        {
                            return null;
                        }

                        return list[index];
                    },
                    elementType
                );
            }

            Type readOnlyListInterface = GetGenericInterface(
                collectionType,
                typeof(IReadOnlyList<>)
            );
            if (readOnlyListInterface != null)
            {
                Type elementType = readOnlyListInterface.GetGenericArguments()[0];
                return new IndexAccessor(
                    value =>
                    {
                        if (value == null)
                        {
                            return null;
                        }

                        PropertyInfo indexer = readOnlyListInterface.GetProperty("Item");
                        if (indexer == null)
                        {
                            return null;
                        }

                        try
                        {
                            return indexer.GetValue(value, new object[] { index });
                        }
                        catch
                        {
                            return null;
                        }
                    },
                    elementType
                );
            }

            if (typeof(IEnumerable).IsAssignableFrom(collectionType))
            {
                Type elementType = ResolveEnumerableElementType(collectionType);
                return new IndexAccessor(
                    value =>
                    {
                        IEnumerable enumerable = value as IEnumerable;
                        if (enumerable == null)
                        {
                            return null;
                        }

                        int current = 0;
                        foreach (object item in enumerable)
                        {
                            if (current == index)
                            {
                                return item;
                            }

                            current += 1;
                        }

                        return null;
                    },
                    elementType
                );
            }

            return IndexAccessor.Invalid;
        }

        private static bool? EvaluateCondition(object conditionValue, WShowIfAttribute attribute)
        {
            WShowIfComparison comparison = attribute.comparison;
#pragma warning disable CS0618 // Type or member is obsolete
            if (comparison == WShowIfComparison.Unknown)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                comparison = WShowIfComparison.Equal;
            }

            switch (comparison)
            {
                case WShowIfComparison.IsNull:
                    return IsNull(conditionValue);
                case WShowIfComparison.IsNotNull:
                    return !IsNull(conditionValue);
                case WShowIfComparison.IsNullOrEmpty:
                    return IsNullOrEmpty(conditionValue);
                case WShowIfComparison.IsNotNullOrEmpty:
                    return !IsNullOrEmpty(conditionValue);
                default:
                    break;
            }

            object[] expectedValues = attribute.expectedValues;
            if (conditionValue is bool boolean)
            {
                return EvaluateBooleanCondition(boolean, comparison, expectedValues);
            }
            if (expectedValues == null || expectedValues.Length == 0)
            {
                return null;
            }

            switch (comparison)
            {
                case WShowIfComparison.Equal:
                    return MatchesAny(conditionValue, expectedValues);
                case WShowIfComparison.NotEqual:
                    return !MatchesAny(conditionValue, expectedValues);
                case WShowIfComparison.GreaterThan:
                case WShowIfComparison.GreaterThanOrEqual:
                case WShowIfComparison.LessThan:
                case WShowIfComparison.LessThanOrEqual:
                    object referenceValue = expectedValues[0];
                    bool? relational = EvaluateRelationalComparison(
                        conditionValue,
                        referenceValue,
                        comparison
                    );
                    return relational;
                default:
                    return MatchesAny(conditionValue, expectedValues);
            }
        }

        private static bool? EvaluateBooleanCondition(
            bool value,
            WShowIfComparison comparison,
            object[] expectedValues
        )
        {
            if (expectedValues is { Length: > 0 })
            {
                bool matches = MatchesAny(value, expectedValues);
                if (comparison == WShowIfComparison.NotEqual)
                {
                    return !matches;
                }

                return matches;
            }

            if (comparison == WShowIfComparison.NotEqual)
            {
                return !value;
            }

            return value;
        }

        private static bool MatchesAny(object conditionValue, object[] expectedValues)
        {
            for (int index = 0; index < expectedValues.Length; index += 1)
            {
                if (ValuesEqual(conditionValue, expectedValues[index]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool? EvaluateRelationalComparison(
            object actual,
            object expected,
            WShowIfComparison comparison
        )
        {
            if (!TryCompare(actual, expected, out int compareResult))
            {
                return null;
            }

            switch (comparison)
            {
                case WShowIfComparison.GreaterThan:
                    return compareResult > 0;
                case WShowIfComparison.GreaterThanOrEqual:
                    return compareResult >= 0;
                case WShowIfComparison.LessThan:
                    return compareResult < 0;
                case WShowIfComparison.LessThanOrEqual:
                    return compareResult <= 0;
                default:
                    return null;
            }
        }

        private static bool TryCompare(object actual, object expected, out int comparisonResult)
        {
            comparisonResult = 0;
            if (actual == null || expected == null)
            {
                return false;
            }

            IComparable comparable = actual as IComparable;
            if (comparable != null)
            {
                object converted = ConvertValue(
                    actual.GetType(),
                    expected,
                    out bool conversionSucceeded
                );
                if (conversionSucceeded)
                {
                    try
                    {
                        comparisonResult = comparable.CompareTo(converted);
                        return true;
                    }
                    catch { }
                }
            }

            if (TryGenericComparableCompare(actual, expected, out comparisonResult, false))
            {
                return true;
            }

            IComparable expectedComparable = expected as IComparable;
            if (expectedComparable != null)
            {
                object converted = ConvertValue(
                    expected.GetType(),
                    actual,
                    out bool conversionSucceeded
                );
                if (conversionSucceeded)
                {
                    try
                    {
                        comparisonResult = -expectedComparable.CompareTo(converted);
                        return true;
                    }
                    catch { }
                }
            }

            if (TryGenericComparableCompare(expected, actual, out comparisonResult, true))
            {
                return true;
            }

            if (
                TryConvertToDouble(actual, out double actualDouble)
                && TryConvertToDouble(expected, out double expectedDouble)
            )
            {
                comparisonResult = actualDouble.CompareTo(expectedDouble);
                return true;
            }

            return false;
        }

        private static object ConvertValue(Type targetType, object value, out bool success)
        {
            success = true;
            if (value == null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                {
                    success = false;
                }

                return null;
            }

            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }

            try
            {
                if (targetType.IsEnum)
                {
                    Type underlyingType = Enum.GetUnderlyingType(targetType);
                    object numericValue = Convert.ChangeType(value, underlyingType);
                    return Enum.ToObject(targetType, numericValue);
                }

                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                success = false;
                return null;
            }
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            result = 0d;
            if (value == null)
            {
                return false;
            }

            try
            {
                result = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGenericComparableCompare(
            object lhs,
            object rhs,
            out int comparisonResult,
            bool invert
        )
        {
            comparisonResult = 0;
            if (lhs == null)
            {
                return false;
            }

            Type lhsType = lhs.GetType();
            Type[] interfaces = lhsType.GetInterfaces();
            for (int index = 0; index < interfaces.Length; index += 1)
            {
                Type iface = interfaces[index];
                if (
                    !iface.IsGenericType
                    || iface.GetGenericTypeDefinition() != typeof(IComparable<>)
                )
                {
                    continue;
                }

                Type genericArgument = iface.GetGenericArguments()[0];
                object converted = ConvertValue(genericArgument, rhs, out bool success);
                if (!success)
                {
                    continue;
                }

                MethodInfo compareTo = iface.GetMethod("CompareTo", new[] { genericArgument });
                if (compareTo == null)
                {
                    continue;
                }

                try
                {
                    object compareResult = compareTo.Invoke(lhs, new[] { converted });
                    comparisonResult = Convert.ToInt32(compareResult);
                    if (invert)
                    {
                        comparisonResult = -comparisonResult;
                    }

                    return true;
                }
                catch { }
            }

            return false;
        }

        private static bool IsNull(object value)
        {
            if (value == null)
            {
                return true;
            }

            UnityEngine.Object unityObject = value as UnityEngine.Object;
            if (unityObject != null)
            {
                return unityObject == null;
            }

            return false;
        }

        private static bool IsNullOrEmpty(object value)
        {
            if (IsNull(value))
            {
                return true;
            }

            string stringValue = value as string;
            if (stringValue != null)
            {
                return stringValue.Length == 0;
            }

            ICollection collection = value as ICollection;
            if (collection != null)
            {
                return collection.Count == 0;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable != null)
            {
                foreach (object _ in enumerable)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private static List<MemberPathSegment> ParseMemberPath(string memberPath)
        {
            string[] rawSegments = memberPath.Split('.');
            List<MemberPathSegment> segments = new(rawSegments.Length);
            for (int index = 0; index < rawSegments.Length; index += 1)
            {
                string raw = rawSegments[index];
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                string name = raw;
                List<int> indices = new();

                int bracket = raw.IndexOf('[');
                if (bracket >= 0)
                {
                    name = raw.Substring(0, bracket);
                    int cursor = bracket;
                    while (cursor < raw.Length && (cursor = raw.IndexOf('[', cursor)) != -1)
                    {
                        int endBracket = raw.IndexOf(']', cursor + 1);
                        if (endBracket == -1)
                        {
                            break;
                        }

                        string slice = raw.Substring(cursor + 1, endBracket - cursor - 1);
                        if (int.TryParse(slice, out int parsedIndex))
                        {
                            indices.Add(parsedIndex);
                        }
                        cursor = endBracket + 1;
                    }
                }

                MemberPathSegment segment = new(
                    name,
                    indices.Count > 0 ? indices.ToArray() : Array.Empty<int>()
                );
                segments.Add(segment);
            }

            return segments;
        }

        private static Type ResolveListElementType(Type type)
        {
            if (type.IsGenericType)
            {
                Type[] args = type.GetGenericArguments();
                if (args.Length == 1)
                {
                    return args[0];
                }
            }

            return typeof(object);
        }

        private static Type ResolveEnumerableElementType(Type type)
        {
            Type genericInterface = GetGenericInterface(type, typeof(IEnumerable<>));
            if (genericInterface != null)
            {
                Type[] args = genericInterface.GetGenericArguments();
                if (args.Length == 1)
                {
                    return args[0];
                }
            }

            return typeof(object);
        }

        private static Type GetGenericInterface(Type type, Type interfaceTemplate)
        {
            if (
                type.IsInterface
                && type.IsGenericType
                && type.GetGenericTypeDefinition() == interfaceTemplate
            )
            {
                return type;
            }

            Type[] interfaces = type.GetInterfaces();
            for (int index = 0; index < interfaces.Length; index += 1)
            {
                Type candidate = interfaces[index];
                if (
                    candidate.IsGenericType
                    && candidate.GetGenericTypeDefinition() == interfaceTemplate
                )
                {
                    return candidate;
                }
            }

            return null;
        }

        private readonly struct MemberAccessor
        {
            public static readonly MemberAccessor Invalid = new(null, null);

            public MemberAccessor(Func<object, object> getter, Type valueType)
            {
                Getter = getter;
                ValueType = valueType;
            }

            public Func<object, object> Getter { get; }

            public Type ValueType { get; }

            public bool IsValid => Getter != null;
        }

        private readonly struct IndexAccessor
        {
            public static readonly IndexAccessor Invalid = new(null, null);

            public IndexAccessor(Func<object, object> getter, Type elementType)
            {
                Getter = getter;
                ElementType = elementType;
            }

            public Func<object, object> Getter { get; }

            public Type ElementType { get; }

            public bool IsValid => Getter != null;
        }

        private readonly struct MemberPathSegment
        {
            public MemberPathSegment(string memberName, int[] indices)
            {
                MemberName = memberName;
                Indices = indices;
            }

            public string MemberName { get; }

            public int[] Indices { get; }
        }
    }
#endif
}
