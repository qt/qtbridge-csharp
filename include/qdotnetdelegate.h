// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetobject.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QString>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

template<typename T, typename... TArg>
class QDotNetDelegate : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(QDotNetDelegate, "System.Delegate");

    QDotNetDelegate() : QDotNetObject(nullptr) { }

    T invoke(TArg... arg) const
    {
        return method("Invoke", fnInvoke).invoke(*this, arg...);
    }

    T operator()(TArg... arg) const
    {
        return invoke(arg...);
    }

private:
    mutable QDotNetFunction<T, TArg...> fnInvoke;
};
