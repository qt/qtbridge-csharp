// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Test_Qt.Bridge.Project
{
    using static AssemblyMetadata;

    internal static class Packages
    {
        private static string QtBridgeRid
        {
            get
            {
                var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "win"
                    : "linux";
                var arch = RuntimeInformation.OSArchitecture switch {
                    Architecture.Arm64 => "arm64",
                    Architecture.X86 => "x86",
                    _ => "x64"
                };
                return $"{os}-{arch}";
            }
        }

        public static (string, string) QtBridge
            => ($"QtGroup.Qt.Bridge.CSharp.{QtBridgeRid}", SelectedVersion);
    }
}
