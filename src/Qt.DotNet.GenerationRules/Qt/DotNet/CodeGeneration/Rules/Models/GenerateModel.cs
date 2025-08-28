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

    public class GenerateModel : Class.GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is Type type && type.IsAssignableTo(TypeOf<Model>());
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var baseTypes = new[] { TypeOf<ListModel>(), TypeOf<TableModel>() };
            var baseType = baseTypes
                .FirstOrDefault(bt => type.IsAssignableTo(bt), TypeOf<Model>());

            var funcs = type.GetMethods().Where(func => func.IsOverrideOf(baseType));

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } includes)
                return Error();
            if (baseType == TypeOf<ListModel>())
                includes += "#include <QAbstractListModel>";
            else if (baseType == TypeOf<TableModel>())
                includes += "#include <QAbstractTableModel>";
            else
                includes += "#include <QAbstractItemModel>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(QObjectBaseClass) is not { } qObjectBase)
                return Error();
            qObjectBase.Reset();
            if (baseType == TypeOf<ListModel>())
                qObjectBase += "public QAbstractListModel";
            else if (baseType == TypeOf<TableModel>())
                qObjectBase += "public QAbstractTableModel";
            else
                qObjectBase += "public QAbstractItemModel";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += "QModelIndex setOwnIndex(const QModelIndex &idx);";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
QModelIndex {type.MFn(Ns | Name | Private)}::setOwnIndex(const QModelIndex &idx)
{{
    if (idx.model() != nullptr && idx.model() != reinterpret_cast<void *>(-1))
        return QModelIndex(idx);
    return q->createIndex(idx.row(), idx.column(), idx.internalId());
}}
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            foreach (var func in type.GetMethods().Where(func => func.IsOverrideOf(baseType))) {
                var result = func switch
                {
                    { Name: nameof(Model.RoleNames) }
                        => GenerateRoleNames(type, func, methods, privateMembers, implementation),
                    { Name: nameof(Model.RowCount) }
                        => GenerateRowCount(type, func, methods, privateMembers, implementation),
                    { Name: nameof(Model.Data) }
                        => GenerateData(type, func, methods, privateMembers, implementation),
                    _ => Error()
                };
                if (!result.Succeeded)
                    return result;
            }

            return Ok;
        }

        private Result GenerateRoleNames(Type type, MethodInfo func,
            Placeholder methods, Placeholder privateMembers, Placeholder implementation)
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            methods += $@"
QHash<int, QByteArray> roleNames() const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            privateMembers += $@"
mutable QDotNetFunction<QDotNetObject> {func.MFn(Func)} = nullptr;";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            implementation += $@"
QHash<int, QByteArray> {type.MFn(Ns | Name)}::roleNames() const
{{
    static QHash<int, QByteArray> roles;
    if (!roles.empty())
        return roles;

    auto a = Convert::toArray(method(""RoleNames"", d->{func.MFn(Func)}).invoke(*this));
    for (int i = 0; i + 1 < a.length(); i += 2) {{
        auto key = a[i];
        if (!Convert::isInt32(key))
            continue;
        auto value = a[i + 1];
        roles.insert(Convert::toInt32(key), Convert::toString(value).toUtf8());
    }}

    return roles;
}}
{Blank}";
            return Ok;
        }

        private Result GenerateRowCount(Type type, MethodInfo func,
            Placeholder methods, Placeholder privateMembers, Placeholder implementation)
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            methods += $@"
int rowCount(const QModelIndex &parent = QModelIndex()) const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            privateMembers += $@"
mutable QDotNetFunction<int, QModelIndex> {func.MFn(Func)} = nullptr;";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            implementation += $@"
int {type.MFn(Ns | Name)}::rowCount(const QModelIndex &parent) const
{{
    return method(""RowCount"", d->{func.MFn(Func)}).invoke(*this, parent);
}}
{Blank}";
            return Ok;
        }

        private Result GenerateData(Type type, MethodInfo func,
            Placeholder methods, Placeholder privateMembers, Placeholder implementation)
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            methods += $@"
QVariant data(const QModelIndex &index, int role) const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            privateMembers += $@"
mutable QDotNetFunction<QDotNetObject, QModelIndex, int> {func.MFn(Func)} = nullptr;";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            implementation += $@"
QVariant {type.MFn(Ns | Name)}::data(const QModelIndex &index, int role) const
{{
    return Convert::toVariant(method(""Data"", d->{func.MFn(Func)}).invoke(*this, index, role));
}}
{Blank}";
            return Ok;
        }
    }
}
