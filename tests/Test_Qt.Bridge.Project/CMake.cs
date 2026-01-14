// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Test_Qt.Bridge.Project
{
    internal static class CMake
    {
        public static string InjectQtSourcesTargets(params string[] extraSources)
        {
            var sources = string.Join(";", extraSources);

            const string xmlTemplate = """
               <UsingTask TaskName="PatchQtDotNetCMake"
                        TaskFactory="CodeTaskFactory"
                        AssemblyFile="$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll">
               <ParameterGroup>
                 <CMakePath ParameterType="System.String" Required="true" />
                 <ExtraSources ParameterType="System.String" Required="true" />
               </ParameterGroup>
               <Task>
                 <Reference Include="System.Core" />
                 <Using Namespace="System" />
                 <Using Namespace="System.IO" />
                 <Using Namespace="System.Linq" />
                 <Code Type="Fragment" Language="cs"><![CDATA[
                     var lines = File.ReadAllLines(CMakePath).ToList();
                     var extra = ExtraSources.Split(new[] { ';' },
                        StringSplitOptions.RemoveEmptyEntries);

                     for (int i = 0; i < lines.Count; ++i) {
                         var trimmed = lines[i].TrimStart();
                         if (!trimmed.StartsWith("qt_add_executable(", StringComparison.Ordinal))
                             continue;

                         int j = i + 1;
                         while (j < lines.Count
                            && !lines[j].TrimStart().StartsWith(")", StringComparison.Ordinal)) {
                             j++;
                         }
                         if (j >= lines.Count)
                             break;

                         var indent = new string(lines[i].TakeWhile(char.IsWhiteSpace).ToArray())
                            + "    ";

                         foreach (var src in extra)
                             lines.Insert(j++, indent + src);
                         break;
                     }

                     File.WriteAllLines(CMakePath, lines);
                 ]]></Code>
               </Task>
             </UsingTask>

             <Target Name="InjectQtQuickTestSources"
                     AfterTargets="QtBridgeGenerate">
               <PropertyGroup>
                 <QtDotNetNativeDir>$(ProjectIntermediateDir)qt\native\source\</QtDotNetNativeDir>
               </PropertyGroup>
               <PatchQtDotNetCMake
                   CMakePath="$(QtDotNetNativeDir)CMakeLists.txt"
                   ExtraSources="__EXTRA_SOURCES__" />
             </Target>
           """;

            return xmlTemplate.Replace("__EXTRA_SOURCES__", sources);
        }
    }
}
