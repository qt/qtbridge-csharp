// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include "QtQuickTestSetup.h"

#include <QtQuickTest>
#include <QCoreApplication>
#include <QDebug>

void QtQuickTestSetup::applicationAvailable()
{
    if (!beforeApplicationAvailable())
        return;

    initHost();
    if (!locateAssembly()) {
        qApp->exit(int(BridgeExitCode::LocateAssemblyFailed));
        return;
    }

    runAppAsynchronous();

    auto readyResult = waitForReady();
    if (!handleReadyResult(readyResult))
        return;

    afterApplicationAvailable();
}

void QtQuickTestSetup::qmlEngineAvailable(QQmlEngine* qmlEngine)
{
    if (!beforeQmlEngineAvailable(qmlEngine))
        return;

    if (!initAdapter(qmlEngine, /*setMainThread*/ true)) {
        qApp->exit(int(BridgeExitCode::InitAdapterFailed));
        return;
    }

    afterQmlEngineAvailable(qmlEngine);
}

void QtQuickTestSetup::cleanupTestCase()
{
    if (!beforeCleanupTestCase())
        return;

    auto finalizeResult = finalizeThreaded(3000);
    if (!handleFinalizeResult(finalizeResult))
        return;

    afterCleanupTestCase();
}

// Default hooks

bool QtQuickTestSetup::beforeApplicationAvailable()
{
    return true;
}

void QtQuickTestSetup::afterApplicationAvailable()
{}

bool QtQuickTestSetup::beforeQmlEngineAvailable(QQmlEngine* /*engine*/)
{
    return true;
}

void QtQuickTestSetup::afterQmlEngineAvailable(QQmlEngine* /*engine*/)
{}

bool QtQuickTestSetup::beforeCleanupTestCase()
{
    return true;
}

void QtQuickTestSetup::afterCleanupTestCase()
{}

bool QtQuickTestSetup::handleReadyResult(ReadyResult result)
{
    switch (result) {
    case ReadyResult::Ok:
        return true;
    case ReadyResult::ExitedEarly:
        qApp->exit(int(BridgeExitCode::WaitForReadyExitedEarly));
        return false;
    case ReadyResult::Timeout:
        qApp->exit(int(BridgeExitCode::WaitForReadyTimeout));
        return false;
    }
    return false;
}

bool QtQuickTestSetup::handleFinalizeResult(FinalizeResult result)
{
    switch (result) {
    case FinalizeResult::Ok:
        return true;
    case FinalizeResult::NotExited:
        qApp->exit(int(BridgeExitCode::FinalizeNotExited));
        return false;
    case FinalizeResult::Failed:
        qApp->exit(int(BridgeExitCode::FinalizeFailed));
        return false;
    }
    return false;
}
