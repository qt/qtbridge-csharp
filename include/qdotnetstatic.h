// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetinterface.h"
#include "qdotnetobject.h"
#include "iqqmlengine.h"
#include "iqtresources.h"

#include <functional>

class QDotNetStatic : public QDotNetInterface
{
public:
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.DotNet.Adapter+IStatic, Qt.DotNet.Adapter");

    QDotNetStatic(const void *objectRef) : QDotNetInterface(objectRef) {}

    QDotNetStatic() : QDotNetInterface(AssemblyQualifiedName, nullptr)
    {
#ifdef QT_QUICK_LIB
        IQQmlEngine::staticInit(this);
#endif
        IQtResources::staticInit(this);
    }
};

inline static bool ctor_static = std::invoke([]()
    {
        QDotNetAdapter::ctor_staticInterface = []()
            {
                auto *staticObject = new QDotNetStatic();
                auto setStatic = QDotNetType::staticMethod<void, QDotNetStatic>(
                    "Qt.DotNet.Adapter, Qt.DotNet.Adapter", "set_Static");
                setStatic(*staticObject);
                return staticObject;
            };
        return true;
    });

inline static bool dtor_static = std::invoke([]()
    {
        QDotNetAdapter::dtor_staticInterface = [](void *that)
            {
                delete reinterpret_cast<QDotNetStatic *>(that);
            };
        return true;
    });
