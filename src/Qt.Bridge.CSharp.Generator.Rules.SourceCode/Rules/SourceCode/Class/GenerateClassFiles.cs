// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateClassFiles : GenerateBuildSpec
    {
        private readonly object criticalSection = new();

        public override int Priority => base.Priority + 1;

        public override bool Matches(MemberInfo src)
            => src is Type { IsEnum: false } type && !src.IsRootNode()
                && type.ExportAsSourceCode() && !type.IsStaticClass()
                && !type.IsAssignableTo(TypeOf<Delegate>());

        public override Result Execute(MemberInfo src)
        {
            if (!StatusFile.CheckIn(src, nameof(GenerateClassFiles)))
                return Ok;

            if (src is not Type type)
                return Error();

            var contributesQmlTypeInfo = type.IsQmlElement()
                || type.IsAssignableTo(TypeOf<global::Qt.Bridge.Models.Model>());
            var sourceFilesPlaceholder = contributesQmlTypeInfo ? QmlElementSourceFiles : SourceFiles;
            if (Root.GetPlaceholder(sourceFilesPlaceholder) is not { } sourceFiles)
                return Error();

            var baseType = type switch
            {
                { IsGenericType: true } => type.GetGenericTypeDefinition(),
                _ => type
            };

            var hppPath = $"{HppDir}{baseType.MFn(Ns | Dir)}{baseType.MFn(File)}.h";
            var cppPath = $"{CppDir}{baseType.MFn(Ns | Dir)}{baseType.MFn(File)}.cpp";

            Placeholder includes, publicDecl, privateDecl, implementation;
            includes = publicDecl = privateDecl = implementation = null;

            lock (criticalSection) {

                ////////////////////////////////////////////////////////////////////////////////////
                // Header file
                //
                if (baseType.GetPlaceholder(HppFile) is not { } hppFile) {
                    sourceFiles.AddText(hppPath);
                    hppFile = new FilePlaceholder(HppFile, baseType, $"{Root.MFn(Dir)}{hppPath}");

                    hppFile += $@"
#pragma once
#include <builtin_types.h>

namespace {baseType.MFn(Ns)}
{{
    {hppFile[new(ForwardDecl) { Distinct = true }]}
}}

{hppFile[new(ForwardDeclBaseOf) { Distinct = true }]}
{hppFile[new(ForwardDeclTypeOf) { Distinct = true }]}
{hppFile[new(ForwardDecl3rdParty) { Distinct = true }]}
{hppFile[includes = new(Includes) { Distinct = true }]}

namespace {baseType.MFn(Ns)}
{{
    {hppFile[new(ForwardDeclPrivate) { Distinct = true }]}
}}

{hppFile[publicDecl = new(PublicDeclarationsGroup)]}
";
                }
                //
                // Header file
                ////////////////////////////////////////////////////////////////////////////////////

                ////////////////////////////////////////////////////////////////////////////////////
                // .cpp file
                //
                if (baseType.GetPlaceholder(CppFile) is not { } cppFile) {
                    sourceFiles.AddText(cppPath);
                    hppFile = new FilePlaceholder(CppFile, baseType, $"{Root.MFn(Dir)}{cppPath}");
                    hppFile += $@"
#include <{baseType.MFn(Ns | Dir)}{baseType.MFn(File)}.h>
{hppFile[new(PrivateIncludes) { Distinct = true }]}

{hppFile[privateDecl = new(PrivateDeclarationsGroup)]}
{hppFile[implementation = new(ImplementationGroup)]}
";
                }
                //
                // .cpp file
                ////////////////////////////////////////////////////////////////////////////////////

            } // END criticalSection

            if (baseType != type) {
                baseType.GetPlaceholder(ForwardDecl).CreateAlias(type);
                baseType.GetPlaceholder(ForwardDeclBaseOf).CreateAlias(type);
                baseType.GetPlaceholder(ForwardDeclTypeOf).CreateAlias(type);
                baseType.GetPlaceholder(ForwardDeclPrivate).CreateAlias(type);
                baseType.GetPlaceholder(PrivateIncludes).CreateAlias(type);
                includes = baseType.GetPlaceholder(Includes).CreateAlias(type);
            } else {
                includes ??= type.GetPlaceholder(Includes);
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            // Dependency includes
            //
            var connectedTypes = SourceGraph.Connected(type).Where(t => t.ExportAsSourceCode());
            foreach (var connectedType in connectedTypes)
                includes += $"#include <{connectedType.MFn(Ns | Dir)}{connectedType.MFn(File)}.h>";

            ////////////////////////////////////////////////////////////////////////////////////////
            // Public declarations
            //
            publicDecl ??= baseType.GetPlaceholder(PublicDeclarationsGroup);
            publicDecl += $@"
////////////////////////////////////////////////////////////////////////////////////////////////////
// [{type.MFn(Src | Ns | Name)}]
{publicDecl[new(PublicDeclarations, type)]}
// [{type.MFn(Src | Ns | Name)}]
////////////////////////////////////////////////////////////////////////////////////////////////////
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            // Private declarations
            //
            privateDecl ??= baseType.GetPlaceholder(PrivateDeclarationsGroup);
            privateDecl += $@"
////////////////////////////////////////////////////////////////////////////////////////////////////
// PRIVATE [{type.MFn(Src | Ns | Name)}]

{privateDecl[new(PrivateDeclarations, type)]}
// PRIVATE [{type.MFn(Src | Ns | Name)}]
////////////////////////////////////////////////////////////////////////////////////////////////////
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            // Implementation
            //
            implementation ??= baseType.GetPlaceholder(ImplementationGroup);
            implementation += $@"
////////////////////////////////////////////////////////////////////////////////////////////////////
// IMPLEMENTATION [{type.MFn(Src | Ns | Name)}]

{implementation[new(QDotNetObjectImpl, type)]}
{implementation[new(Implementation, type)]}
// IMPLEMENTATION [{type.MFn(Src | Ns | Name)}]
////////////////////////////////////////////////////////////////////////////////////////////////////
{Blank}";

            return Ok;
        }
    }
}
