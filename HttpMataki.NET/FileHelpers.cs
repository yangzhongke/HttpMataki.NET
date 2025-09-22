using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HttpMataki.NET;

/// <summary>
/// Helper class to provide File.WriteAllBytesAsync for .NET Standard 2.0 compatibility
/// </summary>
internal static class FileHelpers
{
    /// <summary>
    /// Asynchronously writes bytes to a file. This is a compatibility method for .NET Standard 2.0.
    /// </summary>
    /// <param name="path">The file path to write to</param>
    /// <param name="bytes">The bytes to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await fileStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }
}
