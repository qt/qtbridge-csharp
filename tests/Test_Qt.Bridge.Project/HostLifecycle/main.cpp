// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QMap>
#include <QTest>

#include <qdotnetstatic.h>

#include "QtTestSetupBase.h"

class Test_HostLifecycle : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

private:
    [[nodiscard]] QString programTypeName() const
    {
        return QString("%1, %2").arg("HostLifecycle.Program", assemblyName);
    }

private slots:
    void initTestCase()
    {
        initHost();
        QVERIFY2(locateAssembly(), "Managed test assembly not found");
    }

    void loadHost()
    {
        QVERIFY(!dotNetHost->isLoaded());
        QVERIFY2(dotNetHost->load(), "Failed to load .NET runtime");
        QVERIFY(dotNetHost->isLoaded());
    }

    void runtimeProperties()
    {
        QVERIFY(dotNetHost->isLoaded());

        const QMap<QString, QString> properties = dotNetHost->runtimeProperties();
        QVERIFY2(!properties.isEmpty(), "Expected non-empty runtime properties");
    }

    void unloadHost()
    {
        QVERIFY(dotNetHost->isLoaded());

        QtTestSetupBase::unloadHost();

        QVERIFY(!dotNetHost->isLoaded());
    }

    void appStartup()
    {
        // The old suite covered host-load and app-host lifecycle in separate native variants.
        // Recreate the host here so the app path starts from a fresh hostfxr context.
        dotNetHost.reset(new QDotNetHost());
        dotnetThread = nullptr;
        dotNetExited = false;
        dotNetResult = -1;

        QVERIFY(!dotNetHost->isLoaded());

        runAppAsynchronous();

        const auto readyResult = waitForReady();
        QCOMPARE(readyResult, ReadyResult::Ok);
        QVERIFY(dotNetHost->isReady());

        QVERIFY2(QtTestSetupBase::initAdapter(nullptr, false),
            "Failed to initialize Qt/.NET Adapter");
        QVERIFY(QDotNetAdapter::instance().isValid());
    }

    void appShutdown()
    {
        QVERIFY(dotNetHost->isLoaded());
        QVERIFY(dotNetHost->isReady());

        QtDotNet::call<void, bool>(programTypeName(), "set_KeepRunning", false);

        const auto finalizeResult = finalizeThreaded(3000);
        QCOMPARE(finalizeResult, FinalizeResult::Ok);
        QVERIFY(dotNetExited);
        QVERIFY(!dotNetHost->isLoaded());
    }

    void cleanupTestCase()
    {
        if (dotNetHost && dotNetHost->isLoaded()) {
            if (dotnetThread && !dotNetExited)
                std::ignore = finalizeThreaded(3000);
            else
                QtTestSetupBase::unloadHost();
        }
    }
};

QTEST_MAIN_WITH_DOTNET_SETUP(Test_HostLifecycle)
#include "main.moc"
