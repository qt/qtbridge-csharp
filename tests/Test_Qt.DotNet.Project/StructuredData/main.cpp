/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include <QCoreApplication>
#include <QDebug>
#include <QQmlEngine>
#include <QtQuickTest>

#include "QtQuickTestSetup.h"

class Setup : public QtQuickTestSetup
{
    Q_OBJECT
};

QUICK_TEST_MAIN_WITH_SETUP(Test_StructuredData, Setup)
#include "main.moc"
