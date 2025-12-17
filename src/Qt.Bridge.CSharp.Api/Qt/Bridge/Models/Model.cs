/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using Qt.DotNet;
using Qt.Quick;

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
        public Model.HeaderOrientation Orientation { get; init; }
        [Enable]
        public bool Synchronized { get; set; } = false;
    }

    public abstract class Model
    {
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

        [Flags]
        public enum ItemFlags : int
        {
            NoItemFlags = 0,
            ItemIsSelectable = 1,
            ItemIsEditable = 2,
            ItemIsDragEnabled = 4,
            ItemIsDropEnabled = 8,
            ItemIsUserCheckable = 16,
            ItemIsEnabled = 32,
            ItemIsAutoTristate = 64,
            ItemNeverHasChildren = 128,
            ItemIsUserTristate = 256
        }

        [Include]
        public enum HeaderOrientation : int
        {
            HorizontalHeader = 1,
            VerticalHeader = 2
        }

        public enum SortOrder : int
        {
            Ascending = 0,
            Descending = 1
        }

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

        public abstract int ColumnCount(ModelIndex parent);
        public abstract object Data(ModelIndex index, int role);
        public abstract ModelIndex Index(int row, int column, ModelIndex parent);
        public abstract ModelIndex Parent(ModelIndex index);
        public abstract int RowCount(ModelIndex parent);

        public virtual ModelIndex Buddy(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public virtual bool HasChildren(ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public virtual ModelIndex Sibling(int row, int column, ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public virtual bool HasIndex(int row, int column, ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public virtual bool CanFetchMore(ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public virtual void FetchMore(ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public virtual ItemFlags Flags(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public virtual object HeaderData(int section, HeaderOrientation orientation, int role)
        {
            throw new NotImplementedException();
        }

        public virtual Dictionary<int, string> RoleNames()
        {
            throw new NotImplementedException();
        }

        public virtual Dictionary<int, object> ItemData(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetData(ModelIndex index, object value, int role)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetHeaderData(
            int section, HeaderOrientation orientation, object value, int role)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetItemData(ModelIndex index, Dictionary<int, object> roles)
        {
            throw new NotImplementedException();
        }

        public virtual void Sort(int column, SortOrder order)
        {
            throw new NotImplementedException();
        }

        public virtual bool ClearItemData(ModelIndex index)
        {
            throw new NotImplementedException();
        }

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

        protected void HeaderDataChanged(HeaderOrientation orientation, int first, int last)
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
