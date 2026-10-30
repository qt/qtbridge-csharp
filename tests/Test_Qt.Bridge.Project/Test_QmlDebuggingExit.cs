// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Test_Qt.Bridge.Project.Shared;

namespace Test_Qt.Bridge.Project
{
    [TestClass]
    public sealed class Test_QmlDebuggingExit
    {
        private const string ProgramCs =
            """
                using System.Threading;

                namespace Test_QmlDebuggingExit;

                internal static class Program
                {
                    private static int Main(string[] args)
                    {
                        Thread.Sleep(2000);
                        return 0;
                    }
                }
            """;

        private const string MainQml =
            """
                import QtQuick

                Window {
                    width: 120
                    height: 80
                    visible: true
                }
            """;

        // The host must stop the QML debug server thread before it exits, else Qt fails fast
        // (0xC0000409). Without "block" the app runs without a client. QtGenSourceCode=false
        // uses the prebuilt host from the package.
        [TestMethod]
        [DataRow("true", DisplayName = "Generated host")]
        [DataRow("false", DisplayName = "Prebuilt host")]
        public async Task App_With_Qml_Debugging_Exits_Cleanly(string genSourceCode)
        {
            using var temp = new TempProject();
            temp.Create(new CreationOptions { PackageReferences = [Packages.QtBridge] });
            temp.AddFile("Program.cs", ProgramCs);
            temp.AddFile("Main.qml", MainQml);
            var (buildOk, buildOutput) = await temp.BuildAsync(new BuildOptions {
                Properties = [("QtGenSourceCode", genSourceCode)]
            });
            Assert.IsTrue(buildOk, buildOutput);

            var port = FreePort();
            var (exitCode, stdOut, _) = await temp.RunAsync(new RunOptions {
                Args = [$"-qmljsdebugger=port:{port},host:127.0.0.1,services:V8Debugger"],
                EnvVars = [("QT_FORCE_STDERR_LOGGING", "1")],
                StdErr = Redirect.StdOut,
                Timeout = 60000
            });

            StringAssert.Contains(stdOut, $"Waiting for connection on port {port}");
            Assert.AreEqual(0, exitCode, stdOut);
        }

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
