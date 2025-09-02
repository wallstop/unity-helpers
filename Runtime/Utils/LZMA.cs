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
            Decoder decoder = Decoders.Value;
            using MemoryStream inStream = new(input, writable: false);
            using MemoryStream outStream = new();
            // Read decoder properties
            byte[] properties = Properties.Value;
            Array.Clear(properties, 0, properties.Length);
            inStream.Read(properties, 0, properties.Length);
            decoder.SetDecoderProperties(properties);

            // Read original file size
            byte[] fileLengthBytes = FileLengths.Value;
            Array.Clear(fileLengthBytes, 0, fileLengthBytes.Length);
            inStream.Read(fileLengthBytes, 0, 8);
            long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

            // Decompress the data
            decoder.Code(inStream, outStream, inStream.Length, fileLength, null);
            return outStream.ToArray();
        }
    }
}
