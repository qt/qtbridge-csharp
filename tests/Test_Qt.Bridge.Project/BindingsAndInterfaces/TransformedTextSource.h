// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#pragma once

#include <qdotnetevent.h>
#include <qdotnetinterface.h>
#include <qdotnetobject.h>

#include <bindingsandinterfaces/itexttransformation.h>

#include <QObject>
#include <QString>
#include <QUrl>

struct TransformedTextSourcePrivate;

class NativeTextTransformation : public QDotNetInterface
{
public:
    static inline const QString &AssemblyQualifiedName =
        BindingsAndInterfaces::ITextTransformation::AssemblyQualifiedName;

    virtual QString transform(const QString &text) const = 0;
    virtual QUrl getUri(int n) const = 0;
    virtual void setUri(const QUrl &uri) const = 0;
    virtual int getNumber() const = 0;

protected:
    NativeTextTransformation();
    ~NativeTextTransformation() override = default;
};

class TransformedTextSource final : public QObject
{
    Q_OBJECT
    Q_PROPERTY(QString text READ text WRITE setText NOTIFY textChanged)

public:
    static void setAssemblyName(const QString &assemblyName);

    TransformedTextSource();
    explicit TransformedTextSource(const NativeTextTransformation &transformation);
    ~TransformedTextSource() override;

    [[nodiscard]] bool isValid() const;
    [[nodiscard]] QString text() const;
    void setText(const QString &value);

signals:
    void textChanged();

private:
    static inline QString TypeName;
    TransformedTextSourcePrivate *d = nullptr;
};
