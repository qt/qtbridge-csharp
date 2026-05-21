// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QCoreApplication>
#include <QDebug>
#include <QFileInfo>
#include <QQmlEngine>
#include <QStandardPaths>
#include <QtEnvironmentVariables>
#include <QtQuickTest>

#include <QDotNetDynamicObject>

#include "QtQuickTestSetup.h"

namespace {
// Enable Qt test-mode paths before Qt's
// generated main() creates the app object.
const bool testPathsEnabled = []() {
    QStandardPaths::setTestModeEnabled(true);
    return true;
}();
} // namespace

//QUICK_TEST_MAIN(Test_DynamicObject)
int main(int argc, char **argv)
{
    auto appDirPath = QFileInfo(argv[0]).absoluteDir().path();
    auto *typeDef = QDotNetDynamicObject::defineType("PrimesApp.PrimeFactory", "Primes",
                                                     "Primes.dll", appDirPath);

    QDotNetDynamicObject::addMethod(
            typeDef, "GetNthPrime",
            qEnvironmentVariable("PrimesApp_PrimeFactory_GetNthPrime").toInt(),
            typeDef->addMethod("getNthPrime(int)", "int"),
            { QDotNetInbound<int>::Parameter, QDotNetOutbound<int>::Parameter });

    QDotNetDynamicObject::buildType(typeDef, "PrimeFactory", "Application", 1, 0);

    QTEST_SET_MAIN_SOURCE_PATH
    return quick_test_main(argc, argv, "Test_DynamicObject", nullptr);
}

#include "main.moc"
