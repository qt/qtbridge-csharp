// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils.Text;

namespace Qt.Quick
{
    /// <summary>
    /// Normalizes .NET and QML identifiers to the casing conventions used by QML.
    /// </summary>
    public static class QmlNaming
    {
        /// <summary>
        /// Converts a .NET-style property name such as <c>"DisplayName"</c> to the lower-camel
        /// form expected by QML property and role names, such as <c>"displayName"</c>.
        /// </summary>
        /// <remarks>
        /// QML requires property names to begin with a lower-case letter and contain only letters,
        /// numbers and underscores. See
        /// <see href="https://doc.qt.io/qt-6/qtqml-syntax-objectattributes.html#:~:text=Property%20names%20must%20begin%20with,of%20the%20property%20being%20declared.">
        /// QML Object Attributes</see>.
        /// </remarks>
        public static string ToQmlPropertyName(this string text)
            => text.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel);

        /// <summary>
        /// Converts a QML-style property or role name such as <c>"displayName"</c> to the Pascal
        /// case typically used by .NET property names, such as <c>"DisplayName"</c>.
        /// </summary>
        public static string ToDotNetPropertyName(this string text)
            => text.NormalizeQmlTypeName();

        /// <summary>
        /// Normalizes a QML type name to the PascalCase form required for QML object types.
        /// For example, <c>"mainWindow"</c> becomes <c>"MainWindow"</c>.
        /// </summary>
        /// <remarks>
        /// QML requires type names to begin with an upper-case letter. See
        /// <see href="https://doc.qt.io/qt-6/qtqml-documents-definetypes.html#naming-custom-qml-object-types">
        /// Naming Custom QML Object Types</see>.
        /// </remarks>
        public static string NormalizeQmlTypeName(this string text)
            => text.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal);
    }
}
