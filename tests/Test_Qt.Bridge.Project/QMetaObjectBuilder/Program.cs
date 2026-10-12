// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace QMetaObjectBuilderFixture
{
    public sealed class MetadataEventArgs(int value) : EventArgs
    {
        public int Value { get; } = value;
    }

    // Ensure the QtQuickTest C++ host is built when the other types use metadata export.
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class SourceCodeDependency
    {
        public int Id { get; } = 1;
    }

    [QmlElement]
    public sealed class MetadataObject
    {
        public SourceCodeDependency Dependency { get; } = new();

        public event EventHandler<MetadataEventArgs> Changed;
        public event EventHandler<MetadataEventArgs> Updated;

        public int Value() => 42;
        public int Add(int left, int right) => left + right;

        public void RaiseEvents()
        {
            Changed?.Invoke(this, new(1));
            Updated?.Invoke(this, new(2));
        }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }
}
