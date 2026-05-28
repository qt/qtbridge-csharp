// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CodeGeneration
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
        ConvertHeader,
        ConvertSource
            ,
        ObjectDispatchHeader,
        ObjectDispatchSource
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
                    , QObjectBaseClass
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
                    , PublicCtors
                    , PrivateCtor
                    , Initializer
                    , PrivateDtor
                    , PublicDtor
                    , EventSubscribers
                    , EventUnsubscribers
                    , EventHandlers
                    , PropertyNotifiers
                    , MethodsImplementation
    }
}
