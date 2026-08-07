// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;
using Qt.Quick;

namespace Qt.Bridge
{
    /// <summary>
    /// Provides generated QML cast helpers for bridged C# reference types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TypeCast"/> is exposed to QML as a singleton named <c>TypeCast</c>. The class
    /// itself is only a marker in the API assembly. During code generation, the bridge adds one
    /// <c>as_*</c> method for each supported bridged object type.
    /// </para>
    /// <para>
    /// Use these generated methods when QML receives an object reference, and you want to treat it
    /// as a specific bridged C# type. If the runtime object is assignable to the target type, the
    /// method returns the typed wrapper. Otherwise, it returns <c>null</c>.
    /// </para>
    /// <para>
    /// For example, a bridged C# type like this:
    /// </para>
    /// <![CDATA[
    /// ```csharp
    /// namespace ColorPalette;
    ///
    /// public class ColorResource
    /// {
    ///     public string Name { get; set; }
    ///     public int ColorId { get; set; }
    /// }
    /// ```
    /// ]]>
    /// <para>
    /// produces a QML cast helper named <c>TypeCast.as_ColorPalette_ColorResource(obj)</c>. In
    /// QML, you call that method with an object reference. If the object actually represents a
    /// <c>ColorPalette.ColorResource</c> instance, the return value can be used as that type.
    /// Otherwise, the result is <c>null</c>.
    /// </para>
    /// <![CDATA[
    /// ```qml
    /// function showColorDetails(obj) {
    ///     let color = TypeCast.as_ColorPalette_ColorResource(obj)
    ///     if (color == null)
    ///         return
    ///
    ///     console.log(color.name)
    ///     console.log(color.colorId)
    /// }
    /// ```
    /// ]]>
    /// <para>
    /// Method names are derived from the C# namespace and type name, with namespace separators
    /// replaced by <c>_</c>.
    /// </para>
    /// </remarks>
    [Include]
    [QmlElement(Singleton = true)]
    [Export(Options = ExportAs.SourceCode)]
    public class TypeCast
    {
        [Enable]
        public TypeCast() { }
    }
}
