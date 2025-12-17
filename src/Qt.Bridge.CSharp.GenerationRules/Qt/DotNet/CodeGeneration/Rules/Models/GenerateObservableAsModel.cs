/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Models
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    /// <summary>
    /// Observable-Listen (ObservableCollection&lt;T&gt;, ReadOnlyObservableCollection&lt;T&gt;,
    /// u.a. INotifyCollectionChanged + List) als QAbstractListModel mit Delta-Updates (QAIM).
    /// </summary>
    public sealed class GenerateObservableAsModel : GenerateListAsModel
    {
        public override int Priority => base.Priority + 2;

        public override bool Matches(MemberInfo src)
            => src is Type t && t.IsObservableList(out _);

        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type || !type.IsObservableList(out _))
                return Error();

            // force includes, base class, rowCount/data/roleNames
            var baseResult = base.Execute(src);
            if (!baseResult.Succeeded)
                return baseResult;

            if (type.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += "#include <QMetaObject>";
            includes += "#include <QThread>";

            if (type.GetPlaceholder(Initializer) is not { } initializer)
                return Error();
            initializer += $@"
QObject::connect(q, &{type.MFn(Ns | Name)}::collectionChanged,
    q, &{type.MFn(Ns | Name)}::handleCollectionChanged);
";

            if (type.GetPlaceholder(MethodDeclarations) is not { } methodDeclarations)
                return Error();
            methodDeclarations += "void handleCollectionChanged(QObject* qEvArgs);";

            if (type.GetPlaceholder(MethodsImplementation) is not { } methodsImplementation)
                return Error();
            methodsImplementation += $@"
// Connect .NET collection deltas (emitted as collectionChanged) to a local
// slot that translates them into the corresponding QAbstractItemModel operations.
void {type.MFn(Ns | Name)}::handleCollectionChanged(QObject* qEvArgs)
{{
    // Translates NotifyCollectionChangedEventArgs to QAbstractItemModel delta calls.
    // IMPORTANT:
    //   For Remove we temporarily override rowCount() to the pre-removal value,
    //   because .NET has already mutated the collection when the event arrives.
    //   This keeps Qt invariants satisfied and avoids assertions in beginRemoveRows().

    using Args = System::Collections::Specialized::NotifyCollectionChangedEventArgs;
    auto *args = qobject_cast<Args*>(qEvArgs);
    if (!args)
        return;

    auto countItems = [](const System::Collections::IList* l) -> int {{
        if (!l || !l->isValid())
            return 0;
        auto &self = *const_cast<System::Collections::IList*>(l);
        return self.method<qint32>(QStringLiteral(""get_Count"")).invoke(self);
    }};

    switch (args->action()) {{
    case System::Collections::Specialized::NotifyCollectionChangedAction::Add: {{
        const int index = args->newStartingIndex();
        const auto *newItems = args->newItems();
        const int added = newItems ? countItems(newItems) : 0;
        if (added > 0) {{
            beginInsertRows(QModelIndex(), index, index + added - 1);
            endInsertRows();
            Q_EMIT countChanged();
        }}
        break;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Remove: {{
        const int index = args->oldStartingIndex();
        const auto *oldItems = args->oldItems();
        const int removed = oldItems ? countItems(oldItems) : 0;
        if (removed > 0) {{
            // .NET has already decremented Count when raising the event.
            // Qt expects the model to still report the PRE-REMOVAL row count
            // between beginRemoveRows() and endRemoveRows(). We therefore
            // (temporarily) override rowCount() to return the old count.
            // See GenerateListAsModel.cs for a longer rationale on rowCountOverride.
            d->rowCountOverride = count() + removed;
            beginRemoveRows(QModelIndex(), index, index + removed - 1);
            endRemoveRows();
            d->rowCountOverride = -1;
            Q_EMIT countChanged();
        }}
        break;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Replace: {{
        const int index = args->newStartingIndex();
        const auto *newItems = args->newItems();
        const int replaced = newItems ? countItems(newItems) : 0;
        if (replaced > 0) {{
            Q_EMIT dataChanged(createIndex(index, 0), createIndex(index + replaced - 1, 0), {{}} );
        }}
        break;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Move: {{
        const int oldIndex = args->oldStartingIndex();
        int newIndex = args->newStartingIndex();
        const auto *items = args->newItems();
        const int moved = items ? countItems(items) : 0;
        if (moved > 0) {{
            // Qt requires the destination index to refer to the position AFTER removing
            // the source block. If the destination lies behind the source block, the
            // insertion point shifts by the block size.
            // See QAbstractItemModel::beginMoveRows() docs for the exact contract.
            const int dest = (newIndex > oldIndex) ? (newIndex + moved) : newIndex;
            beginMoveRows(QModelIndex(), oldIndex, oldIndex + moved - 1, QModelIndex(), dest);
            endMoveRows();
        }}
        break;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Reset:
    default:
        // Full model reset. This invalidates selection and current index, which is expected
        // for Reset. Use sparingly; granular deltas above keep selection when possible.
        beginResetModel();
        endResetModel();
        Q_EMIT countChanged();
        break;
    }}
}}
{Blank}";
            return Ok;
        }
    }
}
