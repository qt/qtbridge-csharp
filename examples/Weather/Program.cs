// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt;
using Qt.Quick;

namespace Weather
{
    internal static class Program
    {
        private static void Main()
        {
            Application.Name = "Weather";
            Application.Version = "1.0";
            Application.OrganizationName = "The Qt Company";
            Application.OrganizationDomain = "qt.io";
            Application.DisplayName = "Weather";

            Qml.LoadFromModule("Weather", "Main");
            Qml.WaitForExit();
        }
    }
}
