// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.Diagnostics;
using System.Reflection;
using Qt.Quick;

namespace ColorPalette
{
    internal class Program
    {
        private static bool UseServer { get; set; } = true;

        private static Process Server { get; set; }

        private static void StartServer()
        {
            if (!UseServer || Server != null)
                return;
            try {
                var serverPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location),
                    "server", "colorpaletteserver.exe");
                if (!File.Exists(serverPath))
                    return;
                Server = Process.Start(new ProcessStartInfo()
                {
                    FileName = serverPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
            } catch (Exception e) {
                Debug.WriteLine($"Error starting server: {e.Message}");
            }
        }

        private static void StopServer()
        {
            if (Server == null)
                return;
            Server.Kill();
            Server.WaitForExit();
        }

        public static void Main(string[] args)
        {
            if (args.Contains("--log-requests"))
                RestService.LogRequests = true;
            if (args.Contains("--no-server"))
                UseServer = false;

            PaginatedResource.RegisterResourceType<ColorResource>();
            PaginatedResource.RegisterResourceType<UserResource>();

            StartServer();

            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();

            StopServer();
        }
    }
}
