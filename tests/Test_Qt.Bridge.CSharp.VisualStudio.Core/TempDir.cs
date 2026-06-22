// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using IO = System.IO;

namespace Test_Qt.Bridge.CSharp.VisualStudio.Core
{
    internal sealed class TempDir : IDisposable
    {
        public TempDir()
        {
            Path = IO.Path.Combine(IO.Path.GetTempPath(), IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
