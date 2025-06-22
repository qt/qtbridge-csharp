/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#include "qdotnetinterface.h"
#include "iqmodelindex.h"
#include "iqvariant.h"
#include "qdotnetabstractlistmodel.h"

#include <functional>

class QDotNetStatic : public QDotNetInterface
{
public:
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.DotNet.Adapter+IStatic, Qt.DotNet.Adapter");

    QDotNetStatic(const void *objectRef) : QDotNetInterface(objectRef) {}

    QDotNetStatic() : QDotNetInterface(AssemblyQualifiedName, nullptr)
    {
        IQVariant::staticInit(this);
        IQModelIndex::staticInit(this);
        QDotNetAbstractListModel::staticInit(this);
    }
};

inline static bool ctor_static = std::invoke([]()
    {
        QDotNetAdapter::ctor_staticInterface = []()
            {
                auto *staticQVariant = new QDotNetStatic();
                auto setStatic = QDotNetType::staticMethod<void, QDotNetStatic>(
                    "Qt.DotNet.Adapter, Qt.DotNet.Adapter", "set_Static");
                setStatic(*staticQVariant);
                return staticQVariant;
            };
        return true;
    });

inline static bool dtor_static = std::invoke([]()
    {
        QDotNetAdapter::dtor_staticInterface = [](void *that)
            {
                QtDotNet::call<void, QDotNetStatic>(
                    "Qt.DotNet.Adapter, Qt.DotNet.Adapter", "set_Static", nullptr);
                delete reinterpret_cast<QDotNetStatic *>(that);
            };
        return true;
    });
