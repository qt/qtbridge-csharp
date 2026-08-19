// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Bridge.Models;

namespace ModelsAndViews
{
    public sealed class FolderNode(string name) : TreeNode<FolderNode>
    {
        public string Name { get; } = name;
    }

    public sealed class SimpleTreeData : TreeModel<FolderNode>
    {
        public SimpleTreeData()
        {
            var projects = AddRoot(new FolderNode("Projects"));
            AddChild(projects, new FolderNode("Qt"));
            AddChild(projects, new FolderNode("Examples"));

            var documents = AddRoot(new FolderNode("Documents"));
            AddChild(documents, new FolderNode("Reports"));
            AddChild(documents, new FolderNode("Notes"));
        }
    }
}
