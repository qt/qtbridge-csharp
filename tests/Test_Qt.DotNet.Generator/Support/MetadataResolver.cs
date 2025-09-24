/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Test_Qt.DotNet.Generator.Support
{
    internal static class MetadataResolver
    {
        private const string TPA = "TRUSTED_PLATFORM_ASSEMBLIES";

        public enum CoreAssembly
        {
            DotNetFramework, // mscorlib
            DotNetCore       // System.Private.CoreLib
        }

        /// <summary>
        /// Creates a MetadataLoadContext using a PathAssemblyResolver built from:
        /// - Trusted Platform Assemblies (TPA) if available
        /// - otherwise runtime directory (*.dll)
        /// - any extra directories scanned for *.dll
        /// - optional explicit assembly file paths
        /// </summary>
        public static MetadataLoadContext CreateLoadContext(IEnumerable<string> directories = null,
            IEnumerable<string> assemblyPaths = null, CoreAssembly core = CoreAssembly.DotNetCore)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Trusted Platform Assemblies
            var trustedPlatformAssemblies = AppContext.GetData(TPA) as string;
            if (!string.IsNullOrEmpty(trustedPlatformAssemblies)) {
                foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
                    paths.Add(path);
            } else {
                // Fallback: runtime directory (*.dll)
                var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
                foreach (var path in Directory.EnumerateFiles(runtimeDirectory, "*.dll"))
                    paths.Add(path);
            }

            // Extra directories (*.dll)
            foreach (var directory in directories?.Where(Directory.Exists) ?? []) {
                foreach (var path in Directory.EnumerateFiles(directory, "*.dll"))
                    paths.Add(path);
            }

            // Explicit assembly paths
            foreach (var path in assemblyPaths?.Where(File.Exists) ?? [])
                paths.Add(path);

            return new MetadataLoadContext(new PathAssemblyResolver(paths), core switch {
                CoreAssembly.DotNetFramework => "mscorlib",
                CoreAssembly.DotNetCore => "System.Private.CoreLib",
                _ => throw new ArgumentOutOfRangeException(nameof(core), core, null)
            });
        }
    }
}

