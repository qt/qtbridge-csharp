/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Qt.DotNet.CodeGeneration
{
    public interface IFileSink
    {
        Task<bool?> WriteAsync(FileInfo target, string content, Encoding encoding,
            bool forceWrite,CancellationToken cancellationToken);
    }

    public sealed class IncrementalFileSink : IFileSink
    {
        public async Task<bool?>WriteAsync(FileInfo target, string content, Encoding encoding,
            bool forceWrite, CancellationToken cancellationToken)
        {
            try {
                var data = encoding.GetBytes(content);

                if (!forceWrite && target.Exists) {
                    using var newData = new MemoryStream(data);
                    var newSha1 = await SHA1.HashDataAsync(newData, cancellationToken)
                        .ConfigureAwait(false);

                    await using var oldData = target.OpenRead();
                    var oldSha1 = await SHA1.HashDataAsync(oldData, cancellationToken)
                        .ConfigureAwait(false);

                    if (newSha1.SequenceEqual(oldSha1))
                        return false; // identical -> skip
                }

                if (target.Directory is { Exists: false, FullName.Length: > 0 })
                    Directory.CreateDirectory(target.Directory.FullName);
                await using var file = target.Open(FileMode.Create, FileAccess.Write,
                    FileShare.None);
                await file.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await file.FlushAsync(cancellationToken).ConfigureAwait(false);
                target.Refresh();
            } catch (SystemException) {
                return null;
            }
            return true;
        }
    }

    public sealed class AlwaysWriteFileSink : IFileSink
    {
        public async Task<bool?> WriteAsync(FileInfo target, string content, Encoding encoding,
            bool forceWrite, CancellationToken cancellationToken)
        {
            try {
                target.Directory?.Create();
                var data = encoding.GetBytes(content);
                await using var file = target.Open(FileMode.Create, FileAccess.Write,
                    FileShare.None);
                await file.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                await file.FlushAsync(cancellationToken).ConfigureAwait(false);
                target.Refresh();
            } catch (SystemException) {
                return null;
            }
            return true;
        }
    }

    public sealed class MemorySink : IFileSink
    {
        public ConcurrentDictionary<string, string> Files { get; } = new(StringComparer.Ordinal);

        public Task<bool?> WriteAsync(FileInfo target, string content, Encoding encoding,
            bool forceWrite, CancellationToken cancellationToken)
        {
            var baseDir = Rules.TargetDir?.FullName ?? Directory.GetCurrentDirectory();
            var key = Path.GetRelativePath(baseDir, target.FullName).Replace('\\', '/');
            Files[key] = content;
            return Task.FromResult<bool?>(true);
        }
    }
}
