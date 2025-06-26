/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
{
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
            var cmake = new FilePlaceholder(
                BuildSpecFile, Root, $@"{Root.MFn(Dir)}/CMakeLists.txt");
            cmake += $@"

cmake_minimum_required(VERSION 3.16)

project({Root.MFn(Target)} VERSION {Root.MFn(Version)} LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include_directories({Hpp})

find_package(Qt6 6.6 REQUIRED COMPONENTS
    Core
    Gui
    {cmake[new(Packages)]}
)

qt_standard_project_setup(REQUIRES 6.6)

qt_add_executable({Root.MFn(Target)}
    {cmake[new(SourceFiles)]}
)

{cmake[new(QmlModules)]}
target_link_libraries({Root.MFn(Target)} PRIVATE
    Qt6::Core
    Qt6::Gui
    {cmake[new(Libraries)]}
)";
            return Ok;
        }
    }
}
