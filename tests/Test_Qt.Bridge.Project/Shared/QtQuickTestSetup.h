// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#pragma once

#include <QObject>
#include <QQmlEngine>

#include "QtTestSetupBase.h"

class QtQuickTestSetup : public QObject, protected QtTestSetupBase
{
    Q_OBJECT

public slots:
    void applicationAvailable();
    void qmlEngineAvailable(QQmlEngine* qmlEngine);
    void cleanupTestCase();

protected:
    virtual bool beforeApplicationAvailable();
    virtual void afterApplicationAvailable();

    virtual bool beforeQmlEngineAvailable(QQmlEngine* engine);
    virtual void afterQmlEngineAvailable(QQmlEngine* engine);

    virtual bool beforeCleanupTestCase();
    virtual void afterCleanupTestCase();

    virtual bool handleReadyResult(ReadyResult result);
    virtual bool handleFinalizeResult(FinalizeResult result);
};
