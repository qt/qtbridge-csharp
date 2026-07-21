// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using Qt.MetaObject;

namespace Qt
{
    /// <summary>
    /// Excludes one or more types from Qt Bridge code generation for the annotated assembly.
    /// </summary>
    /// <remarks>
    /// Apply this attribute at assembly scope to remove exact types, external types, or generic
    /// type definitions from the generated bridge surface. When <see cref="Inherited"/> is set to
    /// <see langword="true"/>, derived classes and interface implementers are excluded as well.
    /// If another generated type references an excluded type through a property, field, parameter,
    /// return value, or event payload, that member is excluded too.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoreTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with one or more types to exclude.
        /// </summary>
        /// <param name="excludedTypes">
        /// Exact runtime types or open generic type definitions to exclude from generation.
        /// </param>
        public IgnoreTypeAttribute(params Type[] excludedTypes)
        { }

        /// <summary>
        /// Initializes the attribute with one or more type names to exclude.
        /// </summary>
        /// <param name="excludedTypeNames">
        /// Assembly-qualified or fully-qualified type names to exclude. This is useful when the
        /// target type cannot be referenced directly in source code.
        /// </param>
        public IgnoreTypeAttribute(params string[] excludedTypeNames)
        { }

        /// <summary>
        /// Gets or sets whether the exclusion also applies to derived types and implementers.
        /// </summary>
        public bool Inherited { get; set; } = false;
    }

    /// <summary>
    /// Excludes a type or member declared in source code from Qt Bridge code generation.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to a type to remove the entire type from the generated surface, or to
    /// a constructor, method, property, field, or event to remove only that member. A type-level
    /// <see cref="IncludeAttribute"/> overrides this attribute. Member-level exclusion is local to
    /// the annotated member and is not inherited by overrides.
    /// </remarks>
    [AttributeUsage(TypeAttributeTarget | MemberAttributeTarget, AllowMultiple = false)]
    public class IgnoreAttribute : Attribute
    {
        private const AttributeTargets TypeAttributeTarget
            = AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Interface
            | AttributeTargets.Enum
            | AttributeTargets.Delegate;
        private const AttributeTargets MemberAttributeTarget
            = AttributeTargets.Constructor
            | AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field
            | AttributeTargets.Event;
    }

    /// <summary>
    /// Explicitly includes a type in Qt Bridge code generation.
    /// </summary>
    /// <remarks>
    /// Use this attribute as a type-level opt-in when a type would otherwise be filtered out, for
    /// example by <see cref="IgnoreTypeAttribute"/> or <see cref="IgnoreAttribute"/>. This
    /// attribute is only valid on types.
    /// </remarks>
    [AttributeUsage(TypeAttributeTarget, AllowMultiple = false)]
    public class IncludeAttribute : Attribute
    {
        private const AttributeTargets TypeAttributeTarget
            = AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Interface
            | AttributeTargets.Enum
            | AttributeTargets.Delegate;
    }

    /// <summary>
    /// Internal member-level opt-in used by bridge-owned API types.
    /// </summary>
    /// <remarks>
    /// This attribute is not part of the public API. The generator treats it as a member-scoped
    /// form of <see cref="IncludeAttribute"/> for selected constructors, methods, properties,
    /// fields, and events declared by the bridge itself.
    /// </remarks>
    [AttributeUsage(MemberAttributeTarget, AllowMultiple = false)]
    internal class EnableAttribute : IncludeAttribute
    {
        private const AttributeTargets MemberAttributeTarget
            = AttributeTargets.Constructor
            | AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field
            | AttributeTargets.Event;
    }

    /// <summary>
    /// Supplies assembly-level generation metadata consumed by Qt Bridge templates.
    /// </summary>
    /// <remarks>
    /// The values of this attribute are exposed to the generator as placeholders and are intended
    /// for advanced generation customization.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class GenerateAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets text inserted into the main translation unit include section.
        /// </summary>
        public string MainIncludes { get; set; }

        /// <summary>
        /// Text inserted at the start of main(), before the QApplication object is created.
        /// </summary>
        public string MainStartingUp { get; set; }

        /// <summary>
        /// Gets or sets text inserted before the application event loop is entered.
        /// </summary>
        public string MainBeforeAppExec { get; set; }

        /// <summary>
        /// Gets or sets additional package identifiers required by the generated output.
        /// </summary>
        public string Packages { get; set; }

        /// <summary>
        /// Gets or sets additional native libraries required by the generated output.
        /// </summary>
        public string Libraries { get; set; }
    }

    /// <summary>
    /// Type export options bitmask indices
    /// </summary>
    internal enum Option
    {
        /// <summary>
        /// Custom type export; next bit determines export kind
        /// </summary>
        Export,

        /// <summary>
        /// Custom export kind: 0=Metadata, 1=Source-code
        /// </summary>
        ExportAs
    }

    /// <summary>
    /// Flags that describe how Qt Bridge exposes a managed type to QML
    /// </summary>
    [Flags]
    public enum Options
    {
        /// <summary>
        /// Use the built-in default export mode
        /// </summary>
        Default = 0,

        /// <summary>
        /// Indicates that export behavior is explicitly configured
        /// </summary>
        Export = 1 << Option.Export,

        /// <summary>
        /// Export by generating bridge wrapper code
        /// </summary>
        ExportAsSourceCode = 1 << Option.ExportAs,
    }


    /// <summary>
    /// Export mode values for <see cref="ExportAttribute"/>
    /// </summary>
    /// <remarks>
    /// Qt Bridge can expose a managed type to QML using different export modes. These
    /// values are intended for use with <see cref="ExportAttribute.Options"/>:
    /// <list type="bullet">
    /// <item>
    /// <see cref="Metadata"/> selects metadata-based export, where the type is described through
    /// metadata consumed by the bridge at runtime.
    /// </item>
    /// <item>
    /// <see cref="SourceCode"/> selects wrapper-based export, where the bridge prepares code for
    /// that type.
    /// </item>
    /// <item>
    /// <see cref="Default"/> resets a type-level export declaration to the built-in Qt Bridge
    /// default instead of inheriting an assembly-level <see cref="ExportAttribute"/>.
    /// </item>
    /// </list>
    /// </remarks>
    public static class ExportAs
    {
        /// <summary>
        /// Use the built-in default export mode
        /// </summary>
        /// <remarks>
        /// When applied explicitly on a type, this resets export behavior to the built-in default
        /// chosen by Qt Bridge. It does not inherit an assembly-level <see cref="ExportAttribute"/>
        /// setting.
        /// </remarks>
        public const Options Default = Options.Default;

        /// <summary>
        /// Export the type as metadata
        /// </summary>
        public const Options Metadata = Options.Export;

        /// <summary>
        /// Export the type through generated bridge wrapper code
        /// </summary>
        public const Options SourceCode = Options.Export | Options.ExportAsSourceCode;
    }

    /// <summary>
    /// Configures how Qt Bridge exposes a managed type to QML
    /// </summary>
    /// <remarks>
    /// Qt Bridge can expose a managed type either through wrapper-based export or through
    /// metadata-based export used during runtime registration.
    ///
    /// This attribute can be applied at assembly scope to define the default export mode for types
    /// in that assembly, or at type scope to override that behavior for an individual type.
    ///
    /// Precedence is:
    /// <list type="number">
    /// <item>
    /// If a type has no <see cref="ExportAttribute"/>, it inherits any assembly-level export
    /// setting.
    /// </item>
    /// <item>
    /// If a type explicitly sets <see cref="ExportAs.Metadata"/> or
    /// <see cref="ExportAs.SourceCode"/>, that type-level choice overrides the assembly-level
    /// setting.
    /// </item>
    /// <item>
    /// If a type explicitly sets <see cref="ExportAs.Default"/>, it resets to the built-in Qt
    /// Bridge default rather than inheriting the assembly-level setting.
    /// </item>
    /// </list>
    /// </remarks>
    [AttributeUsage(TypeAttributeTarget, AllowMultiple = false)]
    public class ExportAttribute : Attribute
    {
        private const AttributeTargets TypeAttributeTarget
            = AttributeTargets.Assembly
            | AttributeTargets.Class
            | AttributeTargets.Struct
            | AttributeTargets.Interface
            | AttributeTargets.Enum
            | AttributeTargets.Delegate;

        /// <summary>
        /// Custom export options
        /// </summary>
        /// <value>Bitmask of <see cref="Options"/> flags</value>
        public Options Options { get; set; }
    }
}
