namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using Serialization;
    using UnityEngine;

    public static class StringExtensions
    {
        private static readonly ThreadLocal<StringBuilder> StringBuilderCache = new(() =>
            new StringBuilder()
        );

        private static readonly HashSet<char> PascalCaseSeparators = new()
        {
            '_',
            ' ',
            '\r',
            '\n',
            '\t',
            '.',
            '\'',
            '"',
        };

        public static string Center(this string input, int length)
        {
            if (input == null || length <= input.Length)
            {
                return input;
            }

            return input.PadLeft((length - input.Length) / 2 + input.Length).PadRight(length);
        }

        public static byte[] GetBytes(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Array.Empty<byte>();
            }
            return Encoding.Default.GetBytes(input);
        }

        public static string GetString(this byte[] bytes)
        {
            return Encoding.Default.GetString(bytes);
        }

        public static string ToJson<T>(this T value)
        {
            return Serializer.JsonStringify(value);
        }

        public static int LevenshteinDistance(this string source1, string source2)
        {
            source1 ??= string.Empty;
            source2 ??= string.Empty;

            int source1Length = source1.Length;
            int source2Length = source2.Length;

            int[][] matrix = new int[source1Length + 1][];
            for (int index = 0; index < source1Length + 1; index++)
            {
                matrix[index] = new int[source2Length + 1];
            }

            if (source1Length == 0)
            {
                return source2Length;
            }

            if (source2Length == 0)
            {
                return source1Length;
            }

            for (int i = 0; i <= source1Length; matrix[i][0] = ++i)
            {
                // Spin to force array population
            }

            for (int j = 0; j <= source2Length; matrix[0][j] = ++j)
            {
                // Spin to force array population
            }

            for (int i = 1; i <= source1Length; ++i)
            {
                for (int j = 1; j <= source2Length; ++j)
                {
                    int cost = source2[j - 1] == source1[i - 1] ? 0 : 1;
                    matrix[i][j] = Mathf.Min(
                        Mathf.Min(matrix[i - 1][j] + 1, matrix[i][j - 1] + 1),
                        matrix[i - 1][j - 1] + cost
                    );
                }
            }
            return matrix[source1Length][source2Length];
        }

        public static string ToPascalCase(this string value, string separator = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            int startIndex = 0;
            StringBuilder stringBuilder = StringBuilderCache.Value;
            stringBuilder.Clear();
            bool appendedAnySeparator = false;
            for (int i = 0; i < value.Length; ++i)
            {
                while (
                    startIndex < value.Length && PascalCaseSeparators.Contains(value[startIndex])
                )
                {
                    ++startIndex;
                }

                if (
                    startIndex < i
                    && char.IsLower(value[i - 1])
                    && (char.IsUpper(value[i]) || PascalCaseSeparators.Contains(value[i]))
                )
                {
                    _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                    if (1 < i - startIndex)
                    {
                        for (int j = startIndex + 1; j < i; ++j)
                        {
                            char current = value[j];
                            if (PascalCaseSeparators.Contains(current))
                            {
                                continue;
                            }

                            _ = stringBuilder.Append(char.ToLower(current));
                        }
                    }

                    if (!string.IsNullOrEmpty(separator))
                    {
                        appendedAnySeparator = true;
                        _ = stringBuilder.Append(separator);
                    }

                    startIndex = i;
                    continue;
                }

                if (
                    startIndex + 1 < i
                    && char.IsLower(value[i])
                    && (char.IsUpper(value[i - 1]) || PascalCaseSeparators.Contains(value[i - 1]))
                )
                {
                    _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                    if (1 < i - 1 - startIndex)
                    {
                        for (int j = startIndex + 1; j < i; ++j)
                        {
                            char current = value[j];
                            if (PascalCaseSeparators.Contains(current))
                            {
                                continue;
                            }

                            _ = stringBuilder.Append(char.ToLower(current));
                        }
                    }

                    if (!string.IsNullOrEmpty(separator))
                    {
                        appendedAnySeparator = true;
                        _ = stringBuilder.Append(separator);
                    }

                    startIndex = i - 1;
                }
            }

            if (startIndex < value.Length)
            {
                _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                if (startIndex + 1 < value.Length)
                {
                    for (int j = startIndex + 1; j < value.Length; ++j)
                    {
                        char current = value[j];
                        if (PascalCaseSeparators.Contains(current))
                        {
                            continue;
                        }

                        _ = stringBuilder.Append(char.ToLower(current));
                    }
                }
            }
            else if (
                appendedAnySeparator
                && !string.IsNullOrEmpty(separator)
                && separator.Length <= stringBuilder.Length
            )
            {
                stringBuilder.Remove(stringBuilder.Length - separator.Length, separator.Length);
            }

            return stringBuilder.ToString();
        }

        public static bool NeedsLowerInvariantConversion(this string input)
        {
            foreach (char inputCharacter in input)
            {
                if (char.ToLowerInvariant(inputCharacter) != inputCharacter)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool NeedsTrim(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            return char.IsWhiteSpace(input[0]) || char.IsWhiteSpace(input[^1]);
        }
    }
}
