// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetobject.h"
#include "qdotnetarray.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QList>
#include <QString>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

class QDotNetEventArgs : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(QDotNetEventArgs, "System.EventArgs");
};

class QDotNetPropertyEvent : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(QDotNetPropertyEvent,
        "System.ComponentModel.PropertyChangedEventArgs, System.ObjectModel");

    QString propertyName() const
    {
        return method("get_PropertyName", fnPropertyName).invoke(*this);
    }

private:
    mutable QDotNetSafeMethod<QString> fnPropertyName;
};

Q_DOTNET_ENUM(QDotNetModelAction, "Qt.Bridge.Models.Model+EventAction, Qt.Bridge.CSharp.Api", int)
{
    NoAction = 0,
    BeginResetModel = 1,
    EndResetModel = 2,
    BeginInsertRows = 3,
    EndInsertRows = 4,
    BeginMoveRows = 5,
    EndMoveRows = 6,
    BeginRemoveRows = 7,
    EndRemoveRows = 8,
    BeginInsertColumns = 9,
    EndInsertColumns = 10,
    BeginMoveColumns = 11,
    EndMoveColumns = 12,
    BeginRemoveColumns = 13,
    EndRemoveColumns = 14,
    DataChanged = 15,
    HeaderDataChanged = 16
};

class QDotNetModelEvent : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(QDotNetModelEvent,
        "Qt.Bridge.Models.ModelChangeEventArgs, Qt.Bridge.CSharp.Api");

    Q_PROPERTY(QDotNetModelAction action READ action)
    QDotNetModelAction action() const {
        return method("get_Action", fnGetAction).invoke(*this);
    }

    Q_PROPERTY(QModelIndex parent READ parent)
    QModelIndex parent() const
    {
        return method("get_Parent", fnGetParent).invoke(*this);
    }

    Q_PROPERTY(qint32 first READ first)
    qint32 first() const {
        return method("get_First", fnGetFirst).invoke(*this);
    }

    Q_PROPERTY(qint32 last READ last)
    qint32 last() const {
        return method("get_Last", fnGetLast).invoke(*this);
    }

    Q_PROPERTY(QModelIndex destinationParent READ destinationParent)
    QModelIndex destinationParent() const
    {
        return method("get_DestinationParent", fnGetDestinationParent).invoke(*this);
    }

    Q_PROPERTY(qint32 destinationChild READ destinationChild)
    qint32 destinationChild() const
    {
        return method("get_DestinationChild", fnGetDestinationChild).invoke(*this);
    }

    Q_PROPERTY(QModelIndex topLeft READ topLeft)
    QModelIndex topLeft() const {
        return method("get_TopLeft", fnGetTopLeft).invoke(*this);
    }

    Q_PROPERTY(QModelIndex bottomRight READ bottomRight)
    QModelIndex bottomRight() const
    {
        return method("get_BottomRight", fnGetBottomRight).invoke(*this);
    }

    Q_PROPERTY(QList<int> roles READ roles)
    QList<int> roles() const
    {
        auto resultArray = method("get_Roles", fnGetRoles).invoke(*this);
        QList<int> resultList;
        for (int i = 0; i < resultArray.length(); ++i)
            resultList << resultArray[i];
        return resultList;
    }

    Q_PROPERTY(Qt::Orientation orientation READ orientation)
    Qt::Orientation orientation() const
    {
        return static_cast<Qt::Orientation>(
                method("get_Orientation", fnGetOrientation).invoke(*this));
    }

    Q_PROPERTY(bool synchronized READ synchronized WRITE setSynchronized)
    bool synchronized() const
    {
        return method("get_Synchronized", fnGetSynchronized).invoke(*this);
    }
    void setSynchronized(bool value)
    {
        method("set_Synchronized", fnSetSynchronized).invoke(*this, value);
    }

private:
    mutable QDotNetFunction<QDotNetModelAction> fnGetAction = nullptr;
    mutable QDotNetFunction<QModelIndex> fnGetParent = nullptr;
    mutable QDotNetFunction<qint32> fnGetFirst = nullptr;
    mutable QDotNetFunction<qint32> fnGetLast = nullptr;
    mutable QDotNetFunction<QModelIndex> fnGetDestinationParent = nullptr;
    mutable QDotNetFunction<qint32> fnGetDestinationChild = nullptr;
    mutable QDotNetFunction<QModelIndex> fnGetTopLeft = nullptr;
    mutable QDotNetFunction<QModelIndex> fnGetBottomRight = nullptr;
    mutable QDotNetFunction<QDotNetArray<int>> fnGetRoles = nullptr;
    mutable QDotNetFunction<qint32> fnGetOrientation = nullptr;
    mutable QDotNetFunction<bool> fnGetSynchronized = nullptr;
    mutable QDotNetFunction<void, bool> fnSetSynchronized = nullptr;
};
