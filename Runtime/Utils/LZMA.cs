namespace WallstopStudios.UnityHelpers.Utils
{
    using System;
    using System.IO;
    using System.Threading;
    using SevenZip.Compression.LZMA;
    using WallstopStudios.UnityHelpers.Core.Serialization;

    public static class LZMA
    {
        private static readonly ThreadLocal<Decoder> Decoders = new(() => new Decoder());
        private static readonly ThreadLocal<Encoder> Encoders = new(() => new Encoder());
        private static readonly ThreadLocal<byte[]> Properties = new(() => new byte[5]);
        private static readonly ThreadLocal<byte[]> FileLengths = new(() => new byte[8]);

        public static byte[] Compress(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            using (PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream inStream))
            using (PooledBufferStream.Rent(out PooledBufferStream outStream))
            {
                inStream.SetBuffer(input);

                Encoder encoder = Encoders.Value;
                encoder.WriteCoderProperties(outStream);

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                Span<byte> len = stackalloc byte[8];
                BitConverter.TryWriteBytes(len, inStream.Length);
                outStream.Write(len);
#else
                byte[] len = BitConverter.GetBytes(inStream.Length);
                outStream.Write(len, 0, 8);
#endif

                encoder.Code(inStream, outStream, inStream.Length, -1, null);

                byte[] result = null;
                int count = outStream.ToArrayExact(ref result);
                if (count != result.Length)
                {
                    Array.Resize(ref result, count);
                }
                return result;
            }
        }

        public static byte[] Decompress(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length < 13)
            {
                throw new Exception("Input is too short to be valid LZMA data.");
            }

            using (PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream inStream))
            using (PooledBufferStream.Rent(out PooledBufferStream outStream))
            {
                inStream.SetBuffer(input);

                byte[] properties = Properties.Value;
                Array.Clear(properties, 0, properties.Length);
                int readProps = inStream.Read(properties, 0, properties.Length);
                if (readProps != properties.Length)
                {
                    throw new Exception("Failed to read LZMA properties header.");
                }

                byte[] fileLengthBytes = FileLengths.Value;
                Array.Clear(fileLengthBytes, 0, fileLengthBytes.Length);
                int readLen = inStream.Read(fileLengthBytes, 0, 8);
                if (readLen != 8)
                {
                    throw new Exception("Failed to read LZMA length header.");
                }
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                if (fileLength < 0)
                {
                    throw new Exception("Invalid LZMA length header.");
                }

                if (fileLength == 0 && inStream.Position >= inStream.Length)
                {
                    throw new Exception("Failed to decompress LZMA data. No payload present.");
                }

                Decoder decoder = Decoders.Value;
                try
                {
                    decoder.SetDecoderProperties(properties);
                    decoder.Code(
                        inStream,
                        outStream,
                        inStream.Length - inStream.Position,
                        fileLength,
                        null
                    );

                    if (outStream.Length != fileLength)
                    {
                        throw new Exception("Failed to decompress LZMA data. Length mismatch.");
                    }

                    if (inStream.Position != inStream.Length)
                    {
                        throw new Exception(
                            "Failed to decompress LZMA data. Trailing bytes not consumed."
                        );
                    }

                    byte[] result = null;
                    int count = outStream.ToArrayExact(ref result);
                    if (count != result.Length)
                    {
                        Array.Resize(ref result, count);
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to decompress LZMA data.", ex);
                }
            }
        }

        public static void CompressTo(Stream output, byte[] input)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            using (PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream inStream))
            {
                inStream.SetBuffer(input ?? Array.Empty<byte>());
                Encoder encoder = Encoders.Value;
                encoder.WriteCoderProperties(output);

#if NETSTANDARD2_1 || NET5_0_OR_GREATER
                Span<byte> len = stackalloc byte[8];
                BitConverter.TryWriteBytes(len, inStream.Length);
                output.Write(len);
#else
                byte[] len = BitConverter.GetBytes(inStream.Length);
                output.Write(len, 0, 8);
#endif
                encoder.Code(inStream, output, inStream.Length, -1, null);
            }
        }

        public static void DecompressTo(Stream output, byte[] input)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (input == null || input.Length < 13)
            {
                throw new Exception("Input is too short to be valid LZMA data.");
            }

            using (PooledReadOnlyMemoryStream.Rent(out PooledReadOnlyMemoryStream inStream))
            {
                inStream.SetBuffer(input);

                byte[] properties = Properties.Value;
                Array.Clear(properties, 0, properties.Length);
                int readProps = inStream.Read(properties, 0, properties.Length);
                if (readProps != properties.Length)
                {
                    throw new Exception("Failed to read LZMA properties header.");
                }

                byte[] fileLengthBytes = FileLengths.Value;
                Array.Clear(fileLengthBytes, 0, fileLengthBytes.Length);
                int readLen = inStream.Read(fileLengthBytes, 0, 8);
                if (readLen != 8)
                {
                    throw new Exception("Failed to read LZMA length header.");
                }
                long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);
                if (fileLength < 0)
                {
                    throw new Exception("Invalid LZMA length header.");
                }

                if (fileLength == 0 && inStream.Position >= inStream.Length)
                {
                    throw new Exception("Failed to decompress LZMA data. No payload present.");
                }

                Decoder decoder = Decoders.Value;
                try
                {
                    decoder.SetDecoderProperties(properties);
                    decoder.Code(
                        inStream,
                        output,
                        inStream.Length - inStream.Position,
                        fileLength,
                        null
                    );

                    if (inStream.Position != inStream.Length)
                    {
                        throw new Exception(
                            "Failed to decompress LZMA data. Trailing bytes not consumed."
                        );
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to decompress LZMA data.", ex);
                }
            }
        }

        public static void CompressTo(Stream output, ReadOnlySpan<byte> input)
        {
            CompressTo(output, input.ToArray());
        }

        public static void DecompressTo(Stream output, ReadOnlySpan<byte> input)
        {
            DecompressTo(output, input.ToArray());
        }
    }
}
