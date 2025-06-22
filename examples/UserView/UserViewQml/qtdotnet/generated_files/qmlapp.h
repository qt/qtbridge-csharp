/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#ifndef QMLAPP_H
#define QMLAPP_H

#include <QtQmlIntegration>
#include <QDotNetObject>

class QAbstractListModel;
struct QmlAppPrivate;

class QmlApp : public QObject, public QDotNetObject
{
    Q_OBJECT
    QML_ELEMENT
    QML_SINGLETON
public:
    Q_DOTNET_OBJECT(QmlApp, "UserViewQml.QmlApp, UserViewQml");
    QmlApp(QObject *parent = nullptr);
    ~QmlApp();

    // [QProperty(Constant = true)]
    // public UserListModel Users { get; }
    Q_PROPERTY(QAbstractItemModel *users READ users CONSTANT)
    QAbstractListModel *users() const;

    // public int AmountToAdd { get; set; }
    Q_PROPERTY(qint32 amountToAdd READ amountToAdd WRITE setAmountToAdd NOTIFY amountToAddChanged)
    qint32 amountToAdd();
    void setAmountToAdd(qint32 value);
    Q_SIGNAL void amountToAddChanged();

    // [QSlot]
    // public void Add()
    Q_SLOT void add();

    // [QSlot]
    // public void Remove()
    Q_SLOT void remove();

    // [QSignal<UserSignal>]
    // public event EventHandler<UserEventArgs> UserAdded
    Q_SIGNAL void userAdded(QString, QString, qint32);

    // [QSignal<UserSignal>]
    // public event EventHandler<UserEventArgs> UserRemoved
    Q_SIGNAL void userRemoved(QString, QString, qint32);

private:
    QmlAppPrivate *d = nullptr;
};

#endif //QMLAPP_H
