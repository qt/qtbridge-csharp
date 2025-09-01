/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Qt.DotNet.CodeGeneration.Rules.Models
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateListAsModel : Class.GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is Type type && type.IsList(out _);
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type || !type.IsList(out Type itemType))
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += "#include <QAbstractListModel>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(QObjectBaseClass) is not { } qObjectBase)
                return Error();
            qObjectBase.Reset();
            qObjectBase += "public QAbstractListModel";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
int rowCount(const QModelIndex &parent = QModelIndex()) const override;
{Blank}";
            methods += $@"
QVariant data(const QModelIndex &index, int role) const override;
{Blank}";
            methods += $@"
QHash<int, QByteArray> roleNames() const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
QHash<int, QByteArray> {type.MFn(Ns | Name)}::roleNames() const
{{
    static QHash<int, QByteArray> roles {{ {{ Qt::UserRole, ""item"" }} }};
    return roles;
}}

int {type.MFn(Ns | Name)}::rowCount(const QModelIndex &parent) const
{{
    return {(type.IsArray ? "length" : "count")}();
}}

QVariant {type.MFn(Ns | Name)}::data(const QModelIndex &index, int role) const
{{
    if (index.row() < 0 || index.row() >= {(type.IsArray ? "length" : "count")}())
        return QVariant();
    if (role == Qt::UserRole)
        {(itemType.IsValue() ? $@"{Wrap}
        return QVariant({(type.IsArray ? "get" : "item")}(index.row()));" : $@"{Wrap}
        return QVariant::fromValue<QObject *>({(type.IsArray ? "get" : "item")}(index.row()));")}
    return QVariant();
}}
{Blank}";
            return Ok;
        }
    }
}
