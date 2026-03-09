// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Qt.DotNet;
using Qt.Quick;
using Qt.Bridge.Mime;

namespace Qt.Bridge.Models
{
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

    public abstract class Model
    {
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

        protected static class HeaderOrientation
        {
            public const int Horizontal = 1;
            public const int Vertical = 2;
        }

        protected static class SortOrder
        {
            public const int Ascending = 0;
            public const int Descending = 1;
        }

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

        protected static class DropActions
        {
            public const int CopyAction = 0x1;
            public const int MoveAction = 0x2;
            public const int LinkAction = 0x4;
            public const int ActionMask = 0xff;
            public const int IgnoreAction = 0x0;
            public const int TargetMoveAction = 0x8002;
        }

        public virtual int Flags(ModelIndex index)
            => ItemFlags.ItemIsEnabled | ItemFlags.ItemIsSelectable;

        public abstract int RowCount(ModelIndex parent);

        public abstract int ColumnCount(ModelIndex parent);

        public virtual Dictionary<int, string> RoleNames()
            => throw new NotImplementedException();

        public virtual bool CanFetchMore(ModelIndex parent)
            => throw new NotImplementedException();

        public virtual bool HasChildren(ModelIndex parent)
            => throw new NotImplementedException();

        public abstract ModelIndex Index(int row, int column, ModelIndex parent);

        public abstract ModelIndex Parent(ModelIndex index);

        public virtual ModelIndex Sibling(int row, int column, ModelIndex index)
            => throw new NotImplementedException();

        public virtual ModelIndex Buddy(ModelIndex index)
            => throw new NotImplementedException();

        public abstract object Data(ModelIndex index, int role);

        public virtual object HeaderData(int section, int orientation, int role)
            => throw new NotImplementedException();

        public virtual bool InsertRows(int row, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        public virtual bool InsertColumns(int column, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        public virtual bool MoveRows(ModelIndex sourceParent, int sourceRow, int count,
            ModelIndex destinationParent, int destinationChild)
            => throw new NotImplementedException();

        public virtual bool MoveColumns(ModelIndex sourceParent, int sourceColumn, int count,
            ModelIndex destinationParent, int destinationChild)
            => throw new NotImplementedException();

        public virtual bool RemoveRows(int row, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        public virtual bool RemoveColumns(int column, int count, ModelIndex parent = default)
            => throw new NotImplementedException();

        public virtual void Sort(int column, int order)
            => throw new NotImplementedException();

        public virtual void FetchMore(ModelIndex parent)
            => throw new NotImplementedException();

        public virtual bool ClearItemData(ModelIndex index)
            => throw new NotImplementedException();

        public virtual bool SetData(ModelIndex index, object value, int role)
            => throw new NotImplementedException();

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

        protected void EndInsertColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndInsertColumns
            });
        }

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

        protected void EndMoveColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndMoveColumns
            });
        }

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

        protected void EndRemoveColumns()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndRemoveColumns
            });
        }

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

        protected void EndInsertRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndInsertRows
            });
        }

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

        protected void EndMoveRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndMoveRows
            });
        }

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

        protected void EndRemoveRows()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndRemoveRows
            });
        }

        protected void BeginResetModel()
        {
            OnModelChanged(Sync.Enter, new()
            {
                Action = EventAction.BeginResetModel
            });
        }

        protected void EndResetModel()
        {
            OnModelChanged(Sync.Exit, new()
            {
                Action = EventAction.EndResetModel
            });
        }

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
