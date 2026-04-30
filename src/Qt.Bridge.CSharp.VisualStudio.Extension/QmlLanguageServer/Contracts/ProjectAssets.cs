// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.Serialization;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer.Contracts
{
    [DataContract]
    internal sealed class ProjectAssetsDto
    {
        [DataMember(Name = "libraries")]
        public Dictionary<string, ProjectAssetsLibraryDto>? Libraries { get; set; }

        [DataMember(Name = "packageFolders")]
        public Dictionary<string, ProjectAssetsPackageFolderDto>? PackageFolders { get; set; }
    }

    [DataContract]
    internal sealed class ProjectAssetsLibraryDto
    {
        [DataMember(Name = "path")]
        public string? Path { get; set; }
    }

    [DataContract]
    internal sealed class ProjectAssetsPackageFolderDto;
}
