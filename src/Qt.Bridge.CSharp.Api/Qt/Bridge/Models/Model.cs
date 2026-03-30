// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.DotNet;
using Qt.Quick;
using Qt.Bridge.Mime;

namespace Qt.Bridge.Models
{
    /// <summary>
    /// Describes a change made by a <see cref="Model"/> implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of <see cref="ModelChangeEventArgs"/> are created by <see cref="Model"/> and sent
    /// through <see cref="Model.ModelChanged"/> whenever a subclass calls one of the protected
    /// notification helpers such as <see cref="Model.BeginInsertRows(ModelIndex, int, int)"/> or
    /// <see cref="Model.DataChanged(ModelIndex, ModelIndex, int[])"/>.
    /// </para>
    /// <para>
    /// This type is part of the internal notification flow. Typical model implementations do not
    /// create or modify these objects directly.
    /// </para>
    /// </remarks>
    [Include]
    public class ModelChangeEventArgs : EventArgs
    {
        [Enable]
        public Model.EventAction Action { get; init; }
        [Enable]
        public ModelIndex Parent { get; init; }
        [Enable]
        public int First { get; init; }
        [Enable]
        public int Last { get; init; }
        [Enable]
        public ModelIndex DestinationParent { get; init; }
        [Enable]
        public int DestinationChild { get; init; }
        [Enable]
        public ModelIndex TopLeft { get; init; }
        [Enable]
        public ModelIndex BottomRight { get; init; }
        [Enable]
        public List<int> Roles { get; init; }
        [Enable]
        public int Orientation { get; init; }
        [Enable]
        public bool Synchronized { get; set; } = false;
    }

    /// <summary>
    /// Represents a C# data model that can be consumed by views and delegates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="Model"/> when you need full control over how items are arranged, including
    /// hierarchical data or custom parent-child relationships. For common flat shapes, prefer
    /// <see cref="ListModel"/> or <see cref="TableModel"/>, which provide specialized base
    /// behavior for one-dimensional and row/column data.
    /// </para>
    /// <para>
    /// Subclasses describe the shape of the data and expose values by overriding methods such as
    /// <see cref="RowCount(ModelIndex)"/>, <see cref="ColumnCount(ModelIndex)"/>,
    /// <see cref="Index(int, int, ModelIndex)"/>, <see cref="Parent(ModelIndex)"/>, and
    /// <see cref="Data(ModelIndex, int)"/>. Optional capabilities such as editing, sorting, or
    /// loading data on demand are enabled by overriding the corresponding virtual methods.
    /// </para>
    /// <para>
    /// Structural changes must be announced with matching protected begin/end calls. Call the
    /// relevant <c>Begin*</c> helper before mutating the backing storage, then call the matching
    /// <c>End*</c> helper immediately after the model modification completes. For in-place value
    /// updates, call <see cref="DataChanged(ModelIndex, ModelIndex, int[])"/> or
    /// <see cref="HeaderDataChanged(int, int, int)"/> instead of a structural notification.
    /// </para>
    /// <para>
    /// Once a <c>Begin*</c> helper has been called successfully, the matching <c>End*</c> helper
    /// must also be called. This is part of the base class contract and should usually be enforced
    /// with a <c>try</c>/<c>finally</c> block around the model modification.
    /// </para>
    /// <code language="csharp"><![CDATA[
    /// public class NameListModel : Model
    /// {
    ///     private List<string> Items { get; } = ["John", "Paul", "George", "Ringo"];
    ///
    ///     public override int RowCount(ModelIndex parent)
    ///         => parent?.IsValid == true ? 0 : Items.Count;
    ///     public override int ColumnCount(ModelIndex parent) => 1;
    ///     public override ModelIndex Parent(ModelIndex index) => ModelIndex.Empty;
    ///
    ///     public override ModelIndex Index(int row, int column, ModelIndex parent)
    ///     {
    ///         if (parent?.IsValid == true || column != 0 || row < 0 || row >= Items.Count)
    ///             return ModelIndex.Empty;
    ///         return new ModelIndex(row, column);
    ///     }
    ///
    ///     public override object Data(ModelIndex index, int role)
    ///     {
    ///         if (index is not { IsValid: true } || role != Roles.DisplayRole)
    ///             return null;
    ///         return Items[index.Row];
    ///     }
    ///
    ///     public override bool InsertRows(int row, int count, ModelIndex parent = null)
    ///     {
    ///         if (parent?.IsValid == true || row < 0 || row > Items.Count || count < 1)
    ///             return false;
    ///
    ///         BeginInsertRows(ModelIndex.Empty, row, row + count - 1);
    ///         try {
    ///             Items.InsertRange(row, Enumerable.Repeat("(empty)", count));
    ///         } finally {
    ///             EndInsertRows();
    ///         }
    ///         return true;
    ///     }
    /// }
    /// ]]></code>
    /// </remarks>
    public abstract class Model
    {
        /// <summary>
        /// Identifies the kind of change described by <see cref="ModelChangeEventArgs"/>.
        /// </summary>
        [Include]
        public enum EventAction : int
        {
            NoAction = 0,
            BeginResetModel,
            EndResetModel,
            BeginInsertRows,
            EndInsertRows,
            BeginMoveRows,
            EndMoveRows,
            BeginRemoveRows,
            EndRemoveRows,
            BeginInsertColumns,
            EndInsertColumns,
            BeginMoveColumns,
            EndMoveColumns,
            BeginRemoveColumns,
            EndRemoveColumns,
            DataChanged,
            HeaderDataChanged
        }

        /// <summary>
        /// Provides built-in role identifiers for item data.
        /// </summary>
        protected static class Roles
        {
            public const int DisplayRole = 0;
            public const int DecorationRole = 1;
            public const int EditRole = 2;
            public const int ToolTipRole = 3;
            public const int StatusTipRole = 4;
            public const int WhatsThisRole = 5;
            public const int SizeHintRole = 13;
            public const int FontRole = 6;
            public const int TextAlignmentRole = 7;
            public const int BackgroundRole = 8;
            public const int ForegroundRole = 9;
            public const int CheckStateRole = 10;
            public const int InitialSortOrderRole = 14;
            public const int AccessibleTextRole = 11;
            public const int AccessibleDescriptionRole = 12;
            public const int UserRole = 0x0100;
        }

        /// <summary>
        /// Provides built-in flag values that describe item capabilities.
        /// </summary>
        protected static class ItemFlags
        {
            public const int NoItemFlags = 0;
            public const int ItemIsSelectable = 1;
            public const int ItemIsEditable = 2;
            public const int ItemIsDragEnabled = 4;
            public const int ItemIsDropEnabled = 8;
            public const int ItemIsUserCheckable = 16;
            public const int ItemIsEnabled = 32;
            public const int ItemIsAutoTristate = 64;
            public const int ItemNeverHasChildren = 128;
            public const int ItemIsUserTristate = 256;
        }

        /// <summary>
        /// Provides constants that identify horizontal and vertical headers.
        /// </summary>
        protected static class HeaderOrientation
        {
            public const int Horizontal = 1;
            public const int Vertical = 2;
        }

        /// <summary>
        /// Provides sort direction constants for subclasses.
        /// </summary>
        protected static class SortOrder
        {
            public const int Ascending = 0;
            public const int Descending = 1;
        }

        /// <summary>
        /// Provides matching behavior flags for subclasses.
        /// </summary>
        protected static class MatchFlags
        {
            public const int MatchExactly = 0;
            public const int MatchFixedString = 8;
            public const int MatchContains = 1;
            public const int MatchStartsWith = 2;
            public const int MatchEndsWith = 3;
            public const int MatchCaseSensitive = 16;
            public const int MatchRegularExpression = 4;
            public const int MatchWildcard = 5;
            public const int MatchWrap = 32;
            public const int MatchRecursive = 64;
        }

        /// <summary>
        /// Provides drag-and-drop action constants for subclasses.
        /// </summary>
        protected static class DropActions
        {
            public const int CopyAction = 0x1;
            public const int MoveAction = 0x2;
            public const int LinkAction = 0x4;
            public const int ActionMask = 0xff;
            public const int IgnoreAction = 0x0;
            public const int TargetMoveAction = 0x8002;
        }

        /// <summary>
        /// Returns the capability flags for the specified item.
        /// </summary>
        /// <remarks>
        /// The default implementation marks items as enabled and selectable.
        /// </remarks>
        public virtual int Flags(ModelIndex index)
            => ItemFlags.ItemIsEnabled | ItemFlags.ItemIsSelectable;

        /// <summary>
        /// Returns the number of child rows under the specified parent index.
        /// </summary>
        public abstract int RowCount(ModelIndex parent);

        /// <summary>
        /// Returns the number of columns for the specified parent index.
        /// </summary>
        public abstract int ColumnCount(ModelIndex parent);

        /// <summary>
        /// Returns the names used to address values exposed by this model.
        /// </summary>
        /// <remarks>
        /// A role identifies a value that can be requested from an item, such as a display string,
        /// an editable value, or a custom property. Return a dictionary whose keys are role ids,
        /// such as <see cref="Roles.DisplayRole"/> or values starting at
        /// <see cref="Roles.UserRole"/>, and whose values are stable, consumer-facing role names.
        /// </remarks>
        public virtual Dictionary<int, string> RoleNames()
            => throw new NotImplementedException();

        /// <summary>
        /// Returns whether more child items can be loaded for the specified parent.
        /// </summary>
        public virtual bool CanFetchMore(ModelIndex parent)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns whether the specified parent item has any children.
        /// </summary>
        public virtual bool HasChildren(ModelIndex parent)
            => throw new NotImplementedException();

        /// <summary>
        /// Creates an index that identifies the item at the specified row and column under a
        /// parent.
        /// </summary>
        public abstract ModelIndex Index(int row, int column, ModelIndex parent);

        /// <summary>
        /// Returns the parent index for the specified item.
        /// </summary>
        public abstract ModelIndex Parent(ModelIndex index);

        /// <summary>
        /// Returns another item with the same parent as the specified item.
        /// </summary>
        public virtual ModelIndex Sibling(int row, int column, ModelIndex index)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns an alternate item that should be edited instead of the specified item.
        /// </summary>
        public virtual ModelIndex Buddy(ModelIndex index)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns the value stored for the specified item and role.
        /// </summary>
        public abstract object Data(ModelIndex index, int role);

        /// <summary>
        /// Returns the value for a header section and role.
        /// </summary>
        public virtual object HeaderData(int section, int orientation, int role)
            => throw new NotImplementedException();

        /// <summary>
        /// Inserts rows under the specified parent item.
        /// </summary>
        /// <remarks>
        /// If implemented, call <see cref="BeginInsertRows(ModelIndex, int, int)"/> before
        /// changing the backing storage and <see cref="EndInsertRows()"/> immediately after the
        /// insertion completes successfully. Once <c>BeginInsertRows</c> has been called, the
        /// matching <c>EndInsertRows</c> call must still happen, typically from a
        /// <c>try</c>/<c>finally</c> block.
        /// </remarks>
        public virtual bool InsertRows(int row, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        /// <summary>
        /// Inserts columns under the specified parent item.
        /// </summary>
        public virtual bool InsertColumns(int column, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        /// <summary>
        /// Moves rows from one parent and position to another.
        /// </summary>
        public virtual bool MoveRows(ModelIndex sourceParent, int sourceRow, int count,
            ModelIndex destinationParent, int destinationChild)
            => throw new NotImplementedException();

        /// <summary>
        /// Moves columns from one parent and position to another.
        /// </summary>
        public virtual bool MoveColumns(ModelIndex sourceParent, int sourceColumn, int count,
            ModelIndex destinationParent, int destinationChild)
            => throw new NotImplementedException();

        /// <summary>
        /// Removes rows under the specified parent item.
        /// </summary>
        /// <remarks>
        /// If implemented, call <see cref="BeginRemoveRows(ModelIndex, int, int)"/> before
        /// changing the backing storage and <see cref="EndRemoveRows()"/> immediately after the
        /// removal completes successfully. Once <c>BeginRemoveRows</c> has been called, the
        /// matching <c>EndRemoveRows</c> call must still happen, typically from a
        /// <c>try</c>/<c>finally</c> block.
        /// </remarks>
        public virtual bool RemoveRows(int row, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        /// <summary>
        /// Removes columns under the specified parent item.
        /// </summary>
        public virtual bool RemoveColumns(int column, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        /// <summary>
        /// Reorders the model by the specified column and direction.
        /// </summary>
        public virtual void Sort(int column, int order)
            => throw new NotImplementedException();

        /// <summary>
        /// Loads additional child items for the specified parent.
        /// </summary>
        public virtual void FetchMore(ModelIndex parent)
            => throw new NotImplementedException();

        /// <summary>
        /// Clears all values associated with the specified item.
        /// </summary>
        public virtual bool ClearItemData(ModelIndex index)
            => throw new NotImplementedException();

        /// <summary>
        /// Updates the value stored for the specified item and role.
        /// </summary>
        /// <remarks>
        /// Call <see cref="DataChanged(ModelIndex, ModelIndex, int[])"/> after a successful
        /// in-place edit so that connected consumers refresh the affected data.
        /// </remarks>
        public virtual bool SetData(ModelIndex index, object value, int role)
            => throw new NotImplementedException();

        /// <summary>
        /// Updates the value of a header section for the specified role.
        /// </summary>
        public virtual bool SetHeaderData(int section, int orientation,
            object value, int role)
            => throw new NotImplementedException();

        #region Unsupported overrides //////////////////////////////////////////////////////////////

        internal virtual (int Width, int Height) Span(ModelIndex index)
            => throw new NotImplementedException();

        internal virtual void MultiData(ModelIndex index, IDictionary<int, object> roleDataSpan)
            => throw new NotImplementedException();

        internal virtual IDictionary<int, object> ItemData(ModelIndex index)
            => throw new NotImplementedException();

        internal virtual bool SetItemData(ModelIndex index, IDictionary<int, object> roles)
            => throw new NotImplementedException();

        internal virtual ModelIndex[] Match(ModelIndex start, int role, object value, int hits = 1,
            int flags = (int)(MatchFlags.MatchStartsWith | MatchFlags.MatchWrap))
            => throw new NotImplementedException();

        internal virtual string[] MimeTypes()
            => throw new NotImplementedException();

        internal virtual IMimeData MimeData(ModelIndex[] indexes)
            => throw new NotImplementedException();

        internal virtual int SupportedDragActions()
            => throw new NotImplementedException();

        internal virtual int SupportedDropActions()
            => throw new NotImplementedException();

        internal virtual bool CanDropMimeData(IMimeData data, int action,
            int row, int column, ModelIndex parent)
            => throw new NotImplementedException();

        internal virtual bool DropMimeData(IMimeData data, int action,
            int row, int column, ModelIndex parent)
            => throw new NotImplementedException();

        #endregion Unsupported overrides ///////////////////////////////////////////////////////////

        /// <summary>
        /// Raised when the model reports a structural or data change.
        /// </summary>
        /// <remarks>
        /// This event is used by the generated bridge layer to forward model updates to consumers.
        /// Application model code normally uses the protected <c>Begin*</c>, <c>End*</c>, and
        /// <c>*Changed</c> methods instead of raising this event directly.
        /// </remarks>
        [Enable]
        public event EventHandler<ModelChangeEventArgs> ModelChanged;

        private enum Sync { None, Enter, Exit }

        private bool EventSync(ModelChangeEventArgs args)
        {
            if (args.Synchronized)
                return true;
            Qml.ProcessEvents();
            return args.Synchronized;
        }

        private bool EnterCriticalSection()
        {
            if (Monitor.TryEnter(CriticalSection))
                return true;
            Qml.ProcessEvents();
            return Monitor.TryEnter(CriticalSection);
        }

        private void OnModelChanged(Sync sync, ModelChangeEventArgs args)
        {
            if (sync == Sync.Enter)
                SpinWait.SpinUntil(EnterCriticalSection);
            try {
                if (ModelChanged != null) {
                    ModelChanged.Invoke(this, args);
                    if (sync != Sync.None)
                        SpinWait.SpinUntil(() => EventSync(args));
                }
            } finally {
                if (sync == Sync.Exit)
                    Monitor.Exit(CriticalSection);
            }
        }

        private readonly object CriticalSection = new();

        /// <summary>
        /// Notifies that columns are about to be inserted under the specified parent.
        /// </summary>
        /// <param name="parent">The parent whose child columns will change.</param>
        /// <param name="first">The first inserted column index.</param>
        /// <param name="last">The inclusive last inserted column index.</param>
        /// <remarks>
        /// Call this method immediately before mutating the backing storage. Each call must be
        /// paired with <see cref="EndInsertColumns()"/> once the begin/end section has been
        /// entered.
        /// </remarks>
        protected void BeginInsertColumns(ModelIndex parent, int first, int last)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginInsertColumns,
                Parent = parent,
                First = first,
                Last = last
            });
        }

        /// <summary>
        /// Completes a column insertion previously started with
        /// <see cref="BeginInsertColumns(ModelIndex, int, int)"/>.
        /// </summary>
        protected void EndInsertColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndInsertColumns
            });
        }

        /// <summary>
        /// Notifies that columns are about to move to a new parent and destination.
        /// </summary>
        protected void BeginMoveColumns(
            ModelIndex sourceParent, int sourceFirst, int sourceLast,
            ModelIndex destinationParent, int destinationChild)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginMoveColumns,
                Parent = sourceParent,
                First = sourceFirst,
                Last = sourceLast,
                DestinationParent = destinationParent,
                DestinationChild = destinationChild
            });
        }

        /// <summary>
        /// Completes a column move previously started with
        /// <see cref="BeginMoveColumns(ModelIndex, int, int, ModelIndex, int)"/>.
        /// </summary>
        protected void EndMoveColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndMoveColumns
            });
        }

        /// <summary>
        /// Notifies that columns are about to be removed under the specified parent.
        /// </summary>
        /// <param name="parent">The parent whose child columns will change.</param>
        /// <param name="first">The first removed column index.</param>
        /// <param name="last">The inclusive last removed column index.</param>
        /// <remarks>
        /// Call this method immediately before mutating the backing storage. Each call must be
        /// paired with <see cref="EndRemoveColumns()"/> once the begin/end section has been
        /// entered.
        /// </remarks>
        protected void BeginRemoveColumns(ModelIndex parent, int first, int last)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginRemoveColumns,
                Parent = parent,
                First = first,
                Last = last
            });
        }

        /// <summary>
        /// Completes a column removal previously started with
        /// <see cref="BeginRemoveColumns(ModelIndex, int, int)"/>.
        /// </summary>
        protected void EndRemoveColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndRemoveColumns
            });
        }

        /// <summary>
        /// Notifies that rows are about to be inserted under the specified parent.
        /// </summary>
        /// <param name="parent">The parent whose child rows will change.</param>
        /// <param name="first">The first inserted row index.</param>
        /// <param name="last">The inclusive last inserted row index.</param>
        /// <remarks>
        /// Call this method immediately before mutating the backing storage. Each call must be
        /// paired with <see cref="EndInsertRows()"/> once the begin/end section has been entered.
        /// </remarks>
        protected void BeginInsertRows(ModelIndex parent, int first, int last)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginInsertRows,
                Parent = parent,
                First = first,
                Last = last
            });
        }

        /// <summary>
        /// Completes a row insertion previously started with
        /// <see cref="BeginInsertRows(ModelIndex, int, int)"/>.
        /// </summary>
        protected void EndInsertRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndInsertRows
            });
        }

        /// <summary>
        /// Notifies that rows are about to move to a new parent and destination.
        /// </summary>
        protected void BeginMoveRows(ModelIndex sourceParent, int sourceFirst, int sourceLast,
            ModelIndex destinationParent, int destinationChild)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginMoveRows,
                Parent = sourceParent,
                First = sourceFirst,
                Last = sourceLast,
                DestinationParent = destinationParent,
                DestinationChild = destinationChild
            });
        }

        /// <summary>
        /// Completes a row move previously started with
        /// <see cref="BeginMoveRows(ModelIndex, int, int, ModelIndex, int)"/>.
        /// </summary>
        protected void EndMoveRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndMoveRows
            });
        }

        /// <summary>
        /// Notifies that rows are about to be removed under the specified parent.
        /// </summary>
        /// <param name="parent">The parent whose child rows will change.</param>
        /// <param name="first">The first removed row index.</param>
        /// <param name="last">The inclusive last removed row index.</param>
        /// <remarks>
        /// Call this method immediately before mutating the backing storage. Each call must be
        /// paired with <see cref="EndRemoveRows()"/> once the begin/end section has been entered.
        /// </remarks>
        protected void BeginRemoveRows(ModelIndex parent, int first, int last)
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginRemoveRows,
                Parent = parent,
                First = first,
                Last = last
            });
        }

        /// <summary>
        /// Completes a row removal previously started with
        /// <see cref="BeginRemoveRows(ModelIndex, int, int)"/>.
        /// </summary>
        protected void EndRemoveRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndRemoveRows
            });
        }

        /// <summary>
        /// Notifies that the entire model is about to be reset.
        /// </summary>
        /// <remarks>
        /// Use a reset when the structure or contents change so broadly that fine-grained insert,
        /// remove, move, or data notifications are no longer practical. Each call must be paired
        /// with <see cref="EndResetModel()"/> once the begin/end section has been entered.
        /// </remarks>
        protected void BeginResetModel()
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginResetModel
            });
        }

        /// <summary>
        /// Completes a model reset previously started with <see cref="BeginResetModel()"/>.
        /// </summary>
        protected void EndResetModel()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndResetModel
            });
        }

        /// <summary>
        /// Notifies that existing item values changed within the specified inclusive range.
        /// </summary>
        /// <param name="topLeft">The top-left changed index.</param>
        /// <param name="bottomRight">The bottom-right changed index.</param>
        /// <param name="roles">
        /// The affected roles, or <see langword="null"/> to indicate a general value update.
        /// </param>
        /// <remarks>
        /// Use this for in-place value edits only. Do not use it for insertions, removals, moves,
        /// or resets.
        /// </remarks>
        protected void DataChanged(ModelIndex topLeft, ModelIndex bottomRight, int[] roles = null)
        {
            OnModelChanged(Sync.None, new()
            {
                Action = EventAction.DataChanged,
                TopLeft = topLeft,
                BottomRight = bottomRight,
                Roles = roles?.ToList() ?? []
            });
        }

        /// <summary>
        /// Notifies that header values changed for the specified inclusive section range.
        /// </summary>
        /// <param name="orientation">
        /// The header orientation, typically <see cref="HeaderOrientation.Horizontal"/> or
        /// <see cref="HeaderOrientation.Vertical"/>.
        /// </param>
        /// <param name="first">The first changed section index.</param>
        /// <param name="last">The inclusive last changed section index.</param>
        protected void HeaderDataChanged(int orientation, int first, int last)
        {
            OnModelChanged(Sync.None, new()
            {
                Action = EventAction.HeaderDataChanged,
                Orientation = orientation,
                First = first,
                Last = last
            });
        }
    }
}
