// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

#pragma once

#include <qdotnetobject.h>

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QString>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

struct StringBuilderPrivate;

class StringBuilder final : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT(StringBuilder, "System.Text.StringBuilder");

    StringBuilder();
    StringBuilder(qint32 capacity, qint32 maxCapacity);
    ~StringBuilder() override;

    StringBuilder append(const QString &str);

private:
    StringBuilderPrivate *d;
};
