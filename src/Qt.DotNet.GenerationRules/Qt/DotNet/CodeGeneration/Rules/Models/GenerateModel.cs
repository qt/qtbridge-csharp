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
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var modelTypes = new[]
            {
                TypeOf<QAbstractListModel>(),
            };

            var baseModelType = modelTypes
                .Where(mt => type.IsAssignableTo(mt))
                .FirstOrDefault();
            if (baseModelType == null)
                return Ok;

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDecl3rdParty) is not { } fwdDecl)
                return Error();
            fwdDecl += "class QAbstractItemModel;";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            properties += $@"
Q_PROPERTY(QAbstractItemModel *model READ model CONSTANT)
QAbstractItemModel *model();
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } includes)
                return Error();
            includes += "#include <QAbstractItemModel>";
            includes += "#include <QDotNetInterface>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
mutable QAbstractItemModel *baseModel = nullptr;
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
QAbstractItemModel *{type.MFn(Ns | Name)}::model()
{{
    if (d->baseModel)
        return d->baseModel;
    auto baseObj = method<QDotNetRef>(""get_Base"").invoke(*this);
    if (!baseObj.isValid())
        return nullptr;
    auto baseInterface = baseObj.cast<QDotNetInterface>();
    if (!baseInterface.isValid())
        return nullptr;
    d->baseModel = baseInterface.dataAs<QAbstractItemModel>();
    if (!d->baseModel)
        return nullptr;
    d->baseModel->setParent(this);
    return d->baseModel;
}}
";
            return Ok;
        }
    }
}
