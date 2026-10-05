// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using Extensions;
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
            if (!StatusFile.CheckIn(Root, nameof(GenerateBuildSpec)))
                return Ok;

            Placeholder sourceFiles = null, qmlElementSourceFiles = null;
            var resourceSpec = BuildResourceSpec();
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

# Deploying Qt on Linux rewrites the RPATH of the copied plugins. CMake can only replace an
# existing RPATH, but distribution packages, such as those of Debian and Ubuntu, ship plugins
# without one, so prefer patchelf, which can also add it. Qt reads these settings when
# find_package(Qt6) sets up deployment, so they must be defined before that call.
if(UNIX AND NOT APPLE AND NOT DEFINED QT_DEPLOY_USE_PATCHELF)
    find_program(QT_DEPLOY_PATCHELF_EXECUTABLE patchelf)
    if(QT_DEPLOY_PATCHELF_EXECUTABLE)
        set(QT_DEPLOY_USE_PATCHELF ON)
    endif()
endif()

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
            Debug.Assert(sourceFiles != null);
            if (qmlElementSourceFiles == null)
                sourceFiles.CreateAlias(Root, QmlElementSourceFiles);
            return Ok;
        }

        private static string BuildResourceSpec()
        {
            var resources = GenerateResources.Resources;
            if (!GenerateResources.Linked || resources.Count == 0)
                return string.Empty;

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
            return builder.ToString();
        }

        private static string CMakePath(string path)
        {
            return path.Replace('\\', '/').Replace("\"", "\\\"");
        }
    }
}
