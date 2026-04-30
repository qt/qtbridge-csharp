// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Runtime.Serialization;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer.Contracts
{
    [DataContract]
    internal sealed class LspMethodDto
    {
        [DataMember(Name = "method")]
        public string? Method { get; set; }
    }

    [DataContract]
    internal sealed class SemanticTokensRefreshRequestDto
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [DataMember(Name = "id")]
        public string? Id { get; set; }

        [DataMember(Name = "method")]
        public string Method { get; set; } = "workspace/semanticTokens/refresh";
    }

    [DataContract]
    internal sealed class WorkspaceFoldersNotificationDto
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [DataMember(Name = "method")]
        public string Method { get; set; } = "workspace/didChangeWorkspaceFolders";

        [DataMember(Name = "params")]
        public WorkspaceFoldersEventContainerDto? Params { get; set; }
    }

    [DataContract]
    internal sealed class WorkspaceFoldersEventContainerDto
    {
        [DataMember(Name = "event")]
        public WorkspaceFoldersEventDto? Event { get; set; }
    }

    [DataContract]
    internal sealed class WorkspaceFoldersEventDto
    {
        [DataMember(Name = "added")]
        public WorkspaceFolderDto[]? Added { get; set; }

        [DataMember(Name = "removed")]
        public WorkspaceFolderDto[]? Removed { get; set; }
    }

    [DataContract]
    internal sealed class WorkspaceFolderDto
    {
        [DataMember(Name = "uri")]
        public string? Uri { get; set; }

        [DataMember(Name = "name")]
        public string? Name { get; set; }
    }

    [DataContract]
    internal sealed class AddBuildDirsNotificationDto
    {
        [DataMember(Name = "jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [DataMember(Name = "method")]
        public string Method { get; set; } = "$/addBuildDirs";

        [DataMember(Name = "params")]
        public AddBuildDirsParamsDto? Params { get; set; }
    }

    [DataContract]
    internal sealed class AddBuildDirsParamsDto
    {
        [DataMember(Name = "buildDirsToSet")]
        public BuildDirsEntryDto[]? BuildDirsToSet { get; set; }
    }

    [DataContract]
    internal sealed class BuildDirsEntryDto
    {
        [DataMember(Name = "baseUri")]
        public string? BaseUri { get; set; }

        [DataMember(Name = "buildDirs")]
        public string[]? BuildDirs { get; set; }
    }
}
