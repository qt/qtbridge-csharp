// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using Extensions;
    using MetaFunctions;
    using static Placeholders;
    using static Traits;

    public class GenerateBuildSpec : Rule
    {
        protected const string Hpp = "hpp";
        protected const string Cpp = "cpp";
        protected const string HppDir = Hpp + "/";
        protected const string CppDir = Cpp + "/";

        public override bool Matches(MemberInfo src) => src.IsRootNode();
        public override Result Execute(MemberInfo _)
        {
            Placeholder sourceFiles = null, qmlElementSourceFiles = null;
            var (collisions, resourceSpec) = BuildResourceSpec();
            if (collisions.Count > 0)
                return Error(string.Join(Environment.NewLine, collisions));
            var cmake = new FilePlaceholder(
                BuildSpecFile, Root, $@"{Root.MFn(Dir)}/CMakeLists.txt");
            cmake += $@"

cmake_minimum_required(VERSION 3.16)

project({Root.MFn(Target)} VERSION {Root.MFn(Version)} LANGUAGES CXX)

if (WIN32)
    set(CMAKE_OBJECT_PATH_MAX 200)
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if (APPLE)
    # Keep external framework search paths when installing a non-bundle executable. This
    # also makes repeated installs safe instead of trying to delete the build rpath twice.
    set(CMAKE_INSTALL_RPATH_USE_LINK_PATH TRUE)
endif()

{cmake[new(IncludeDirs) { Distinct = true, Content = [$"include_directories({Hpp})"] }]}

find_package(Qt6 6.6 REQUIRED COMPONENTS
    Core CorePrivate
    Gui
    Qml
    Quick
    {cmake[new(Packages)]}
)

qt_standard_project_setup(REQUIRES 6.6)

qt_add_executable({Root.MFn(Target)}
    {cmake[sourceFiles = new(SourceFiles)]}
)

{(Root.Assembly.QmlRootModule() is not { Length: > 0 } rootModule ? "" : $@"{Wrap}
qt_add_qml_module({Root.MFn(Target)}
    URI {rootModule}
    VERSION {Root.MFn(Version)}
    SOURCES
        {cmake[qmlElementSourceFiles = new(QmlElementSourceFiles)]}
)")}

{resourceSpec}

target_link_libraries({Root.MFn(Target)} PRIVATE
    Qt6::Core Qt::CorePrivate
    Qt6::Gui
    Qt6::Qml
    Qt6::Quick
    {cmake[new(Libraries)]}
)

install(TARGETS {Root.MFn(Target)}
    BUNDLE DESTINATION .
    LIBRARY DESTINATION .
    RUNTIME DESTINATION .
)

qt_generate_deploy_app_script(
    TARGET {Root.MFn(Target)}
    OUTPUT_SCRIPT deploy_script
    NO_UNSUPPORTED_PLATFORM_ERROR
)
install(SCRIPT ${{deploy_script}})

add_custom_command(
  TARGET {Root.MFn(Target)}
  POST_BUILD
  COMMAND ${{CMAKE_COMMAND}}
  ARGS -E copy $<TARGET_FILE:{Root.MFn(Target)}> ../bin
)";
#if DEBUG
            cmake += $@"
file(GENERATE OUTPUT ALL_BUILD.vcxproj.user
    CONTENT ""<?xml version=\""1.0\"" encoding=\""utf-8\""?>
<Project ToolsVersion=\""Current\"" xmlns=\""http://schemas.microsoft.com/developer/msbuild/2003\"">
  <PropertyGroup>
    <LocalDebuggerWorkingDirectory>$([System.IO.Path]::GetFullPath('$(TargetDir)../bin/'))</LocalDebuggerWorkingDirectory>
    <LocalDebuggerCommand>$(LocalDebuggerWorkingDirectory){Root.MFn(Target)}.exe</LocalDebuggerCommand>
    <DebuggerFlavor>WindowsLocalDebugger</DebuggerFlavor>
    <LocalDebuggerDebuggerType>NativeWithManagedCore</LocalDebuggerDebuggerType>
  </PropertyGroup>
</Project>""
    TARGET {Root.MFn(Target)}
)";
#endif
            Debug.Assert(sourceFiles != null);
            if (qmlElementSourceFiles == null)
                sourceFiles.CreateAlias(Root, QmlElementSourceFiles);
            return Ok;
        }

        private static (List<string> Collisions, string Spec) BuildResourceSpec()
        {
            var groups = Root.Assembly.QtResourcesWithReferences(SourceGraph)
                .Where(resource => string.Equals(resource.AccessMode, "Default",
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(resource.AccessMode, "ManagedAndNative",
                        StringComparison.OrdinalIgnoreCase))
                .Where(resource => resource.SourcePath is { Length: > 0 }
                    && resource.Alias is { Length: > 0 })
                .GroupBy(resource => resource.Alias, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();

            var collisions = groups
                .Where(group => group
                    .Select(r => r.SourcePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() > 1)
                .Select(group =>
                {
                    var sources = group
                        .Select(r => r.SourcePath
                            + (r.AssemblyId is { Length: > 0 } ? $" ({r.AssemblyId})" : ""))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(s => s, StringComparer.Ordinal);
                    return $"Duplicate resource alias '{group.Key}':{Environment.NewLine}"
                        + string.Join(Environment.NewLine, sources.Select(s => "  " + s));
                })
                .ToList();

            if (collisions.Count > 0)
                return (collisions, string.Empty);

            var resources = groups.Select(g => g.First()).ToArray();

            if (resources.Length == 0)
                return ([], string.Empty);

            var builder = new StringBuilder();
            foreach (var resource in resources) {
                var sourcePath = CMakePath(resource.SourcePath);
                var alias = CMakePath(resource.Alias.TrimStart('/', '\\'));
                builder.AppendLine($"""
set_source_files_properties("{sourcePath}" PROPERTIES
    QT_RESOURCE_ALIAS "{alias}"
)
""");
            }

            builder.AppendLine($"""
qt_add_resources({Root.MFn(Target)} qtbridge_resources
    PREFIX "/"
    FILES
""");

            foreach (var resource in resources)
                builder.AppendLine($"        \"{CMakePath(resource.SourcePath)}\"");

            builder.Append(')');
            return ([], builder.ToString());
        }

        private static string CMakePath(string path)
        {
            return path.Replace('\\', '/').Replace("\"", "\\\"");
        }
    }
}
