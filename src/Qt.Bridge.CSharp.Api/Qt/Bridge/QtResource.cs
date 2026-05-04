// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge
{
    // Infrastructure attribute - not part of the public API for C# application developers. The
    // QtBridgeAddResources MSBuild target stamps these onto the compiled assembly for each
    // <QtResource> / <QtResx> item declared in the .csproj. The Qt Bridge generator then reads
    // them back via reflection (AssemblyExtensions.QtResources) to emit the CMake / .qrc build
    // spec. End-users shall never write [QtResource(...)] by hand.
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class QtResourceAttribute : Attribute
    {
        public string SourcePath { get; set; }
        public string Alias { get; set; }
        public string Prefix { get; set; } = "/";
        public string AssemblyId { get; set; }
        public string Key { get; set; }
        public string AccessMode { get; set; } = "Default";
        public string Reason { get; set; }
    }
}
