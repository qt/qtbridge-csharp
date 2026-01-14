// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules
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
            var rootModuleUri = Root.Assembly.QmlFiles()
                .Where(x => x.IsRoot)
                .Select(x => x.Uri)
                .FirstOrDefault();
            var hasQmlFiles = !string.IsNullOrEmpty(rootModuleUri);

            var cmake = new FilePlaceholder(
                BuildSpecFile, Root, $@"{Root.MFn(Dir)}/CMakeLists.txt");
            cmake += $@"

cmake_minimum_required(VERSION 3.16)

project({Root.MFn(Target)} VERSION {Root.MFn(Version)} LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

{cmake[new(IncludeDirs) { Distinct = true, Content = [$"include_directories({Hpp})"] }]}

find_package(Qt6 6.6 REQUIRED COMPONENTS
    Core
    Gui
    Qml
    Quick
    {cmake[new(Packages)]}
)

qt_standard_project_setup(REQUIRES 6.6)

qt_add_executable({Root.MFn(Target)}
    {cmake[new(SourceFiles)]}
)

{(!hasQmlFiles ? Wrap : $@"{Wrap}
qt_add_qml_module({Root.MFn(Target)}
    URI {rootModuleUri}
    VERSION {Root.MFn(Version)}
    SOURCES
        {cmake[new(QmlElementSourceFiles)]}
)")}

target_link_libraries({Root.MFn(Target)} PRIVATE
    Qt6::Core
    Qt6::Gui
    Qt6::Qml
    Qt6::Quick
    {cmake[new(Libraries)]}
)

add_custom_command(
  TARGET {Root.MFn(Target)}
  POST_BUILD
  COMMAND ${{CMAKE_COMMAND}}
  ARGS -E copy $<TARGET_FILE:{Root.MFn(Target)}> ../bin
)";
#if DEBUG
            cmake += $@"
add_custom_command(
  TARGET {Root.MFn(Target)}
  POST_BUILD
  COMMAND ${{CMAKE_COMMAND}}
  ARGS -E copy $<TARGET_FILE:{Root.MFn(Target)}> ../../../../../../bin/Debug/net8.0
)
file(GENERATE OUTPUT ALL_BUILD.vcxproj.user
    CONTENT ""<?xml version=\""1.0\"" encoding=\""utf-8\""?>
<Project ToolsVersion=\""Current\"" xmlns=\""http://schemas.microsoft.com/developer/msbuild/2003\"">
  <PropertyGroup>
    <LocalDebuggerWorkingDirectory>$([System.IO.Path]::GetFullPath('$(TargetDir)../../../../../../../../bin/Debug/net8.0/'))</LocalDebuggerWorkingDirectory>
    <LocalDebuggerCommand>$(LocalDebuggerWorkingDirectory){Root.MFn(Target)}.exe</LocalDebuggerCommand>
    <DebuggerFlavor>WindowsLocalDebugger</DebuggerFlavor>
    <LocalDebuggerDebuggerType>NativeWithManagedCore</LocalDebuggerDebuggerType>
  </PropertyGroup>
</Project>""
    TARGET {Root.MFn(Target)}
)";
#endif
            return Ok;
        }
    }
}
