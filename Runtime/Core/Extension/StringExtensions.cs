namespace Core.Extension
{
    using System.Text;
    using Serialization;

    public static class StringExtensions
    {
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
            return System.Text.Encoding.Default.GetBytes(input);
        }

        public static string GetString(this byte[] bytes)
        {
            return System.Text.Encoding.Default.GetString(bytes);
        }

        public static string ToJson<T>(this T value)
        {
            return Serializer.JsonStringify(value);
        }

        public static string ToPascalCase(this string value, string separator = "")
        {
            int startIndex = 0;
            StringBuilder stringBuilder = new();
            for (int i = 0; i < value.Length; ++i)
            {
                if (startIndex < i && char.IsLower(value[i - 1]) && char.IsUpper(value[i]))
                {
                    _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                    if (1 < i - startIndex)
                    {
                        _ = stringBuilder.Append(value.Substring(startIndex + 1, i - 1 - startIndex).ToLower());
                    }

                    _ = stringBuilder.Append(separator);
                    startIndex = i;
                    continue;
                }

                if (startIndex + 1 < i && char.IsLower(value[i]) && char.IsUpper(value[i - 1]))
                {
                    _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                    if (1 < i - 1 - startIndex)
                    {
                        _ = stringBuilder.Append(value.Substring(startIndex + 1, i - 1 - startIndex).ToLower());
                    }
                    _ = stringBuilder.Append(separator);
                    startIndex = i - 1;
                }
            }

            if (startIndex < value.Length)
            {
                _ = stringBuilder.Append(char.ToUpper(value[startIndex]));
                if (startIndex + 1 < value.Length)
                {
                    _ = stringBuilder.Append(value.Substring(startIndex + 1, value.Length - 1 - startIndex).ToLower());
                }
            }

            return stringBuilder.ToString();
        }
    }
}
