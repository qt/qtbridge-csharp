// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

// Helpers for language features used by C# records and source-generated code that are not
// present in the .NET Framework 4.7.2 base class library.

namespace System.Runtime.CompilerServices
{
    /// <summary>Required by the C# compiler for init-only properties and records.</summary>
    internal static class IsExternalInit;
}

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Marks an API as experimental. The extensibility SDK source generator emits
    /// <c>[Experimental]</c> on its output; this helper satisfies the compiler on
    /// net472 where the attribute is absent from the BCL.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly
        | AttributeTargets.Module
        | AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Enum
        | AttributeTargets.Constructor
        | AttributeTargets.Method
        | AttributeTargets.Property
        | AttributeTargets.Field
        | AttributeTargets.Event
        | AttributeTargets.Interface
        | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class ExperimentalAttribute(string diagnosticId) : Attribute
    {
        public string DiagnosticId { get; } = diagnosticId;
        public string? UrlFormat { get; set; }
    }
}
