// MIT License - Copyright (c) 2025 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    [Serializable]
    public sealed class SerializedStringComparer : IEqualityComparer<string>
    {
        public enum StringCompareMode
        {
            Ordinal = 0,
            OrdinalIgnoreCase = 1,
            CurrentCulture = 2,
            CurrentCultureIgnoreCase = 3,
            InvariantCulture = 4,
            InvariantCultureIgnoreCase = 5,
        }

        public StringCompareMode compareMode = StringCompareMode.Ordinal;

        public SerializedStringComparer() { }

        public SerializedStringComparer(StringCompareMode compareMode)
        {
            this.compareMode = compareMode;
        }

        public bool Equals(string x, string y)
        {
            switch (compareMode)
            {
                case StringCompareMode.Ordinal:
                {
                    return StringComparer.Ordinal.Equals(x, y);
                }
                case StringCompareMode.OrdinalIgnoreCase:
                {
                    return StringComparer.OrdinalIgnoreCase.Equals(x, y);
                }
                case StringCompareMode.CurrentCulture:
                {
                    return StringComparer.CurrentCulture.Equals(x, y);
                }
                case StringCompareMode.CurrentCultureIgnoreCase:
                {
                    return StringComparer.CurrentCultureIgnoreCase.Equals(x, y);
                }
                case StringCompareMode.InvariantCulture:
                {
                    return StringComparer.InvariantCulture.Equals(x, y);
                }
                case StringCompareMode.InvariantCultureIgnoreCase:
                {
                    return StringComparer.InvariantCultureIgnoreCase.Equals(x, y);
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(compareMode),
                        (int)compareMode,
                        typeof(StringCompareMode)
                    );
                }
            }
        }

        public int GetHashCode(string obj)
        {
            switch (compareMode)
            {
                case StringCompareMode.Ordinal:
                {
                    return StringComparer.Ordinal.GetHashCode(obj);
                }
                case StringCompareMode.OrdinalIgnoreCase:
                {
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
                }
                case StringCompareMode.CurrentCulture:
                {
                    return StringComparer.CurrentCulture.GetHashCode(obj);
                }
                case StringCompareMode.CurrentCultureIgnoreCase:
                {
                    return StringComparer.CurrentCultureIgnoreCase.GetHashCode(obj);
                }
                case StringCompareMode.InvariantCulture:
                {
                    return StringComparer.InvariantCulture.GetHashCode(obj);
                }
                case StringCompareMode.InvariantCultureIgnoreCase:
                {
                    return StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj);
                }
                default:
                {
                    throw new InvalidEnumArgumentException(
                        nameof(compareMode),
                        (int)compareMode,
                        typeof(StringCompareMode)
                    );
                }
            }
        }
    }
}
