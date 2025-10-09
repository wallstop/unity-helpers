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
            // Use a fresh encoder per call to avoid stateful side effects
            Encoder encoder = new Encoder();
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

            if (input.Length < 13)
            {
                throw new Exception("Input is too short to be valid LZMA data.");
            }

            using MemoryStream inStream = new(input, writable: false);
            using MemoryStream outStream = new();

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

            Decoder decoder = new Decoder();
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

                // Validate declared length matches actual output
                if (outStream.Length != fileLength)
                {
                    throw new Exception("Failed to decompress LZMA data. Length mismatch.");
                }

                // Ensure the decoder consumed the entire payload
                if (inStream.Position != inStream.Length)
                {
                    throw new Exception(
                        "Failed to decompress LZMA data. Trailing bytes not consumed."
                    );
                }
                return outStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to decompress LZMA data.", ex);
            }
        }
    }
}
