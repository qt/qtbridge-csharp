/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#include "qdotnetinterface.h"
#include "qdotnetobject.h"
#include "qdotnetarray.h"
#include "iqvariant.h"
#include "iqmodelindex.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QAbstractListModel>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

#include <functional>

template<typename Base>
struct IQAbstractListModel : public QDotNetNativeInterface<QAbstractListModel>
{
    static inline const QString &AssemblyQualifiedName =
        QStringLiteral("Qt.DotNet.IQAbstractListModel, Qt.DotNet.Adapter");

    IQAbstractListModel(Base *self)
        : QDotNetNativeInterface<QAbstractListModel>(AssemblyQualifiedName, self, false)
    {
        setCallback<int, IQModelIndex>(
            "Flags", [this](void *selfPtr, IQModelIndex index)
            {
                Base &self = *reinterpret_cast<Base *>(selfPtr);
                return (int)self.base_flags(index);
            });
        setCallback<bool, IQModelIndex, IQVariant, int>(
            "SetData", [this](void *selfPtr, IQModelIndex index, IQVariant value, int role)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                return self->base_setData(index, value, role);
            });
        setCallback<bool, int, int, IQModelIndex>(
            "InsertRows", [this](void *selfPtr, int row, int count, IQModelIndex parent)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                return self->base_insertRows(row, count, parent);
            });
        setCallback<bool, int, int, IQModelIndex>(
            "RemoveRows", [this](void *selfPtr, int row, int count, IQModelIndex parent)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                return self->base_removeRows(row, count, parent);
            });
        setCallback<void, IQModelIndex, int, int>(
            "BeginInsertRows", [this](void *selfPtr, IQModelIndex parent, int first, int last)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                self->base_beginInsertRows(parent, first, last);
            });
        setCallback<void>(
            "EndInsertRows", [this](void *selfPtr)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                self->base_endInsertRows();
            });
        setCallback<void, IQModelIndex, int, int>(
            "BeginRemoveRows", [this](void *selfPtr, IQModelIndex parent, int first, int last)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                self->base_beginRemoveRows(parent, first, last);
            });
        setCallback<void>(
            "EndRemoveRows", [this](void *selfPtr)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                self->base_endRemoveRows();
            });
        setCallback<IQModelIndex, int, int, void *>(
            "CreateIndex", [this](void *selfPtr, int row, int col, void *ptr)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                return IQModelIndex(self->base_createIndex(row, col, ptr));
            });
        setCallback<void, IQModelIndex, IQModelIndex, QDotNetArray<int>>(
            "EmitDataChanged", [this](void *selfPtr,
                IQModelIndex topLeft, IQModelIndex bottomRight, QDotNetArray<int> roles)
            {
                auto *self = reinterpret_cast<Base *>(selfPtr);
                if (roles.isValid()) {
                    QList<int> listRoles(roles.length());
                    for (int i = 0; i < roles.length(); ++i)
                        listRoles[i] = roles[i];
                    self->emit_base_dataChanged(topLeft, bottomRight, listRoles);
                }
                else {
                    self->emit_base_dataChanged(topLeft, bottomRight);
                }
            });
    }
};

class QDotNetAbstractListModel : public QAbstractListModel, public QDotNetObject
{

public:
    using IBase = IQAbstractListModel<QDotNetAbstractListModel>;
    Q_DOTNET_OBJECT_REF_INLINE(QDotNetAbstractListModel, Q_DOTNET_OBJECT_INIT(base(this)))
    Q_DOTNET_OBJECT_COPY_INLINE(QDotNetAbstractListModel, Q_DOTNET_OBJECT_INIT(base(this)))

    QDotNetAbstractListModel(QDotNetObject &&movSrc) noexcept
    : QDotNetObject(std::move(movSrc)), base(this)
    {}

    QDotNetAbstractListModel &operator=(QDotNetObject &&movSrc) noexcept
    {
        QDotNetObject::operator=(std::move(movSrc));
        return *this;
    }

    static void staticInit(QDotNetInterface *sta)
    {
        sta->setCallback<IBase, QDotNetObject>("QAbstractListModel_Create",
            [](void *, QDotNetObject self)
            {
                auto *obj = new QDotNetAbstractListModel(std::move(self));
                return obj->base;
            });
    }

    int rowCount(const QModelIndex &parent = QModelIndex()) const override
    {
        return method("RowCount", fnRowCount).invoke(*this, parent);
    }
    int base_rowCount(const QModelIndex &parent = QModelIndex()) const
    {
        return QAbstractListModel::rowCount(parent);
    }

    QVariant data(const QModelIndex &index, int role = Qt::DisplayRole) const override
    {
        return method("Data", fnData).invoke(*this, index, role);
    }
    QVariant base_data(const QModelIndex &index, int role = Qt::DisplayRole) const
    {
        return QAbstractListModel::data(index, role);
    }

    Qt::ItemFlags flags(const QModelIndex &index) const override
    {
        return Qt::ItemFlags::fromInt(method("Flags", fnFlags).invoke(*this, index));
    }
    Qt::ItemFlags base_flags(const QModelIndex &index) const
    {
        return QAbstractListModel::flags(index);
    }

    bool setData(const QModelIndex &index, const QVariant &value, int role = Qt::EditRole) override
    {
        return method("SetData", fnSetData)
            .invoke(*this, index, const_cast<std::remove_const_t<QVariant &>>(value), role);
    }
    bool base_setData(const QModelIndex &index, const QVariant &value, int role = Qt::EditRole)
    {
        return QAbstractListModel::setData(index, value, role);
    }

    bool insertRows(int row, int count, const QModelIndex &parent = QModelIndex()) override
    {
        return method("InsertRows", fnInsertRows).invoke(*this, row, count, parent);
    }
    bool base_insertRows(int row, int count, const QModelIndex &parent = QModelIndex())
    {
        return QAbstractListModel::insertRows(row, count, parent);
    }

    bool removeRows(int row, int count, const QModelIndex &parent = QModelIndex()) override
    {
        return method("RemoveRows", fnRemoveRows).invoke(*this, row, count, parent);
    }
    bool base_removeRows(int row, int count, const QModelIndex &parent = QModelIndex())
    {
        return QAbstractListModel::removeRows(row, count, parent);
    }

    QHash<int, QByteArray> roleNames() const override
    {
        auto names = method("RoleNames", fnRoleNames).invoke(*this);
        if (names.isEmpty())
            return QAbstractListModel::roleNames();
        auto nameList = names.split(u',', Qt::SkipEmptyParts);
        QHash<int, QByteArray> roles;
        for (int i = 0; i < nameList.size(); ++i)
            roles[Qt::UserRole + i + 1] = nameList[i].toUtf8();
        return roles;
    }

    void base_beginInsertRows(const QModelIndex &parent, int first, int last)
    {
        QAbstractListModel::beginInsertRows(parent, first, last);
    }

    void base_endInsertRows()
    {
        QAbstractListModel::endInsertRows();
    }

    void base_beginRemoveRows(const QModelIndex &parent, int first, int last)
    {
        QAbstractListModel::beginRemoveRows(parent, first, last);
    }

    void base_endRemoveRows()
    {
        QAbstractListModel::endRemoveRows();
    }

    QModelIndex base_createIndex(int arow, int acolumn, const void *adata) const
    {
        return QAbstractListModel::createIndex(arow, acolumn, adata);
    }

    void emit_base_dataChanged(const QModelIndex &topLeft, const QModelIndex &bottomRight,
        const QList<int> &roles = QList<int>())
    {
        emit QAbstractListModel::dataChanged(topLeft, bottomRight, roles);
    }

protected:
    IBase base;
    mutable QDotNetFunction<int, IQModelIndex> fnFlags = nullptr;
    mutable QDotNetFunction<int, IQModelIndex> fnRowCount = nullptr;
    mutable QDotNetFunction<IQVariant, IQModelIndex, int> fnData = nullptr;
    mutable QDotNetFunction<bool, IQModelIndex, IQVariant, int> fnSetData = nullptr;
    mutable QDotNetFunction<bool, int, int, IQModelIndex> fnInsertRows = nullptr;
    mutable QDotNetFunction<bool, int, int, IQModelIndex> fnRemoveRows = nullptr;
    mutable QDotNetFunction<QString> fnRoleNames = nullptr;
};
