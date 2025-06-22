/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include "qmlapp.h"
#include <QAbstractListModel>
#include <QDotNetPropertyEvent>
#include <QDotNetFunction>
#include <QDotNetInterface>

struct UserListModel : public QDotNetObject
{
    Q_DOTNET_OBJECT_INLINE(UserListModel, "UserViewQml.UserListModel, UserViewQml");
    mutable QDotNetFunction<QDotNetRef> fnBase = nullptr;
    QAbstractListModel *base()
    {
        auto baseObj = method("get_Base", fnBase).invoke(*this);
        auto baseInterface = baseObj.cast<QDotNetInterface>();
        return baseInterface.dataAs<QAbstractListModel>();
    }
};

struct UserEventArgs : public QDotNetObject
{
    Q_DOTNET_OBJECT_INLINE(UserEventArgs, "UserViewQml.QmlApp+UserEventArgs, UserViewQml");
};

struct UserSignal : public QDotNetObject
{
    Q_DOTNET_OBJECT_INLINE(UserSignal, "UserViewQml.QmlApp+UserSignal, UserViewQml");
    UserSignal()
    {
        *this = constructor<UserSignal>().invoke(nullptr);
    }
    bool convert(QDotNetRef sender, UserEventArgs args)
    {
        return method<bool, QDotNetRef, UserEventArgs>("Convert")
            .invoke(*this, sender, args);
    }
    QString arg1() { return method<QString>("get_Arg1").invoke(*this); }
    QString arg2() { return method<QString>("get_Arg2").invoke(*this); }
    qint32 arg3() { return method<qint32>("get_Arg3").invoke(*this); }
};

struct QmlAppPrivate : public QDotNetEventHandler
{
    QmlApp *q = nullptr;

    QmlAppPrivate(QmlApp *q) : q(q)
    { }

    void handleEvent(const QString &ev, QDotNetObject &obj, QDotNetObject &args) override
    {
        if (ev == "PropertyChanged") {
            const auto propertyChangedEvent = args.cast<QDotNetPropertyEvent>();
            if (propertyChangedEvent.propertyName() == "AmountToAdd")
                emit q->amountToAddChanged();
        } else if (ev == "UserAdded") {
            UserSignal s;
            if (s.convert(obj, args.cast<UserEventArgs>()))
                emit q->userAdded(s.arg1(), s.arg2(), s.arg3());
        } else if (ev == "UserRemoved") {
            UserSignal s;
            if (s.convert(obj, args.cast<UserEventArgs>()))
                emit q->userRemoved(s.arg1(), s.arg2(), s.arg3());
        }
    }

    mutable QDotNetFunction<UserListModel> fnUsers = nullptr;
    mutable QDotNetFunction<qint32> fnGetAmountToAdd = nullptr;
    mutable QDotNetFunction<void, qint32> fnSetAmountToAdd = nullptr;
    mutable QDotNetFunction<void> fnAdd = nullptr;
    mutable QDotNetFunction<void> fnRemove = nullptr;
};

Q_DOTNET_OBJECT_IMPL(QmlApp);

QmlApp::QmlApp(QObject *parent) : d(new QmlAppPrivate(this))
{
    *this = constructor<QmlApp>().invoke(nullptr);
    subscribe("PropertyChanged", d);
    subscribe("UserAdded", d);
    subscribe("UserRemoved", d);
}

QmlApp::~QmlApp()
{
    delete d;
}

QAbstractListModel *QmlApp::users() const
{
    auto usersObj = method("get_Users", d->fnUsers).invoke(*this);
    return usersObj.base();
}

qint32 QmlApp::amountToAdd()
{
    return method("get_AmountToAdd", d->fnGetAmountToAdd).invoke(*this);
}

void QmlApp::setAmountToAdd(qint32 value)
{
    method("set_AmountToAdd", d->fnSetAmountToAdd).invoke(*this, value);
}

void QmlApp::add()
{
    method("Add", d->fnAdd).invoke(*this);
}

void QmlApp::remove()
{
    method("Remove", d->fnRemove).invoke(*this);
}

#include "moc_qmlapp.cpp"
