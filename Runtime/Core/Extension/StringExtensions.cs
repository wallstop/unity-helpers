namespace UnityHelpers.Core.Extension
{
    using System.Collections.Generic;
    using System.Text;
    using Serialization;

    public static class StringExtensions
    {
        private static readonly HashSet<char> PascalCaseSeparators =
            new() { '_', ' ', '\r', '\n', '\t', '.', '\'', '"' };

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

        public static string ToPascalCase(this string value, string separator = "")
        {
            int startIndex = 0;
            StringBuilder stringBuilder = new();
            bool appendedAnySeparator = false;
            for (int i = 0; i < value.Length; ++i)
            {
                while (startIndex < value.Length && PascalCaseSeparators.Contains(value[startIndex]))
                {
                    ++startIndex;
                }

                if (startIndex < i && char.IsLower(value[i - 1]) &&
                    (char.IsUpper(value[i]) || PascalCaseSeparators.Contains(value[i])))
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

                if (startIndex + 1 < i && char.IsLower(value[i]) &&
                    (char.IsUpper(value[i - 1]) || PascalCaseSeparators.Contains(value[i - 1])))
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
            else if (appendedAnySeparator && !string.IsNullOrEmpty(separator) &&
                     separator.Length <= stringBuilder.Length)
            {
                stringBuilder.Remove(stringBuilder.Length - separator.Length, separator.Length);
            }

            return stringBuilder.ToString();
        }
    }
}