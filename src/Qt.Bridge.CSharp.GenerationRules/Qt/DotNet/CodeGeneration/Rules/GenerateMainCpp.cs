// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules
{
    using MetaFunctions;
    using static Placeholders;
    using static Traits;

    public class GenerateMainCpp : GenerateBuildSpec
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo _)
        {
            var mainPath = "cpp/main.cpp";

            if (Root.GetPlaceholder(SourceFiles) is not { } sourceFiles)
                return Error();
            sourceFiles += mainPath;

            var mainCpp = new FilePlaceholder(MainCpp, Root, $@"{Root.MFn(Dir)}{mainPath}");
            mainCpp += $@"

{mainCpp[new(MainIncludes)
            {
                Distinct = true,
                Content = new[]
                {
"#include <QtDebug>",
"#include <QDir>",
"#include <QFile>",
"#include <QFileInfo>",
"#include <QGuiApplication>",
"#include <QQmlApplicationEngine>",
"#include <QQmlDebuggingEnabler>",
"#include <QThread>",
"#include <QTimer>",
"#include <QDotNetHost>",
"#include <QDotNetAdapter>",
"#include <QDotNetRef>",
"#include <QDotNetStatic>",
"#include <QDotNetConvert>",
"#include <object_dispatch.h>",
"#include <metadata_loader.h>",
"#include <qml_register_types.h>"
                }
            }
]}

QT_DOTNET_HOST(appName);

int main(int argc, char *argv[])
{{
    QDotNetConvert::setDispatch(QtDotNet::objectDispatch);
    auto appDirPath = QFileInfo(argv[0]).absoluteDir().path();
    QtDotNet::loadTypeMetadata(appDirPath, qml_register_types);

    {mainCpp[new(MainStartingUp) { Sorted = false }]}

    for (int i = 1; i < argc; ++i) {{
        if (QString::fromLocal8Bit(argv[i]).startsWith(""-qmljsdebugger="")) {{
            QQmlDebuggingEnabler::enableDebugging(true);
            break;
        }}
    }}

    QGuiApplication app(argc, argv);
    auto assemblyPath = QDir(appDirPath).filePath(appName);
    if (!QFile::exists(assemblyPath)) {{
        assemblyPath = QDir(appDirPath)
            .filePath(""{Root.Assembly.GetName().Name}.dll"");
    }}
    if (!QFile::exists(assemblyPath)) {{
        qCritical() << ""App assembly not found: "" << assemblyPath;
        return -1;
    }}

    QDotNetHost dotNetHost;
    QStringList args;
    for (int i = 0; i < argc; ++i)
        args << argv[i];
    int dotNetResult = -1;
    bool dotNetExited = false;
    auto *dotnetThread = QThread::create(
        [&args, &app, &dotNetHost, &assemblyPath, &dotNetResult, &dotNetExited]()
        {{
            dotNetHost.loadApp(assemblyPath, args);
            dotNetResult = dotNetHost.runApp();
            dotNetExited = true;
            app.exit(dotNetResult);
        }});
    dotnetThread->start();

    int tries = 0;
    constexpr int maxTries = 10000; // ~1 sec total
    while (!dotNetExited && !dotNetHost.isReady() && tries++ < maxTries)
        QThread::usleep(100);
    if (dotNetExited)
        return dotNetResult;
    if (!dotNetHost.isReady()) {{
        qCritical() << "".NET host not ready after timeout."";
        return -2;
    }}

    QQmlApplicationEngine qmlEngine;
    QDotNetAdapter::instance().init(
        QDir(appDirPath).filePath(""Qt.DotNet.Adapter.dll""),
        ""Qt.DotNet.Adapter"", ""Qt.DotNet.Adapter"", &dotNetHost, &qmlEngine);

    QtDotNet::call<void>(""Qt.DotNet.Adapter, Qt.DotNet.Adapter"", ""SetMainThread"");

    {mainCpp[new(MainBeforeAppExec) { Sorted = false }]}
    QObject::connect(&app, &QGuiApplication::aboutToQuit, &qmlEngine, &QQmlEngine::quit);

    if (dotNetExited)
        return dotNetResult;

    auto res = app.exec();
    dotnetThread->wait();
#ifdef Q_OS_WINDOWS
    ExitProcess(res);
#endif
    return res;
}}
";
            return Ok;
        }
    }
}
