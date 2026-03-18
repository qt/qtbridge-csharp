// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#include "TransformedTextSource.h"

struct TransformedTextSourcePrivate final : QDotNetEventHandler
{
    explicit TransformedTextSourcePrivate(TransformedTextSource *q)
        : q(q)
    {
    }

    void handleEvent(const QString &eventName, QDotNetObject &, QDotNetObject &args) override
    {
        if (eventName != "PropertyChanged")
            return;

        if (!propertyEventType.isValid())
            propertyEventType = QDotNetType::typeOf<QDotNetPropertyEvent>();
        if (!args.type().equals(propertyEventType))
            return;

        const auto propertyChangedEvent = args.cast<QDotNetPropertyEvent>();
        if (propertyChangedEvent.propertyName() == "Text")
            emit q->textChanged();
    }

    TransformedTextSource *q = nullptr;
    QDotNetObject object;
    QDotNetSafeMethod<QDotNetObject> ctorDefault;
    QDotNetSafeMethod<QDotNetObject, NativeTextTransformation> ctorWithTransformation;
    QDotNetFunction<QString> getText = nullptr;
    QDotNetFunction<void, QString> setText = nullptr;
    QDotNetType propertyEventType = nullptr;
};

NativeTextTransformation::NativeTextTransformation()
    : QDotNetInterface(AssemblyQualifiedName, nullptr)
{
    setCallback<QString, QString>("Transform",
        [this](void *, const QString &text)
        {
            return transform(text);
        });
    setCallback<QUrl, int>("GetUri",
        [this](void *, int n)
        {
            return getUri(n);
        });
    setCallback<void, QUrl>("SetUri",
        [this](void *, const QUrl &uri)
        {
            setUri(uri);
        });
    setCallback<int>("GetNumber",
        [this](void *)
        {
            return getNumber();
        });
}

void TransformedTextSource::setAssemblyName(const QString &assemblyName)
{
    TypeName = QString("%1, %2").arg("BindingsAndInterfaces.TransformedTextSource", assemblyName);
}

TransformedTextSource::TransformedTextSource()
    : d(new TransformedTextSourcePrivate(this))
{
    QDotNetObject::constructor(TypeName, d->ctorDefault);
    d->object = d->ctorDefault.invoke(nullptr);
    d->object.subscribe("PropertyChanged", d);
}

TransformedTextSource::TransformedTextSource(const NativeTextTransformation &transformation)
    : d(new TransformedTextSourcePrivate(this))
{
    QDotNetObject::constructor(TypeName, d->ctorWithTransformation);
    d->object = d->ctorWithTransformation.invoke(nullptr, transformation);
    d->object.subscribe("PropertyChanged", d);
}

TransformedTextSource::~TransformedTextSource()
{
    delete d;
}

bool TransformedTextSource::isValid() const
{
    return d->object.isValid();
}

QString TransformedTextSource::text() const
{
    return d->object.method("get_Text", d->getText).invoke(d->object);
}

void TransformedTextSource::setText(const QString &value)
{
    d->object.method("set_Text", d->setText).invoke(d->object, value);
}
