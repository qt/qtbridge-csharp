// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.Bridge.Utils.Text;

namespace Qt.Quick
{
    public static class QmlNaming
    {
        public static string ToQmlPropertyName(this string text)
            => text.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel);

        public static string ToDotNetPropertyName(this string text)
            => text.NormalizeQmlTypeName();

        public static string NormalizeQmlTypeName(this string text)
            => text.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal);
    }
}
