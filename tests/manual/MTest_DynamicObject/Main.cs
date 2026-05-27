// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Qt.Quick;

[assembly: Qt.Generate(
    Packages = "CorePrivate", Libraries = "PRIVATE Qt::CorePrivate",
    MainIncludes = """
    #include <QDotNetDynamicObject>
    """,
    MainStartingUp = """
        auto *typeFoo = QDotNetDynamicObject::defineType("MTest_DynamicObject.Foo",
            "MTest_DynamicObject", "MTest_DynamicObject.dll", appDirPath);

        auto intProperty = typeFoo->addProperty("intProperty", "qint32");
        intProperty.setReadable(true);
        intProperty.setWritable(true);
        QDotNetDynamicObject::addProperty(typeFoo, "IntProperty", intProperty,
            QDotNetInbound<qint32>::Parameter, QDotNetOutbound<qint32>::Parameter);

        auto intReadOnlyProperty = typeFoo->addProperty("intReadOnlyProperty", "qint32");
        intReadOnlyProperty.setReadable(true);
        intReadOnlyProperty.setWritable(false);
        QDotNetDynamicObject::addProperty(typeFoo, "IntReadOnlyProperty", intReadOnlyProperty,
            QDotNetInbound<qint32>::Parameter, QDotNetOutbound<void>::Parameter);

        auto intWriteOnlyProperty = typeFoo->addProperty("intWriteOnlyProperty", "qint32");
        intWriteOnlyProperty.setReadable(false);
        intWriteOnlyProperty.setWritable(true);
        QDotNetDynamicObject::addProperty(typeFoo, "IntWriteOnlyProperty", intWriteOnlyProperty,
            QDotNetInbound<void>::Parameter, QDotNetOutbound<qint32>::Parameter);

        auto stringProperty = typeFoo->addProperty("stringProperty", "QString");
        stringProperty.setReadable(true);
        stringProperty.setWritable(true);
        QDotNetDynamicObject::addProperty(typeFoo, "StringProperty", stringProperty,
            QDotNetInbound<QString>::Parameter, QDotNetOutbound<QString>::Parameter);

        auto dateTimeProperty = typeFoo->addProperty("dateTimeProperty", "QDateTime");
        dateTimeProperty.setReadable(true);
        dateTimeProperty.setWritable(true);
        QDotNetDynamicObject::addProperty(typeFoo, "DateTimeProperty", dateTimeProperty,
            QDotNetInbound<QDateTime>::Parameter, QDotNetOutbound<QDateTime>::Parameter);

        auto uriProperty = typeFoo->addProperty("uriProperty", "QUrl");
        uriProperty.setReadable(true);
        uriProperty.setWritable(true);
        QDotNetDynamicObject::addProperty(typeFoo, "UriProperty", uriProperty,
            QDotNetInbound<QUrl>::Parameter, QDotNetOutbound<QUrl>::Parameter);

        QDotNetDynamicObject::addMethod(
            typeFoo, "UInt64FuncInt", 0, typeFoo->addMethod("uInt64FuncInt(qint32)", "quint64"),
            { QDotNetInbound<quint64>::Parameter, QDotNetOutbound<qint32>::Parameter });

        QDotNetDynamicObject::buildType(typeFoo, "Foo", "Application", 1, 0);
    """)]

namespace MTest_DynamicObject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Qml.LoadFromRootModule("Main");
            Qml.WaitForExit();
        }
    }
}
