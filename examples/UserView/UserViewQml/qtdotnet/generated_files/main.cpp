/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include <QGuiApplication>
#include <QQmlApplicationEngine>
#include <QQmlContext>
#include <QThread>
#include <QFile>
#include <QDotNetStatic>

#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89" // SHA-256 of "foobar" in UTF-8
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8    (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8) // NUL terminated

constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]);
constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025); // 1024 DLL name length, 1 NUL

// Contains the EMBED_HASH_FULL_UTF8 value at compile time or the managed DLL name replaced by "dotnet build".
// Must not be 'const' because std::string(&embed[0]) below would bind to a const string ctor plus length
// where length is determined at compile time (=64) instead of the actual length of the string at runtime.
static char appName[EMBED_MAX] = EMBED_HASH_FULL_UTF8;     // series of NULs followed by embed hash string

static const char hi_part[] = EMBED_HASH_HI_PART_UTF8;
static const char lo_part[] = EMBED_HASH_LO_PART_UTF8;

int main(int argc, char *argv[])
{
    qInfo() << "App name" << appName;

    QGuiApplication app(argc, argv);
    auto assemblyPath = QDir(QCoreApplication::applicationDirPath()).filePath(appName);
    if (!QFile::exists(assemblyPath)) {
        qInfo() << "App assembly not found: " << assemblyPath;
        return -1;
    }

    QDotNetHost dotNetHost;
    auto *dotnetThread = QThread::create(
        [argc, argv, &app, &dotNetHost, &assemblyPath]()
        {
            dotNetHost.loadApp(assemblyPath);
            int result = dotNetHost.runApp();
            app.exit(result);
        });
    dotnetThread->start();

    while (!dotNetHost.isReady())
        QThread::usleep(100);

    QQmlApplicationEngine qmlEngine;
    QDotNetAdapter::instance().init(
        QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"),
        "Qt.DotNet.Adapter", "Qt.DotNet.Adapter", &dotNetHost, &qmlEngine);

    return app.exec();
}
