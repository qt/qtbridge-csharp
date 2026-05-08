// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Quick;
using App = Qt.Application;

namespace Bookshelf
{
    internal static class Program
    {
        private static readonly System.Resources.ResourceManager ManagedResources =
            new("Bookshelf.App", typeof(Program).Assembly);

        private static void Main(string[] args)
        {
            // Load the app metadata from the managed resource.
            App.Name = GetManagedString("ApplicationName", "Bookshelf");
            App.Version = GetManagedString("ApplicationVersion", "1.0");
            App.OrganizationName = GetManagedString("OrganizationName", "The Qt Company");
            App.OrganizationDomain = GetManagedString("OrganizationDomain", "qt.io");
            App.DisplayName = GetManagedString("DisplayName", "Bookshelf");

            // Load the app icon from the packaged qrc:/ resource.
            App.SetWindowIcon("qrc:/assemblies/Bookshelf/icons/app.svg");

            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();
        }

        private static string GetManagedString(string key, string fallback)
        {
            return ManagedResources.GetString(key) ?? fallback;
        }
    }
}
