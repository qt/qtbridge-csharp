// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include <QFileInfo>
#include <QtCore/private/qmetaobjectbuilder_p.h>
#include <QtQuickTest>

#include "QtQuickTestSetup.h"
#include "metadata_loader.h"

namespace
{
    void checkMethodIndexDelta()
    {
#if QT_VERSION_CHECK(6, 12, 0) <= QT_VERSION
        QMetaObjectBuilder typeDef;
        typeDef.addMethod("firstMethod()");
        typeDef.addMethod("secondMethod()");
        typeDef.addSignal("firstSignal()");
        typeDef.addSignal("secondSignal()");

        int methodIndexDelta = typeDef.methodCount();
        for (int i = 0; i < typeDef.methodCount() && !typeDef.method(i).signature().isNull(); ++i)
            --methodIndexDelta;

        if (typeDef.methodCount() != 4
            || typeDef.method(0).signature() != "firstMethod()"
            || typeDef.method(1).signature() != "secondMethod()"
            || !typeDef.method(2).signature().isNull()
            || !typeDef.method(3).signature().isNull()
            || typeDef.signal(0).signature() != "firstSignal()"
            || typeDef.signal(1).signature() != "secondSignal()"
            || methodIndexDelta != 2) {
            qFatal("QMetaObjectBuilder method-index delta behavior changed (expected 2, got %d)",
                   methodIndexDelta);
        }
#endif
    }
}

class Setup : public QtQuickTestSetup
{
    Q_OBJECT
};

int main(int argc, char **argv)
{
    checkMethodIndexDelta();
    QDotNetConvert::setDispatch(QtDotNet::objectDispatch);
    QtDotNet::loadTypeMetadata(QFileInfo(argv[0]).absoluteDir().path(), {});
    QTEST_SET_MAIN_SOURCE_PATH
    Setup setup;
    return quick_test_main_with_setup(argc, argv, "Test_QMetaObjectBuilder",
                                      QUICK_TEST_SOURCE_DIR_DOTNET, &setup);
}

#include "main.moc"
