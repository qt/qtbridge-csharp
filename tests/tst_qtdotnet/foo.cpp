/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#include "foo.h"

#include <qdotnetevent.h>

struct FooPrivate final : QDotNetEventHandler
{
    Foo *q;
    FooPrivate(Foo *q) : q(q) {}

    QDotNetFunction<Foo, IBarTransformation> ctor = nullptr;

    QDotNetFunction<QString> bar;
    QDotNetFunction<void, QString> setBar;

    QDotNetType typePropertyEvent = nullptr;

    void handleEvent(const QString &eventName, QDotNetObject &sender, QDotNetObject &args) override
    {
        if (eventName != "PropertyChanged")
            return;

        if (!typePropertyEvent.isValid())
            typePropertyEvent = QDotNetType::typeOf<QDotNetPropertyEvent>();
        if (!args.type().equals(typePropertyEvent))
            return;

        const auto propertyChangedEvent = args.cast<QDotNetPropertyEvent>();
        if (propertyChangedEvent.propertyName() == "Bar")
            emit q->barChanged();
    }
};

Q_DOTNET_OBJECT_IMPL(Foo, Q_DOTNET_OBJECT_INIT(d(new FooPrivate(this))));

Foo::Foo() : d(new FooPrivate(this))
{
    const auto ctor = constructor<Foo, Null<IBarTransformation>>();
    *this = ctor(nullptr);
    subscribe("PropertyChanged", d);
}

Foo::Foo(const IBarTransformation &transformation) : d(new FooPrivate(this))
{
    *this = constructor(d->ctor).invoke(*this, transformation);
    subscribe("PropertyChanged", d);
}

Foo::~Foo()
{
    delete d;
}

QString Foo::bar() const
{
    return method("get_Bar", d->bar).invoke(*this);
}

void Foo::setBar(const QString &value)
{
    method("set_Bar", d->setBar).invoke(*this, value);
}

IBarTransformation::IBarTransformation() : QDotNetInterface(AssemblyQualifiedName, nullptr)
{
    setCallback<QString, QString>("Transform",
        [this](void *, const QString &bar) {
            return transform(bar);
        });

    setCallback<Uri, int>("GetUri",
        [this](void *, int n)
        {
            return getUri(n);
        });

    setCallback<void, Uri>("SetUri",
        [this](void *, Uri uri)
        {
            setUri(uri);
        });

    setCallback<int>("GetNumber",
        [this](void *)
        {
            return getNumber();
        });
}
