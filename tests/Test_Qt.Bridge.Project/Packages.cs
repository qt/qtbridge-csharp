// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Test_Qt.Bridge.Project
{
    using static AssemblyMetadata;

    internal static class Packages
    {
        private static readonly string CachedOS =
            OperatingSystem.IsWindows() ? "win" :
            OperatingSystem.IsMacOS()   ? "osx" :
            OperatingSystem.IsLinux()   ? "linux" :
            throw new PlatformNotSupportedException($"OS '{Environment.OSVersion}' is not supported.");

        private static readonly string CachedArch = RuntimeInformation.OSArchitecture switch {
            Architecture.X64   => "x64",
            Architecture.Arm64 => "arm64",
            Architecture.X86   => "x86",
            _ => throw new PlatformNotSupportedException(
                $"Architecture '{RuntimeInformation.OSArchitecture}' "
                + "is not supported by QtBridge.")
        };

        private static readonly string CachedQtBridgeRid = $"{CachedOS}-{CachedArch}";

        public static (string PackageId, string Version) QtBridge =>
            ($"QtGroup.Qt.Bridge.CSharp.{CachedQtBridgeRid}", SelectedVersion);
    }
}
