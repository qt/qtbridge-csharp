// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using Qt.Bridge.Models;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    public sealed class FolderNode(string name) : TreeNode<FolderNode>
    {
        public string Name { get; } = name;
    }

    [QmlElement]
    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class MetadataTree : TreeModel<FolderNode>
    {
        public MetadataTree()
        {
            var projects = AddRoot(new FolderNode("Projects"));
            AddChild(projects, new FolderNode("Qt"));
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
