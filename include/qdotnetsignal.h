// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetobject.h"
#include "qdotnetarray.h"
#include "qdotnetevent.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QList>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

class QDotNetSignal : public QDotNetObject
{
public:
    Q_DOTNET_OBJECT_INLINE(QDotNetSignal, "Qt.MetaObject.Signal, Qt.Bridge.CSharp.Api");

    static QList<QDotNetSignal> fromEvent(const QString& name, const QDotNetRef &sender)
    {
        if (!typeSignal.isValid())
            typeSignal = QDotNetType::typeOf(AssemblyQualifiedName);
        if (typeSignal.isValid() && !fnFromEvent.isValid())
            typeSignal.staticMethod("FromEvent", fnFromEvent);
        if (!fnFromEvent.isValid())
            return {};
        auto signalRefs = fnFromEvent(name, sender);
        if (!signalRefs.isValid())
            return {};
        QList<QDotNetSignal> signalList;
        for (int i = 0; i < signalRefs.length(); ++i) {
            auto signalObj = signalRefs.get(i).cast<QDotNetSignal>();
            if (!signalObj.isValid())
                continue;
            signalList.append(signalObj);
        }
        return signalList;
    }

    static bool convert(const QDotNetSignal &signalObj, const QDotNetRef &sender,
                        const QDotNetEventArgs &args)
    {
        if (!typeSignal.isValid())
            typeSignal = QDotNetType::typeOf(AssemblyQualifiedName);
        if (typeSignal.isValid() && !fnConvert.isValid()) {
            fnConvert = adapter().resolveStaticMethod(AssemblyQualifiedName, "Convert",
                {
                    UnmanagedType::Bool,
                    AssemblyQualifiedName,
                    QDotNetOutbound<QDotNetRef>::Parameter,
                    QDotNetOutbound<QDotNetEventArgs>::Parameter
                });
        }
        if (!fnConvert.isValid())
            return false;
        return fnConvert(signalObj, sender, args);
    }

    QString name() const
    {
        return method("get_Name", fnName).invoke(*this);
    }

    int count() const
    {
        return method("get_Count", fnCount).invoke(*this);
    }

    QDotNetObject item(int index) const
    {
        return method("get_Item", fnItem).invoke(*this, index);
    }

    template<typename T>
    T arg(int argIdx)
    {
        const QString argName = QString("get_Arg%1").arg(argIdx + 1);
        QDotNetFunction<T> fnGetArg = method<T>(argName);
        return fnGetArg();
    }

    QDotNetType type(int argIdx)
    {
        if (argIdx < 0 || argIdx >= count())
            return { nullptr };
        auto fnGetType = method<QDotNetType>(QString("get_Type%1").arg(argIdx + 1));
        return fnGetType();
    }

    bool is(int argIdx, const QString &typeName)
    {
        if (argIdx < 0 || argIdx >= count())
            return false;
        auto argType = type(argIdx);
        return argType.isValid() && argType.isAssignableTo(QDotNetType::typeOf(typeName));
    }

private:
    static inline
        QDotNetType typeSignal = nullptr;
    static inline
        QDotNetFunction<QDotNetArray<QDotNetRef>, QString, QDotNetRef> fnFromEvent = nullptr;
    static inline
        QDotNetFunction<bool, QDotNetRef, QDotNetRef, QDotNetEventArgs> fnConvert = nullptr;
    mutable QDotNetFunction<QString> fnName = nullptr;
    mutable QDotNetFunction<int> fnCount = nullptr;
    mutable QDotNetFunction<QDotNetObject, int> fnItem = nullptr;
};
