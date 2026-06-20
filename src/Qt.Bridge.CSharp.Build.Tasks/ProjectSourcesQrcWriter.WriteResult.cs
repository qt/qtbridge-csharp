// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CSharp.Build.Tasks
{
    internal static partial class ProjectSourcesQrcWriter
    {
        internal readonly struct WriteResult(
            string? path,
            bool changed,
            IReadOnlyCollection<ResourceIdentityCollision> collisions)
        {
            public string? Path { get; } = path;
            public bool Changed { get; } = changed;
            public IReadOnlyCollection<ResourceIdentityCollision> Collisions { get; } = collisions;
        }

        internal readonly struct ResourceIdentityCollision(
            string resourcePath,
            IReadOnlyCollection<string> sourcePaths)
        {
            public string ResourcePath { get; } = resourcePath;
            public IReadOnlyCollection<string> SourcePaths { get; } = sourcePaths;
        }

        internal readonly struct BuildContentResult(
            string content,
            IReadOnlyCollection<ResourceIdentityCollision> collisions)
        {
            public string Content { get; } = content;
            public IReadOnlyCollection<ResourceIdentityCollision> Collisions { get; } = collisions;
        }
    }
}
