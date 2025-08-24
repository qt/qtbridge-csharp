/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet.CodeGeneration
{
    public enum Placeholders
    {
        BuildSpecFile
            , IncludeDirs
            , Packages
            , SourceFiles
            , QmlElementSourceFiles
            , QmlFiles
            , Libraries
            ,
        BuiltInTypes
            ,
        EventDispatchHeader,
        EventDispatchSource
            ,
        MainCpp
            , MainIncludes
            , MainBeforeAppExec
            ,
        HppFile
            , ForwardDecl
            , ForwardDeclBaseOf
            , ForwardDeclTypeOf
            , ForwardDecl3rdParty
            , Includes
            , ForwardDeclPrivate
            , PublicDeclarationsGroup
                , PublicDeclarations
                    , BaseClasses
                    , TypeTraits
                    , CtorDeclarations
                    , PropertyDeclarations
                    , MethodDeclarations
                    , SignalDeclarations
            ,
        CppFile
            , PrivateIncludes
            , PrivateDeclarationsGroup
                , PrivateDeclarations
                    , PrivateMemberDeclarations
            , ImplementationGroup
                , QDotNetObjectImpl
                , Implementation
                    , EventSubscribers
                    , EventUnsubscribers
                    , EventHandlers
                    , PropertyNotifiers
    }
}
