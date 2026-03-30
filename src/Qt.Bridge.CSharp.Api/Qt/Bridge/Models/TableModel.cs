// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Utils;
using Qt.DotNet;
using Qt.Quick;

namespace Qt.Bridge.Models
{
    public abstract class TableModel : Model
    {
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Parent(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override bool HasChildren(ModelIndex parent)
        {
            return base.HasChildren(parent);
        }
    }

    public abstract class TableModel<T> : TableModel
    {
        protected abstract int Rows { get; }

        protected abstract int Columns { get; }

        protected abstract T this[int row, int col] { get; set; }

        protected virtual bool ClearItemData(int row, int col) => false;

        protected virtual string RowHeader(int row) => $"R{row + 1}";
        protected virtual string ColumnHeader(int column) => $"C{column + 1}";

        protected virtual bool CanInsertRows(int row, int count) => false;
        protected virtual bool InsertRows(int row, int count) => false;

        protected virtual bool CanRemoveRows(int row, int count) => false;
        protected virtual bool RemoveRows(int row, int count) => false;

        protected virtual bool CanInsertColumns(int column, int count) => false;
        protected virtual bool InsertColumns(int column, int count) => false;

        protected virtual bool CanRemoveColumns(int column, int count) => false;
        protected virtual bool RemoveColumns(int column, int count) => false;

        protected virtual bool IsReadOnly => false;
        protected virtual bool HasItemRole => true;

        private const int ItemRole = Roles.UserRole;

        private readonly LazyFactory Lazy = new();

        private Type ItemType => Lazy.Get(() => ItemType, () => typeof(T));

        private bool ItemTypeIsConvertible => Lazy.Get(() => ItemTypeIsConvertible,
            () => ValueConverter.IsConvertible(ItemType));

        private bool ItemTypeIsModelItem => Lazy.Get(() => ItemTypeIsModelItem,
            () => ItemType.IsAssignableTo(typeof(IModelItem)));

        private bool ItemTypeIsDisplayable => Lazy.Get(() => ItemTypeIsDisplayable,
            () => ItemTypeIsConvertible || ItemType.IsAssignableTo(typeof(IDisplayable)));

        private bool ItemTypeIsEditable => Lazy.Get(() => ItemTypeIsEditable,
            () => ItemTypeIsConvertible || ItemType.IsAssignableTo(typeof(IEditable)));

        private Dictionary<int, PropertyInfo> RolePropsById => Lazy.Get(() => RolePropsById,
            () => ItemTypeIsConvertible ? []
                : ItemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select((p, i) => (Role: ItemRole + i + 1, Property: p))
                    .ToDictionary(x => x.Role, x => x.Property));

        private Dictionary<int, string> RoleNamesById => Lazy.Get(() => RoleNamesById, () =>
        {
            var roles = new Dictionary<int, string>();
            if (ItemTypeIsDisplayable)
                roles[Roles.DisplayRole] = "display";
            if (ItemTypeIsEditable && !IsReadOnly)
                roles[Roles.EditRole] = "edit";
            if (!ItemTypeIsConvertible) {
                if (HasItemRole)
                    roles[ItemRole] = "item";
                foreach (var prop in RolePropsById) {
                    roles[prop.Key] = prop.Value.Name.ToQmlPropertyName();
                }
            }
            return roles;
        });

        private T this[ModelIndex idx]
        {
            get => idx is { IsValid: true } ? this[idx.Row, idx.Column] : default;
            set
            {
                if (idx is { IsValid: true })
                    this[idx.Row, idx.Column] = value;
            }
        }

        public sealed override int RowCount(ModelIndex idx) => idx is { IsValid: true } ? 0 : Rows;

        public sealed override int ColumnCount(ModelIndex idx) => Columns;

        public sealed override int Flags(ModelIndex index)
        {
            if (index is not { IsValid: true })
                return ItemFlags.NoItemFlags;

            var item = this[index];
            int flags = ItemFlags.NoItemFlags;
            if (ItemTypeIsModelItem) {
                if (item is IModelItem { IsEnabled: true })
                    flags |= ItemFlags.ItemIsEnabled;
                if (item is IModelItem { IsSelectable: true })
                    flags |= ItemFlags.ItemIsSelectable;
            } else {
                flags = base.Flags(index);
            }

            if (!IsReadOnly && (ItemTypeIsConvertible || item is IEditable { IsEditable: true }))
                flags |= ItemFlags.ItemIsEditable;

            return flags;
        }

        public sealed override Dictionary<int, string> RoleNames() => RoleNamesById;

        public sealed override object Data(ModelIndex index, int role)
        {
            if (index is not { IsValid: true } || this[index] is not { } item)
                return null;

            if (role == Roles.DisplayRole) {
                if (ItemTypeIsConvertible)
                    return item;
                if (item is IDisplayable { } displayableItem)
                    return displayableItem.DisplayValue;
                return null;
            }

            if (role == Roles.EditRole) {
                if (ItemTypeIsConvertible)
                    return item;
                if (item is IEditable { IsEditable: true } editableItem)
                    return editableItem.EditValue;
                return null;
            }

            if (role == ItemRole)
                return item;

            if (!RolePropsById.TryGetValue(role, out var prop) || prop is not { CanRead: true })
                return null;

            return prop.GetValue(item);
        }

        public sealed override bool SetData(ModelIndex index, object value, int role)
        {
            if (IsReadOnly)
                return false;

            if (index is not { IsValid: true } || this[index] is not { } item)
                return false;

            if (role == Roles.EditRole) {
                if (ItemTypeIsConvertible) {
                    this[index] = ValueConverter.ToValue<T>(value);
                    DataChanged(index, index);
                    return true;
                }

                if (item is IEditable { IsEditable: true } editableItem) {
                    editableItem.EditValue = value;
                    DataChanged(index, index);
                    return true;
                }

                return false;
            }

            if (role == ItemRole && HasItemRole && value is T newItem) {
                this[index] = newItem;
                DataChanged(index, index);
                return true;
            }

            if (!RolePropsById.TryGetValue(role, out var prop) || prop is not { CanWrite: true })
                return false;

            try {
                prop.SetValue(item, value);
                DataChanged(index, index);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public sealed override object HeaderData(int section, int orientation, int role)
        {
            return (orientation, role) switch
            {
                (HeaderOrientation.Horizontal, Roles.DisplayRole)
                    when 0 <= section && section < Columns => ColumnHeader(section),
                (HeaderOrientation.Vertical, Roles.DisplayRole)
                    when 0 <= section && section < Rows => RowHeader(section),
                _ => null
            };
        }

        public sealed override bool InsertRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanInsertRows(row, count))
                return false;
            BeginInsertRows(parent, row, row + count - 1);
            if (!InsertRows(row, count))
                return false;
            EndInsertRows();
            return true;
        }

        public sealed override bool RemoveRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanRemoveRows(row, count))
                return false;
            BeginRemoveRows(parent, row, row + count - 1);
            if (!RemoveRows(row, count))
                return false;
            EndRemoveRows();
            return true;
        }

        public sealed override bool InsertColumns(int column, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanInsertColumns(column, count))
                return false;
            BeginInsertColumns(parent, column, column + count - 1);
            if (!InsertColumns(column, count))
                return false;
            EndInsertColumns();
            return true;
        }

        public sealed override bool RemoveColumns(int column, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanRemoveColumns(column, count))
                return false;
            BeginRemoveColumns(parent, column, column + count - 1);
            if (!RemoveColumns(column, count))
                return false;
            EndRemoveColumns();
            return true;
        }

        public sealed override bool ClearItemData(ModelIndex index)
        {
            if (index is not { IsValid: true })
                return false;
            return ClearItemData(index.Row, index.Column);
        }

        protected void DataChanged(int row, int column)
        {
            DataChanged(row, column, row, column);
        }

        protected void DataChanged(int topRow, int leftColumn, int bottomRow, int rightColumn)
        {
            var topLeft = new ModelIndex(topRow, leftColumn);
            var bottomRight = new ModelIndex(bottomRow, rightColumn);
            DataChanged(topLeft, bottomRight);
        }

        [Qt.Ignore]
        public sealed override bool SetHeaderData(
            int section, int orientation, object value, int role) => default;
        [Qt.Ignore]
        public sealed override ModelIndex Buddy(ModelIndex index) => default;
        [Qt.Ignore]
        public sealed override bool CanFetchMore(ModelIndex parent) => default;
        [Qt.Ignore]
        public sealed override void FetchMore(ModelIndex parent) { }
        [Qt.Ignore]
        public sealed override bool MoveColumns(
            ModelIndex sourceParent, int sourceColumn, int count,
            ModelIndex destinationParent, int destinationChild) => default;
        [Qt.Ignore]
        public sealed override bool MoveRows(
            ModelIndex sourceParent, int sourceRow, int count,
            ModelIndex destinationParent, int destinationChild) => default;
        [Qt.Ignore]
        public sealed override void Sort(int column, int order) { }
    }
}
