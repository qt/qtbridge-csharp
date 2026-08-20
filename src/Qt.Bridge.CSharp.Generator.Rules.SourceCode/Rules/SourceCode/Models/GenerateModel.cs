// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Models;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Models
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateModel : Class.GenerateClass
    {
        public override int Priority => base.Priority + 1;

        public override bool Matches(MemberInfo src)
            => src is Type type && type.IsAssignableTo(TypeOf<Model>())
            && type.ExportAsSourceCode();

        public override Result Execute(MemberInfo src)
        {
            if (!StatusFile.CheckIn(src, nameof(GenerateModel)))
                return Ok;

            if (src is not Type type)
                return Error();

            var baseTypes = new[] { TypeOf<ListModel>(), TypeOf<TableModel>() };
            var baseType = baseTypes
                .FirstOrDefault(bt => type.IsAssignableTo(bt), TypeOf<Model>());

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
            includes += "#include <QtQml/qqmlregistration.h>";

            // Models used only as QML property types must be available to tooling without being
            // creatable QML elements. Models that are QML elements must remain named and cannot
            // also be anonymous.
            if (!type.IsQmlElement()) {
                if (type.GetPlaceholder(TypeTraits) is not { } traits)
                    return Error();
                traits += "QML_ANONYMOUS";
            }

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
            privateMembers += "QVariant toVariant(QDotNetObject &&obj);";

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

QVariant {type.MFn(Ns | Name | Private)}::toVariant(QDotNetObject &&obj)
{{
    return QDotNetConvert::toVariant(obj, q);
}}
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            var c = new OverrideContext
            {
                Type = type,
                Methods = methods,
                PrivateMembers = privateMembers,
                Implementation = implementation
            };
            var overrides = type.GetMethods()
                .Where(func => func.IsOverrideOf(baseType) && !func.IsIgnored());
            foreach (var f in overrides) {
                var result = f switch
                {
                    // READ model meta-data
                    { Name: nameof(Model.RowCount) } => Gen_rowCount(c, f),
                    { Name: nameof(Model.ColumnCount) } => Gen_columnCount(c, f),
                    { Name: nameof(Model.RoleNames) } => Gen_roleNames(c, f),
                    { Name: nameof(Model.CanFetchMore) } => Gen_canFetchMore(c, f),

                    //// Currently unsupported overrides: remove comments when available
                    //
                    //{ Name: nameof(Model.Match) } => Gen_match(c, f),
                    //{ Name: nameof(Model.MimeTypes) } => Gen_mimeTypes(c, f),
                    //{ Name: nameof(Model.SupportedDragActions) } => Gen_supportedDragActions(c, f),
                    //{ Name: nameof(Model.SupportedDropActions) } => Gen_supportedDropActions(c, f),

                    // READ item meta-data
                    { Name: nameof(Model.Flags) } => Gen_flags(c, f),
                    { Name: nameof(Model.HasChildren) } => Gen_hasChildren(c, f),
                    { Name: nameof(Model.Index) } => Gen_index(c, f),
                    { Name: nameof(Model.Parent) } => Gen_parent(c, f),
                    { Name: nameof(Model.Sibling) } => Gen_sibling(c, f),
                    { Name: nameof(Model.Buddy) } => Gen_buddy(c, f),

                    //// Currently unsupported overrides: remove comments when available
                    //
                    //{ Name: nameof(Model.CanDropMimeData) } => Gen_canDropMimeData(c, f),
                    //{ Name: nameof(Model.MimeData) } => Gen_mimeData(c, f),
                    //{ Name: nameof(Model.Span) } => Gen_span(c, f),

                    // READ item data
                    { Name: nameof(Model.Data) } => Gen_data(c, f),
                    { Name: nameof(Model.HeaderData) } => Gen_headerData(c, f),

                    //// Currently unsupported overrides: remove comments when available
                    //
                    //{ Name: nameof(Model.ItemData) } => Gen_itemData(c, f),
                    //{ Name: nameof(Model.MultiData) } => Gen_multiData(c, f),

                    // WRITE model meta-data
                    { Name: nameof(Model.InsertRows) } => Gen_insertRows(c, f),
                    { Name: nameof(Model.InsertColumns) } => Gen_insertColumns(c, f),
                    { Name: nameof(Model.MoveRows) } => Gen_moveRows(c, f),
                    { Name: nameof(Model.MoveColumns) } => Gen_moveColumns(c, f),
                    { Name: nameof(Model.RemoveRows) } => Gen_removeRows(c, f),
                    { Name: nameof(Model.RemoveColumns) } => Gen_removeColumns(c, f),
                    { Name: nameof(Model.Sort) } => Gen_sort(c, f),

                    // WRITE item meta-data
                    { Name: nameof(Model.FetchMore) } => Gen_fetchMore(c, f),

                    //// Currently unsupported override: remove comment when available
                    //
                    //{ Name: nameof(Model.DropMimeData) } => Gen_dropMimeData(c, f),

                    // WRITE item data
                    { Name: nameof(Model.ClearItemData) } => Gen_clearItemData(c, f),
                    { Name: nameof(Model.SetData) } => Gen_setData(c, f),
                    { Name: nameof(Model.SetHeaderData) } => Gen_setHeaderData(c, f),

                    //// Currently unsupported override: remove comment when available
                    //
                    //{ Name: nameof(Model.SetItemData) } => Gen_setItemData(c, f),

                    _ => Error($"Unexpected override: {f.Name}")
                };
                if (!result.Succeeded)
                    return result;
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Initializer) is not { } init)
                return Error();
            init += $@"
QObject::connect(q, &{type.MFn(Ns | Name)}::modelChanged, [q](QObject *evObj)
{{
    Q_ASSERT_X(q->thread() == QThread::currentThread(),
        ""Model Changed"", ""Emit must run on the object's thread"");
    auto args = qobject_cast<{typeof(ModelChangeEventArgs).MFn(Ns | Name)} *>(evObj);
    if (args) {{
        auto idxParent = q->d->setOwnIndex(args->parent());
        auto idxDestinationParent = q->d->setOwnIndex(args->destinationParent());
        auto idxTopLeft = q->d->setOwnIndex(args->topLeft());
        auto idxBottomRight = q->d->setOwnIndex(args->bottomRight());
        switch (args->action()) {{
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginResetModel:
            q->beginResetModel();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndResetModel:
            q->endResetModel();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginInsertRows:
            q->beginInsertRows(idxParent, args->first(), args->last());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndInsertRows:
            q->endInsertRows();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginMoveRows:
            q->beginMoveRows(idxParent, args->first(), args->last(),
                idxDestinationParent, args->destinationChild());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndMoveRows:
            q->endMoveRows();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginRemoveRows:
            q->beginRemoveRows(idxParent, args->first(), args->last());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndRemoveRows:
            q->endRemoveRows();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginInsertColumns:
            q->beginInsertColumns(idxParent, args->first(), args->last());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndInsertColumns:
            q->endInsertColumns();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginMoveColumns:
            q->beginMoveColumns(idxParent, args->first(), args->last(),
                idxDestinationParent, args->destinationChild());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndMoveColumns:
            q->endMoveColumns();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::BeginRemoveColumns:
            q->beginRemoveColumns(idxParent, args->first(), args->last());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::EndRemoveColumns:
            q->endRemoveColumns();
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::DataChanged:
            {{
                QList<int> roles;
                if (auto *a = args->roles()) {{
                    int n = a->length();
                    for (int i = 0; i < n; i++)
                        roles << a->get(i);
                }}
                emit q->dataChanged(idxTopLeft, idxBottomRight, roles);
            }}
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::HeaderDataChanged:
            emit q->headerDataChanged(
                (Qt::Orientation)args->orientation(), args->first(), args->last());
            break;
        case {typeof(Model.EventAction).MFn(Ns | Name)}::NoAction:
            break;
        }}
        args->setSynchronized(true);
    }}
}});
";
            return Ok;
        }

        private class OverrideContext
        {
            public Type Type { get; set; }
            public Placeholder Methods { get; set; }
            public Placeholder PrivateMembers { get; set; }
            public Placeholder Implementation { get; set; }
        }

        private Result GenOverride(OverrideContext context, MethodInfo func,
            string retType, string[] formalArgs, string[] fnTypes, string[] actualArgs,
            string convert = "", bool isConst = false)
        {
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.Methods += $@"
{retType} {func.MFn(Name)}({string.Join(", ", formalArgs)}) {(isConst ? "const " : "")}override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.PrivateMembers += $@"
mutable QDotNetFunction<{string.Join(", ", fnTypes)}> {func.MFn(Func)} = nullptr;";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.Implementation += $@"
{retType} {context.Type.MFn(Ns | Name)}::{func.MFn(Name)}({string.Join(", ", formalArgs)}){Wrap}
{(isConst ? " const" : "")}
{{
    return {convert}(
        method(""{func.MFn(Src)}"", d->{func.MFn(Func)})
            .invoke(*this, {string.Join(", ", actualArgs)})
    );
}}
{Blank}";
            return Ok;
        }

        #region READ model meta-data ///////////////////////////////////////////////////////////////

        private Result Gen_rowCount(OverrideContext context, MethodInfo func)
        {
            // int rowCount(const QModelIndex &parent = QModelIndex()) const = 0
            return GenOverride(context, func,
                "int", ["const QModelIndex &parent"],
                ["int", "QModelIndex"], ["parent"],
                isConst: true);
        }

        private Result Gen_columnCount(OverrideContext context, MethodInfo func)
        {
            // int columnCount(const QModelIndex &parent = QModelIndex()) const = 0
            return GenOverride(context, func,
                "int", ["const QModelIndex &parent"],
                ["int", "QModelIndex"], ["parent"],
                isConst: true);
        }

        private Result Gen_roleNames(OverrideContext context, MethodInfo func)
        {
            // QHash<int, QByteArray> roleNames() const

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.Methods += $@"
QHash<int, QByteArray> roleNames() const override;
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.PrivateMembers += $@"
mutable QDotNetFunction<QDotNetObject> {func.MFn(Func)} = nullptr;";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            context.Implementation += $@"
QHash<int, QByteArray> {context.Type.MFn(Ns | Name)}::roleNames() const
{{
    static QHash<int, QByteArray> roles;
    if (!roles.empty())
        return roles;

    auto a = QDotNetConvert::toArray(method(""RoleNames"", d->{func.MFn(Func)}).invoke(*this));
    for (int i = 0; i + 1 < a.length(); i += 2) {{
        auto key = a[i];
        if (!QDotNetConvert::isInt32(key))
            continue;
        auto value = a[i + 1];
        roles.insert(QDotNetConvert::toInt32(key), QDotNetConvert::toString(value).toUtf8());
    }}

    return roles;
}}
{Blank}";
            return Ok;
        }

        private Result Gen_canFetchMore(OverrideContext context, MethodInfo func)
        {
            // bool canFetchMore(const QModelIndex &parent) const
            return GenOverride(context, func,
                "bool", ["const QModelIndex &parent"],
                ["bool", "QModelIndex"], ["parent"],
                isConst: true);
        }

        private Result Gen_match(OverrideContext context, MethodInfo func)
        {
            // QModelIndexList match(const QModelIndex &start, int role, const QVariant &value,
            //     int hits = 1,
            //     Qt::MatchFlags flags = Qt::MatchFlags(Qt::MatchStartsWith|Qt::MatchWrap)) const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_mimeTypes(OverrideContext context, MethodInfo func)
        {
            // QStringList mimeTypes() const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_supportedDragActions(OverrideContext context, MethodInfo func)
        {
            // Qt::DropActions supportedDragActions() const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_supportedDropActions(OverrideContext context, MethodInfo func)
        {
            // Qt::DropActions supportedDropActions() const
            return Error($"Unsupported override: {func.Name}");
        }

        #endregion READ model meta-data ////////////////////////////////////////////////////////////

        #region READ item meta-data ////////////////////////////////////////////////////////////////

        private Result Gen_flags(OverrideContext context, MethodInfo func)
        {
            // Qt::ItemFlags flags(const QModelIndex &index) const
            return GenOverride(context, func,
                "Qt::ItemFlags", ["const QModelIndex &index"],
                ["int", "QModelIndex"], ["index"],
                convert: "static_cast<Qt::ItemFlags>", isConst: true);
        }

        private Result Gen_hasChildren(OverrideContext context, MethodInfo func)
        {
            // bool hasChildren(const QModelIndex &parent = QModelIndex()) const
            return GenOverride(context, func,
                "bool", ["const QModelIndex &parent"],
                ["bool", "QModelIndex"], ["parent"],
                isConst: true);
        }

        private Result Gen_index(OverrideContext context, MethodInfo func)
        {
            // QModelIndex index(int row, int column,
            //     const QModelIndex &parent = QModelIndex()) const = 0
            return GenOverride(context, func,
                "QModelIndex", ["int row", "int column", "const QModelIndex &parent"],
                ["QModelIndex", "int", "int", "QModelIndex"], ["row", "column", "parent"],
                convert: "d->setOwnIndex", isConst: true);
        }

        private Result Gen_parent(OverrideContext context, MethodInfo func)
        {
            // QModelIndex parent(const QModelIndex &index) const = 0
            return GenOverride(context, func,
                "QModelIndex", ["const QModelIndex &index"],
                ["QModelIndex", "QModelIndex"], ["index"],
                convert: "d->setOwnIndex", isConst: true);
        }

        private Result Gen_sibling(OverrideContext context, MethodInfo func)
        {
            // QModelIndex sibling(int row, int column, const QModelIndex &index) const
            return GenOverride(context, func,
                "QModelIndex", ["int row", "int column", "const QModelIndex &index"],
                ["QModelIndex", "int", "int", "QModelIndex"], ["row", "column", "index"],
                convert: "d->setOwnIndex", isConst: true);
        }

        private Result Gen_buddy(OverrideContext context, MethodInfo func)
        {
            // QModelIndex buddy(const QModelIndex &index) const
            return GenOverride(context, func,
                "QModelIndex", ["const QModelIndex &index"],
                ["QModelIndex", "QModelIndex"], ["index"],
                convert: "d->setOwnIndex", isConst: true);
        }

        private Result Gen_span(OverrideContext context, MethodInfo func)
        {
            // QSize span(const QModelIndex &index) const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_canDropMimeData(OverrideContext context, MethodInfo func)
        {
            // bool canDropMimeData(const QMimeData *data, Qt::DropAction action,
            //     int row, int column, const QModelIndex &parent) const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_mimeData(OverrideContext context, MethodInfo func)
        {
            // QMimeData * mimeData(const QModelIndexList &indexes) const
            return Error($"Unsupported override: {func.Name}");
        }

        #endregion READ item meta-data /////////////////////////////////////////////////////////////

        #region READ item data /////////////////////////////////////////////////////////////////////

        private Result Gen_data(OverrideContext context, MethodInfo func)
        {
            // QVariant data(const QModelIndex &index, int role = Qt::DisplayRole) const = 0
            return GenOverride(context, func,
                "QVariant", ["const QModelIndex &index", "int role"],
                ["QDotNetObject", "QModelIndex", "int"], ["index", "role"],
                "d->toVariant", isConst: true);
        }

        private Result Gen_headerData(OverrideContext context, MethodInfo func)
        {
            // QVariant headerData(
            //  int section, Qt::Orientation orientation, int role = Qt::DisplayRole) const
            return GenOverride(context, func,
                "QVariant", ["int section", "Qt::Orientation orientation", "int role"],
                ["QDotNetObject", "int", "int", "int"], ["section", "orientation", "role"],
                convert: "d->toVariant", isConst: true);
        }

        private Result Gen_itemData(OverrideContext context, MethodInfo func)
        {
            // QMap<int, QVariant> itemData(const QModelIndex &index) const
            return Error($"Unsupported override: {func.Name}");
        }

        private Result Gen_multiData(OverrideContext context, MethodInfo func)
        {
            // void multiData(const QModelIndex &index, QModelRoleDataSpan roleDataSpan) const
            return Error($"Unsupported override: {func.Name}");
        }

        #endregion READ item data //////////////////////////////////////////////////////////////////

        #region WRITE model meta-data //////////////////////////////////////////////////////////////

        private Result Gen_insertRows(OverrideContext context, MethodInfo func)
        {
            // bool insertRows(int row, int count, const QModelIndex &parent = QModelIndex())
            return GenOverride(context, func,
                "bool", ["int row", "int count", "const QModelIndex &parent"],
                ["bool", "int", "int", "QModelIndex"], ["row", "count", "parent"],
                isConst: false);
        }

        private Result Gen_insertColumns(OverrideContext context, MethodInfo func)
        {
            // bool insertColumns(int column, int count, const QModelIndex &parent = QModelIndex())
            return GenOverride(context, func,
                "bool", ["int column", "int count", "const QModelIndex &parent"],
                ["bool", "int", "int", "QModelIndex"], ["column", "count", "parent"],
                isConst: false);
        }

        private Result Gen_moveRows(OverrideContext context, MethodInfo func)
        {
            // bool moveRows(const QModelIndex &sourceParent, int sourceRow, int count,
            //     const QModelIndex &destinationParent, int destinationChild)
            return GenOverride(context, func,
                "bool", ["const QModelIndex &sourceParent", "int sourceRow", "int count",
                    "const QModelIndex &destinationParent", "int destinationChild"],
                ["bool", "QModelIndex", "int", "int", "QModelIndex", "int"],
                ["sourceParent", "sourceRow", "count", "destinationParent", "destinationChild"],
                isConst: false);
        }

        private Result Gen_moveColumns(OverrideContext context, MethodInfo func)
        {
            // bool moveColumns(const QModelIndex &sourceParent, int sourceColumn, int count,
            //     const QModelIndex &destinationParent, int destinationChild)
            return GenOverride(context, func,
                "bool", ["const QModelIndex &sourceParent", "int sourceColumn", "int count",
                    "const QModelIndex &destinationParent", "int destinationChild"],
                ["bool", "QModelIndex", "int", "int", "QModelIndex", "int"],
                ["sourceParent", "sourceColumn", "count", "destinationParent", "destinationChild"],
                isConst: false);
        }

        private Result Gen_removeRows(OverrideContext context, MethodInfo func)
        {
            // bool removeRows(int row, int count, const QModelIndex &parent = QModelIndex())
            return GenOverride(context, func,
                "bool", ["int row", "int count", "const QModelIndex &parent"],
                ["bool", "int", "int", "QModelIndex"], ["row", "count", "parent"],
                isConst: false);
        }

        private Result Gen_removeColumns(OverrideContext context, MethodInfo func)
        {
            // bool removeColumns(int column, int count, const QModelIndex &parent = QModelIndex())
            return GenOverride(context, func,
                "bool", ["int column", "int count", "const QModelIndex &parent"],
                ["bool", "int", "int", "QModelIndex"], ["column", "count", "parent"],
                isConst: false);
        }

        private Result Gen_sort(OverrideContext context, MethodInfo func)
        {
            // void sort(int column, Qt::SortOrder order = Qt::AscendingOrder)
            return GenOverride(context, func,
                "void", ["int column", "Qt::SortOrder order"],
                ["void", "int", "int"], ["column", "order"],
                isConst: false);
        }

        #endregion WRITE model meta-data ///////////////////////////////////////////////////////////

        #region WRITE item meta-data ///////////////////////////////////////////////////////////////

        private Result Gen_fetchMore(OverrideContext context, MethodInfo func)
        {
            // void fetchMore(const QModelIndex &parent)
            return GenOverride(context, func,
                "void", ["const QModelIndex &parent"],
                ["void", "QModelIndex"], ["parent"],
                isConst: false);
        }

        private Result Gen_dropMimeData(OverrideContext context, MethodInfo func)
        {
            // bool dropMimeData(const QMimeData *data, Qt::DropAction action,
            //     int row, int column, const QModelIndex &parent)
            return Error($"Unsupported override: {func.Name}");
        }

        #endregion WRITE item meta-data ////////////////////////////////////////////////////////////

        #region WRITE item data ////////////////////////////////////////////////////////////////////

        private Result Gen_clearItemData(OverrideContext context, MethodInfo func)
        {
            // bool clearItemData(const QModelIndex &index)
            return GenOverride(context, func,
                "bool", ["const QModelIndex &index"],
                ["bool", "QModelIndex"], ["index"],
                isConst: false);
        }

        private Result Gen_setData(OverrideContext context, MethodInfo func)
        {
            // bool setData(const QModelIndex &index,
            //     const QVariant &value, int role = Qt::EditRole)
            return GenOverride(context, func,
                "bool", ["const QModelIndex &index", "const QVariant &value", "int role"],
                ["bool", "QModelIndex", "QDotNetObject", "int"],
                ["index", "QDotNetConvert::fromVariant(value)", "role"],
                isConst: false);
        }

        private Result Gen_setHeaderData(OverrideContext context, MethodInfo func)
        {
            // bool setHeaderData(int section, Qt::Orientation orientation,
            //     const QVariant &value, int role = Qt::EditRole)
            return GenOverride(context, func,
                "bool", ["int section", "Qt::Orientation orientation",
                    "const QVariant &value", "int role"],
                ["bool", "int", "int", "QDotNetObject", "int"],
                ["section", "orientation", "QDotNetConvert::fromVariant(value)", "role"],
                isConst: false);
        }

        private Result Gen_setItemData(OverrideContext context, MethodInfo func)
        {
            // bool setItemData(const QModelIndex &index, const QMap<int, QVariant> &roles)
            return Error($"Unsupported override: {func.Name}");
        }

        #endregion WRITE item data /////////////////////////////////////////////////////////////////
    }
}
