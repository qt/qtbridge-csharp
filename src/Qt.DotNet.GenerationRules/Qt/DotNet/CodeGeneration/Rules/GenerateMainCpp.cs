/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules
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
                Content = new()
                {
"#include <QtDebug>",
"#include <QDir>",
"#include <QFile>",
"#include <QGuiApplication>",
"#include <QQmlApplicationEngine>",
"#include <QThread>",
"#include <QTimer>",
"#include <QDotNetHost>",
"#include <QDotNetAdapter>",
"#include <QDotNetRef>",
"#include <QDotNetStatic>"
                }
            }
]}

#define EMBED_HASH_HI_PART_UTF8 ""c3ab8ff13720e8ad9047dd39466b3c89""
#define EMBED_HASH_LO_PART_UTF8 ""74e592c2fa383d4a3960714caef0c4f2""
#define EMBED_HASH_FULL_UTF8 (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8)
constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]);
constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025);
static char appName[EMBED_MAX] = EMBED_HASH_FULL_UTF8;
static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

int main(int argc, char *argv[])
{{
    QGuiApplication app(argc, argv);
    auto assemblyPath = QDir(QCoreApplication::applicationDirPath()).filePath(appName);
    if (!QFile::exists(assemblyPath)) {{
        qCritical() << ""App assembly not found: "" << assemblyPath;
        return -1;
    }}

    QDotNetHost dotNetHost;
    QStringList args;
    for (int i = 0; i < argc; ++i)
        args << argv[i];
    auto *dotnetThread = QThread::create(
        [&args, &app, &dotNetHost, &assemblyPath]()
        {{
            dotNetHost.loadApp(assemblyPath, args);
            int result = dotNetHost.runApp();
            app.exit(result);
        }});
    dotnetThread->start();

    int tries = 0;
    constexpr int maxTries = 10000; // ~1 sec total
    while (!dotNetHost.isReady() && tries++ < maxTries)
        QThread::usleep(100);
    if (!dotNetHost.isReady()) {{
        qCritical() << "".NET host not ready after timeout."";
        return -2;
    }}

    QQmlApplicationEngine qmlEngine;
    QDotNetAdapter::instance().init(
        QDir(QCoreApplication::applicationDirPath()).filePath(""Qt.DotNet.Adapter.dll""),
        ""Qt.DotNet.Adapter"", ""Qt.DotNet.Adapter"", &dotNetHost, &qmlEngine);

    {mainCpp[new(MainBeforeAppExec) { Sorted = false }]}

    return app.exec();
}}
";
            return Ok;
        }
    }
}
