// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Models
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
            => src is Type t && t.IsObservableList(out _)
            && t.ExportAsSourceCode();

        public override Result Execute(MemberInfo src)
        {
            if (!StatusFile.CheckIn(src, nameof(GenerateObservableAsModel)))
                return Ok;

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
            methodDeclarations += "bool applyCollectionChange(QObject* qEvArgs);";

            if (type.GetPlaceholder(MethodsImplementation) is not { } methodsImplementation)
                return Error();
            methodsImplementation += $@"
// Translates a collection change into the corresponding model delta, as generated source
// models do. The collection has already changed when the event arrives, so a change that
// cannot be mapped, for example because its position is unknown, resets the model.
void {type.MFn(Ns | Name)}::handleCollectionChanged(QObject* qEvArgs)
{{
    using Args = System::Collections::Specialized::NotifyCollectionChangedEventArgs;
    if (auto *args = qobject_cast<Args*>(qEvArgs); args && applyCollectionChange(args))
        return;
    beginResetModel();
    endResetModel();
    Q_EMIT countChanged();
}}

// Applies a collection change as a model delta. Returns false if the change does not map to
// one, positions are checked against the row count from before the change.
bool {type.MFn(Ns | Name)}::applyCollectionChange(QObject* qEvArgs)
{{
    using Args = System::Collections::Specialized::NotifyCollectionChangedEventArgs;
    auto *args = static_cast<Args*>(qEvArgs);

    // Counts the items in the NewItems or OldItems list of the event. The list is only read
    // here, so a reference on the stack suffices; a wrapper object would outlive the event.
    auto countItems = [args](const QString &listGetter) -> int {{
        const auto list = args->method<QDotNetObject>(listGetter).invoke(*args);
        if (!list.isValid())
            return 0;
        return list.method<qint32>(QStringLiteral(""get_Count"")).invoke(list);
    }};

    const int count = this->count();
    switch (args->action()) {{
    case System::Collections::Specialized::NotifyCollectionChangedAction::Add: {{
        const int first = args->newStartingIndex();
        const int added = countItems(QStringLiteral(""get_NewItems""));
        if (added == 0)
            return true;
        if (first < 0 || first > count - added)
            return false;
        beginInsertRows(QModelIndex(), first, first + added - 1);
        endInsertRows();
        Q_EMIT countChanged();
        return true;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Remove: {{
        const int first = args->oldStartingIndex();
        const int removed = countItems(QStringLiteral(""get_OldItems""));
        if (removed == 0)
            return true;
        const int before = count + removed;
        if (first < 0 || first + removed > before)
            return false;
        // Qt expects the model to report the row count from before the removal until
        // endRemoveRows(). See GenerateListAsModel.cs for a longer rationale.
        d->rowCountOverride = before;
        beginRemoveRows(QModelIndex(), first, first + removed - 1);
        endRemoveRows();
        d->rowCountOverride = -1;
        Q_EMIT countChanged();
        return true;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Replace: {{
        const int first = args->newStartingIndex();
        const int replaced = countItems(QStringLiteral(""get_NewItems""));
        if (replaced != countItems(QStringLiteral(""get_OldItems"")))
            return false;
        if (replaced == 0)
            return true;
        if (first < 0 || first + replaced > count)
            return false;
        Q_EMIT dataChanged(createIndex(first, 0), createIndex(first + replaced - 1, 0), {{}});
        return true;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Move: {{
        const int first = args->oldStartingIndex();
        const int target = args->newStartingIndex();
        const int moved = countItems(QStringLiteral(""get_NewItems""));
        if (moved == 0 || (first == target && first >= 0))
            return true;
        if (first < 0 || target < 0 || first + moved > count || target + moved > count)
            return false;
        // Qt expects the destination as it was before the move, so a block that moves down
        // lands behind the items it passes.
        const int destination = target > first ? target + moved : target;
        if (!beginMoveRows(QModelIndex(), first, first + moved - 1, QModelIndex(), destination))
            return false;
        endMoveRows();
        return true;
    }}
    case System::Collections::Specialized::NotifyCollectionChangedAction::Reset:
    default:
        return false;
    }}
}}
{Blank}";
            return Ok;
        }
    }
}
