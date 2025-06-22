/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

namespace Qt.DotNet
{
    public enum ItemDataRole
    {
        DisplayRole = 0,
        DecorationRole = 1,
        EditRole = 2,
        ToolTipRole = 3,
        StatusTipRole = 4,
        WhatsThisRole = 5,
        FontRole = 6,
        TextAlignmentRole = 7,
        BackgroundRole = 8,
        ForegroundRole = 9,
        CheckStateRole = 10,
        // Accessibility
        AccessibleTextRole = 11,
        AccessibleDescriptionRole = 12,
        // More general purpose
        SizeHintRole = 13,
        InitialSortOrderRole = 14,
        // Internal UiLib roles. Start worrying when public roles go that high.
        DisplayPropertyRole = 27,
        DecorationPropertyRole = 28,
        ToolTipPropertyRole = 29,
        StatusTipPropertyRole = 30,
        WhatsThisPropertyRole = 31,
        // Reserved
        UserRole = 0x0100
    };

    [Flags]
    public enum ItemFlag
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
    };

    public interface IQAbstractListModel
    {
        int Flags(IQModelIndex index);
        bool SetData(IQModelIndex index, IQVariant value, int role);

        bool InsertRows(int row, int count, IQModelIndex parent);
        bool RemoveRows(int row, int count, IQModelIndex parent);

        void BeginInsertRows(IQModelIndex parent, int first, int last);
        void EndInsertRows();

        void BeginRemoveRows(IQModelIndex parent, int first, int last);
        void EndRemoveRows();

        IQModelIndex CreateIndex(int arow, int acolumn, IntPtr adata);

        void EmitDataChanged(IQModelIndex topLeft, IQModelIndex bottomRight, int[] roles);
    }

    public abstract class QAbstractListModel
    {
        public IQAbstractListModel Base { get; private set; }

        public QAbstractListModel()
        {
            Base = Adapter.Static.QAbstractListModel_Create(this);
        }

        public abstract int RowCount(IQModelIndex parent = null);

        public abstract IQVariant Data(IQModelIndex index,
            int role = (int)ItemDataRole.DisplayRole);

        public virtual int Flags(IQModelIndex index)
        {
            if (Base == null)
                return 0;
            var flags = (ItemFlag)Base.Flags(index);
            return (int)flags;
        }

        public virtual bool SetData(IQModelIndex index, IQVariant value,
            int role = (int)ItemDataRole.EditRole)
        {
            return Base?.SetData(index, value, role) ?? false;
        }

        public virtual bool InsertRows(int row, int count, IQModelIndex parent)
        {
            return Base?.InsertRows(row, count, parent) ?? false;
        }

        public virtual bool RemoveRows(int row, int count, IQModelIndex parent)
        {
            return Base?.RemoveRows(row, count, parent) ?? false;
        }

        public virtual string RoleNames()
        {
            return string.Empty;
        }

        protected void BeginInsertRows(IQModelIndex parent, int first, int last)
        {
            Base?.BeginInsertRows(parent, first, last);
        }

        protected void EndInsertRows()
        {
            Base?.EndInsertRows();
        }

        protected void BeginRemoveRows(IQModelIndex parent, int first, int last)
        {
            Base?.BeginRemoveRows(parent, first, last);
        }

        protected void EndRemoveRows()
        {
            Base?.EndRemoveRows();
        }

        protected IQModelIndex CreateIndex(int arow, int acolumn, IntPtr adata)
        {
            return Base?.CreateIndex(arow, acolumn, adata);
        }

        protected void EmitDataChanged(IQModelIndex topLeft, IQModelIndex bottomRight, int[] roles)
        {
            Base?.EmitDataChanged(topLeft, bottomRight, roles);
        }
    }


    public partial class Adapter
    {
        public partial interface IStatic
        {
            IQAbstractListModel QAbstractListModel_Create(object self);
        }
    }
}
