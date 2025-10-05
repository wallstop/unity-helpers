namespace WallstopStudios.UnityHelpers.Core.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text;
    using Serialization;
    using UnityEngine;
    using WallstopStudios.UnityHelpers.Utils;

    public static class StringExtensions
    {
        private static readonly ImmutableHashSet<char> WordSeparators = new HashSet<char>
        {
            '_',
            '-',
            ' ',
            '\r',
            '\n',
            '\t',
            '.',
        }.ToImmutableHashSet();

        private static readonly ImmutableHashSet<char> CharsToStrip = new HashSet<char>
        {
            '\'',
            '"',
        }.ToImmutableHashSet();

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
            return Encoding.UTF8.GetBytes(input);
        }

        public static string GetString(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(bytes);
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

            if (source1Length == 0)
            {
                return source2Length;
            }

            if (source2Length == 0)
            {
                return source1Length;
            }

            using PooledResource<int[][]> matrixResource = WallstopFastArrayPool<int[]>.Get(
                source1Length + 1
            );
            using PooledResource<List<PooledResource<int[]>>> bufferedArrays = Buffers<
                PooledResource<int[]>
            >.List.Get();
            List<PooledResource<int[]>> bufferedArraysList = bufferedArrays.resource;
            try
            {
                int[][] matrix = matrixResource.resource;
                for (int index = 0; index < source1Length + 1; ++index)
                {
                    PooledResource<int[]> innerResource = WallstopFastArrayPool<int>.Get(
                        source2Length + 1
                    );
                    bufferedArraysList.Add(innerResource);
                    matrix[index] = innerResource.resource;
                }

                for (int i = 0; i <= source1Length; ++i)
                {
                    matrix[i][0] = i;
                }

                for (int j = 0; j <= source2Length; ++j)
                {
                    matrix[0][j] = j;
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
            finally
            {
                foreach (PooledResource<int[]> bufferedArray in bufferedArraysList)
                {
                    bufferedArray.Dispose();
                }
            }
        }

        public static string ToPascalCase(this string value, string separator = "")
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;
            using PooledResource<StringBuilder> wordBuffer = Buffers.StringBuilder.Get();
            StringBuilder currentWord = wordBuffer.resource;
            bool isFirstWord = true;

            for (int i = 0; i < value.Length; ++i)
            {
                char current = value[i];

                // Skip characters that should be stripped (apostrophes, quotes)
                if (CharsToStrip.Contains(current))
                {
                    continue;
                }

                // Check if this is a separator or whitespace
                if (WordSeparators.Contains(current))
                {
                    // Flush the current word if we have one
                    if (currentWord.Length > 0)
                    {
                        if (!isFirstWord && !string.IsNullOrEmpty(separator))
                        {
                            _ = stringBuilder.Append(separator);
                        }

                        // Capitalize first letter, lowercase the rest
                        _ = stringBuilder.Append(char.ToUpper(currentWord[0]));
                        for (int j = 1; j < currentWord.Length; ++j)
                        {
                            _ = stringBuilder.Append(char.ToLower(currentWord[j]));
                        }

                        currentWord.Clear();
                        isFirstWord = false;
                    }

                    continue;
                }

                // Check for word boundary: lowercase/digit to uppercase transition
                if (
                    currentWord.Length > 0
                    && !char.IsUpper(currentWord[^1])
                    && char.IsUpper(current)
                )
                {
                    // Flush the current word
                    if (!isFirstWord && !string.IsNullOrEmpty(separator))
                    {
                        _ = stringBuilder.Append(separator);
                    }

                    // Capitalize first letter, lowercase the rest
                    _ = stringBuilder.Append(char.ToUpper(currentWord[0]));
                    for (int j = 1; j < currentWord.Length; ++j)
                    {
                        _ = stringBuilder.Append(char.ToLower(currentWord[j]));
                    }

                    currentWord.Clear();
                    isFirstWord = false;
                }

                // Check for word boundary: multiple uppercase followed by lowercase
                // e.g., "XMLParser" -> "XML" "Parser"
                if (
                    currentWord.Length > 1
                    && char.IsUpper(currentWord[^1])
                    && char.IsUpper(currentWord[^2])
                    && char.IsLower(current)
                )
                {
                    // Take all but the last uppercase character as one word
                    char lastChar = currentWord[^1];
                    currentWord.Length -= 1;

                    if (!isFirstWord && !string.IsNullOrEmpty(separator))
                    {
                        _ = stringBuilder.Append(separator);
                    }

                    // Capitalize first letter, lowercase the rest
                    _ = stringBuilder.Append(char.ToUpper(currentWord[0]));
                    for (int j = 1; j < currentWord.Length; ++j)
                    {
                        _ = stringBuilder.Append(char.ToLower(currentWord[j]));
                    }

                    currentWord.Clear();
                    _ = currentWord.Append(lastChar);
                    isFirstWord = false;
                }

                _ = currentWord.Append(current);
            }

            // Flush any remaining word
            if (currentWord.Length > 0)
            {
                if (!isFirstWord && !string.IsNullOrEmpty(separator))
                {
                    _ = stringBuilder.Append(separator);
                }

                _ = stringBuilder.Append(char.ToUpper(currentWord[0]));
                for (int j = 1; j < currentWord.Length; ++j)
                {
                    _ = stringBuilder.Append(char.ToLower(currentWord[j]));
                }
            }

            return stringBuilder.ToString();
        }

        public static bool NeedsLowerInvariantConversion(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

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

        public static string Truncate(this string input, int maxLength, string ellipsis = "...")
        {
            if (string.IsNullOrEmpty(input) || maxLength < 0)
            {
                return input;
            }

            if (input.Length <= maxLength)
            {
                return input;
            }

            if (string.IsNullOrEmpty(ellipsis))
            {
                return input.Substring(0, maxLength);
            }

            int truncateLength = Math.Max(0, maxLength - ellipsis.Length);
            return input.Substring(0, truncateLength) + ellipsis;
        }

        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string pascalCase = value.ToPascalCase();
            if (pascalCase.Length == 0)
            {
                return string.Empty;
            }

            if (pascalCase.Length == 1)
            {
                return char.ToLower(pascalCase[0]).ToString();
            }

            // Use StringBuilder for better performance
            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;
            _ = stringBuilder.Append(char.ToLower(pascalCase[0]));

            for (int i = 1; i < pascalCase.Length; ++i)
            {
                _ = stringBuilder.Append(pascalCase[i]);
            }

            return stringBuilder.ToString();
        }

        public static string ToSnakeCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Check if the string is already in valid snake_case format
            // (all lowercase letters/digits/underscores, no consecutive underscores, no leading/trailing underscores)
            bool isAlreadySnakeCase = true;
            bool hasUpperCase = false;
            bool hasWordSeparators = false;

            for (int i = 0; i < value.Length; ++i)
            {
                char c = value[i];
                if (char.IsUpper(c))
                {
                    hasUpperCase = true;
                    isAlreadySnakeCase = false;
                    break;
                }
                if (WordSeparators.Contains(c))
                {
                    hasWordSeparators = true;
                    if (c != '_')
                    {
                        isAlreadySnakeCase = false;
                        break;
                    }
                    // Check for consecutive underscores or leading/trailing underscores
                    if (i == 0 || i == value.Length - 1 || (i > 0 && value[i - 1] == '_'))
                    {
                        isAlreadySnakeCase = false;
                        break;
                    }
                }
                else if (!char.IsLower(c) && !char.IsDigit(c))
                {
                    isAlreadySnakeCase = false;
                    break;
                }
            }

            // If already in snake_case, return as-is
            if (isAlreadySnakeCase)
            {
                return value;
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;

            for (int i = 0; i < value.Length; ++i)
            {
                char current = value[i];

                if (WordSeparators.Contains(current))
                {
                    if (stringBuilder.Length > 0 && stringBuilder[^1] != '_')
                    {
                        _ = stringBuilder.Append('_');
                    }
                    continue;
                }

                if (i > 0)
                {
                    char previous = value[i - 1];
                    bool shouldAddSeparator = false;

                    // Handle uppercase letter transitions
                    if (char.IsUpper(current))
                    {
                        if (!WordSeparators.Contains(previous) && !char.IsUpper(previous))
                        {
                            shouldAddSeparator = true;
                        }
                        else if (i + 1 < value.Length && char.IsLower(value[i + 1]))
                        {
                            shouldAddSeparator = true;
                        }
                    }
                    // Handle letter-to-digit transition ONLY if we have uppercase letters or word separators
                    else if (
                        char.IsDigit(current)
                        && char.IsLetter(previous)
                        && (hasUpperCase || hasWordSeparators)
                    )
                    {
                        shouldAddSeparator = true;
                    }
                    // Handle digit-to-letter transition ONLY if we have uppercase letters or word separators
                    else if (
                        char.IsLetter(current)
                        && char.IsDigit(previous)
                        && (hasUpperCase || hasWordSeparators)
                    )
                    {
                        shouldAddSeparator = true;
                    }

                    if (shouldAddSeparator && stringBuilder.Length > 0 && stringBuilder[^1] != '_')
                    {
                        _ = stringBuilder.Append('_');
                    }
                }

                _ = stringBuilder.Append(char.ToLower(current));
            }

            string result = stringBuilder.ToString();
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            return result.Trim('_');
        }

        public static string ToKebabCase(this string value)
        {
            return value.ToSnakeCase().Replace('_', '-');
        }

        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;
            bool capitalizeNext = true;

            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c) || WordSeparators.Contains(c))
                {
                    _ = stringBuilder.Append(c);
                    capitalizeNext = true;
                }
                else if (capitalizeNext)
                {
                    _ = stringBuilder.Append(char.ToUpper(c));
                    capitalizeNext = false;
                }
                else
                {
                    _ = stringBuilder.Append(char.ToLower(c));
                }
            }

            return stringBuilder.ToString();
        }

        public static bool ContainsIgnoreCase(this string input, string value)
        {
            if (input == null || value == null)
            {
                return false;
            }

            return input.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool EqualsIgnoreCase(this string input, string value)
        {
            return string.Equals(input, value, StringComparison.OrdinalIgnoreCase);
        }

        public static string Reverse(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            char[] chars = input.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        public static string RemoveWhitespace(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;

            foreach (char c in input)
            {
                if (!char.IsWhiteSpace(c))
                {
                    _ = stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }

        public static int CountOccurrences(this string input, char character)
        {
            if (string.IsNullOrEmpty(input))
            {
                return 0;
            }

            int count = 0;
            foreach (char c in input)
            {
                if (c == character)
                {
                    ++count;
                }
            }

            return count;
        }

        public static int CountOccurrences(this string input, string substring)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(substring))
            {
                return 0;
            }

            int count = 0;
            int index = 0;

            while ((index = input.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
            {
                ++count;
                index += substring.Length;
            }

            return count;
        }

        public static bool IsNumeric(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (char c in input)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsAlphabetic(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (char c in input)
            {
                if (!char.IsLetter(c))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsAlphanumeric(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        public static string ToBase64(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        public static string FromBase64(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(input);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        public static string Repeat(this string input, int count)
        {
            if (string.IsNullOrEmpty(input) || count <= 0)
            {
                return string.Empty;
            }

            if (count == 1)
            {
                return input;
            }

            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder stringBuilder = stringBuilderBuffer.resource;

            for (int i = 0; i < count; ++i)
            {
                _ = stringBuilder.Append(input);
            }

            return stringBuilder.ToString();
        }

        public static string[] SplitCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Array.Empty<string>();
            }

            using PooledResource<List<string>> listBuffer = Buffers<string>.List.Get();
            List<string> words = listBuffer.resource;
            using PooledResource<StringBuilder> stringBuilderBuffer = Buffers.StringBuilder.Get();
            StringBuilder currentWord = stringBuilderBuffer.resource;

            for (int i = 0; i < input.Length; ++i)
            {
                char current = input[i];

                if (WordSeparators.Contains(current))
                {
                    if (currentWord.Length > 0)
                    {
                        words.Add(currentWord.ToString());
                        currentWord.Clear();
                    }
                    continue;
                }

                if (char.IsUpper(current) && i > 0)
                {
                    char previous = input[i - 1];
                    if (!char.IsUpper(previous) && !WordSeparators.Contains(previous))
                    {
                        if (currentWord.Length > 0)
                        {
                            words.Add(currentWord.ToString());
                            currentWord.Clear();
                        }
                    }
                    else if (i + 1 < input.Length && char.IsLower(input[i + 1]))
                    {
                        if (currentWord.Length > 0)
                        {
                            words.Add(currentWord.ToString());
                            currentWord.Clear();
                        }
                    }
                }

                _ = currentWord.Append(current);
            }

            if (currentWord.Length > 0)
            {
                words.Add(currentWord.ToString());
            }

            return words.ToArray();
        }

        public static string ReplaceFirst(this string input, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
            {
                return input;
            }

            int index = input.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
            {
                return input;
            }

            return input.Substring(0, index)
                + (newValue ?? string.Empty)
                + input.Substring(index + oldValue.Length);
        }

        public static string ReplaceLast(this string input, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(oldValue))
            {
                return input;
            }

            int index = input.LastIndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
            {
                return input;
            }

            return input.Substring(0, index)
                + (newValue ?? string.Empty)
                + input.Substring(index + oldValue.Length);
        }
    }
}
