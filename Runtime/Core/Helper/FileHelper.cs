namespace WallstopStudios.UnityHelpers.Core.Helper
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UnityEngine;

    public static class FileHelper
    {
        public static bool InitializePath(string path, byte[] contents = null)
        {
            if (File.Exists(path))
            {
                return false;
            }

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                using FileStream fileStream = new(
                    path,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None
                );
                contents ??= Array.Empty<byte>();
                fileStream.Write(contents, 0, contents.Length);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogError($"File {path} already exists, not creating.\n{e}");
                return false;
            }
        }

        public static async ValueTask<bool> CopyFileAsync(
            string sourcePath,
            string destinationPath,
            int bufferSize = 81920,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                await using FileStream sourceStream = new(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize,
                    useAsync: true
                );
                await using FileStream destinationStream = new(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize,
                    useAsync: true
                );
                await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
