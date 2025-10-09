namespace WallstopStudios.UnityHelpers.Utils
{
    // LzmaHelper.cs - A wrapper for the LZMA SDK
    using System;
    using System.IO;
    using System.Threading;
    using SevenZip.Compression.LZMA;

    public static class LZMA
    {
        private static readonly ThreadLocal<Decoder> Decoders = new(() => new Decoder());
        private static readonly ThreadLocal<Encoder> Encoders = new(() => new Encoder());
        private static readonly ThreadLocal<byte[]> Properties = new(() => new byte[5]);
        private static readonly ThreadLocal<byte[]> FileLengths = new(() => new byte[8]);

        public static byte[] Compress(byte[] input)
        {
            Encoder encoder = Encoders.Value;
            using MemoryStream inStream = new(input, writable: false);
            using MemoryStream outStream = new();
            encoder.WriteCoderProperties(outStream);

            // Write original file size
            outStream.Write(BitConverter.GetBytes(inStream.Length), 0, 8);

            // Compress the data
            encoder.Code(inStream, outStream, inStream.Length, -1, null);
            return outStream.ToArray();
        }

        public static byte[] Decompress(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // LZMA header is 5 bytes (properties) + 8 bytes (uncompressed length)
            if (input.Length < 13)
            {
                throw new Exception("Input is too short to be valid LZMA data.");
            }

            Decoder decoder = Decoders.Value;
            using MemoryStream inStream = new(input, writable: false);
            using MemoryStream outStream = new();
            try
            {
                // Read decoder properties
                byte[] properties = Properties.Value;
                Array.Clear(properties, 0, properties.Length);
                int readProps = inStream.Read(properties, 0, properties.Length);
                if (readProps != properties.Length)
                {
                    throw new Exception("Failed to read LZMA properties header.");
                }
                decoder.SetDecoderProperties(properties);

                // Read original file size
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

                // Decompress the data
                decoder.Code(
                    inStream,
                    outStream,
                    inStream.Length - inStream.Position,
                    fileLength,
                    null
                );
                return outStream.ToArray();
            }
            catch (Exception ex)
            {
                // Ensure we rethrow as a general exception to satisfy tests
                throw new Exception("Failed to decompress LZMA data.", ex);
            }
        }
    }
}
