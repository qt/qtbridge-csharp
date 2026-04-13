// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.DotNet;

namespace Qt.Bridge.Models
{
    using Qt.Quick;

    /// <summary>
    /// Represents a flat one-dimensional model with no parent-child hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="ListModel"/> or <see cref="ListModel{T}"/> when your data is naturally
    /// arranged as a simple sequence of items, such as a list of names, products, or messages. This
    /// base class fixes the hierarchical parts of <see cref="Model"/> so subclasses can focus on
    /// item count, item data, and list-specific change notifications.
    /// </para>
    /// <para>
    /// In practical terms, a list model has exactly one column at the root level and no child
    /// items. Valid parent indexes are treated as having zero rows and zero columns. If your data
    /// is arranged as rows and columns, prefer <see cref="TableModel"/> instead.
    /// </para>
    /// <para>
    /// Most applications should derive from <see cref="ListModel{T}"/> rather than directly from
    /// <see cref="ListModel"/>, because the generic form automatically maps item values or public
    /// item properties to named roles.
    /// </para>
    /// </remarks>
    public abstract class ListModel : Model
    {
        [Qt.Ignore]
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
            => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index)
            => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override int ColumnCount(ModelIndex parent)
            => parent?.IsValid == true ? 0 : 1;

        [Qt.Ignore]
        public sealed override ModelIndex Parent(ModelIndex index) => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override bool HasChildren(ModelIndex parent)
            => parent?.IsValid != true && RowCount(parent) > 0;
    }

    /// <summary>
    /// Provides a strongly typed base class for implementing a flat list model.
    /// </summary>
    /// <typeparam name="T">
    /// The item type exposed by the model. This can be a simple value such as <see cref="string"/>
    /// or a custom type with multiple public properties.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Derive from <see cref="ListModel{T}"/> when each row in the list can be represented as a
    /// value of type <typeparamref name="T"/>. Implement <see cref="ItemCount"/> to report the
    /// number of items and <see cref="Data(int)"/> to return the item at a given row.
    /// </para>
    /// <para>
    /// If <typeparamref name="T"/> is a simple convertible type such as <see cref="string"/> or
    /// <see cref="int"/>, the model automatically exposes the value through the <c>item</c> role.
    /// If <typeparamref name="T"/> is a custom type, the model still exposes the whole item through
    /// the <c>item</c> role and also exposes each public instance property as an additional named
    /// role.
    /// </para>
    /// <para>
    /// This generic base class is intentionally lightweight. Unlike <see cref="TableModel{T}"/>, it
    /// does not add built-in editing or display-specific interfaces. If you need fully custom role
    /// names or custom role lookup logic, derive from <see cref="ListModel"/> instead and override
    /// <see cref="Model.RoleNames"/> and <see cref="Model.Data(ModelIndex, int)"/> yourself.
    /// </para>
    /// <code language="csharp"><![CDATA[
    /// public class NameList : ListModel<string>
    /// {
    ///     private List<string> Names { get; } = ["Ada", "Linus", "Grace"];
    ///
    ///     public override int ItemCount() => Names.Count;
    ///
    ///     public override string Data(int index)
    ///     {
    ///         if (index < 0 || index >= Names.Count)
    ///             return null;
    ///         return Names[index];
    ///     }
    /// }
    /// ]]></code>
    /// </remarks>
    public abstract class ListModel<T> : ListModel
    {
        private const int ItemRole = Roles.UserRole;

        /// <summary>
        /// Returns the number of items in the list.
        /// </summary>
        public abstract int ItemCount();

        /// <summary>
        /// Returns the item at the specified row.
        /// </summary>
        /// <remarks>
        /// Implement this as a simple row lookup against your backing collection. The generic base
        /// class validates the incoming <see cref="ModelIndex"/> before calling this method. Return
        /// <see langword="null"/> for invalid rows when <typeparamref name="T"/> is a reference
        /// type.
        /// </remarks>
        public abstract T Data(int index);

        protected virtual bool SetData(int index, T value) => false;

        protected virtual bool ClearItemData(int index) => false;

        protected virtual bool IsReadOnly => false;

        protected virtual bool HasItemRole => true;

        private readonly Type itemType = typeof(T);
        private bool ItemTypeIsConvertible => ValueConverter.IsConvertible(itemType);
        private bool ItemTypeIsModelItem => itemType.IsAssignableTo(typeof(IModelItem));
        private bool ItemTypeIsDisplayable
            => ItemTypeIsConvertible || itemType.IsAssignableTo(typeof(IDisplayable));
        private bool ItemTypeIsEditable
            => ItemTypeIsConvertible || itemType.IsAssignableTo(typeof(IEditable));

        private Dictionary<int, PropertyInfo> roleMap;
        private Dictionary<int, string> roleNames;

        /// <summary>
        /// Gets the role-to-property map inferred from <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// The map always includes <see cref="Model.Roles.UserRole"/> for the whole item under the
        /// <c>item</c> role. For custom item types, each public instance property is assigned the
        /// next available role id and exposed under its QML-style property name.
        /// </remarks>
        protected Dictionary<int, PropertyInfo> RoleMap
        {
            get
            {
                if (roleMap != null)
                    return roleMap;

                roleMap = new Dictionary<int, PropertyInfo>();
                if (ItemTypeIsConvertible)
                    return roleMap;

                var props = itemType.GetProperties();
                for (int i = 0; i < props.Length; ++i)
                    roleMap[ItemRole + i + 1] = props[i];
                return roleMap;
            }
        }

        /// <summary>
        /// Returns the role names inferred from <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// The returned dictionary always includes the <c>item</c> role for the whole list item.
        /// For custom item types, additional roles are generated from public instance properties.
        /// </remarks>
        public sealed override Dictionary<int, string> RoleNames()
        {
            if (roleNames != null)
                return roleNames.Any() ? roleNames : null;

            roleNames = new Dictionary<int, string>();
            if (ItemTypeIsDisplayable)
                roleNames[Roles.DisplayRole] = "display";
            if (ItemTypeIsEditable && !IsReadOnly)
                roleNames[Roles.EditRole] = "edit";
            if (HasItemRole)
                roleNames[ItemRole] = "item";
            foreach (var entry in RoleMap)
                roleNames[entry.Key] = entry.Value.Name.ToQmlPropertyName();
            return roleNames.Any() ? roleNames : null;
        }

        /// <summary>
        /// Returns the value for the specified row and role.
        /// </summary>
        /// <remarks>
        /// For the <c>item</c> role, this returns the full item from <see cref="Data(int)"/>. For
        /// property-based roles, it returns the corresponding public property value from the item.
        /// Invalid model indexes are rejected before the row lookup is attempted.
        /// </remarks>
        public sealed override object Data(ModelIndex index, int role)
        {
            if (index is not { IsValid: true } || index.Row < 0)
                return null;
            if (Data(index.Row) is not { } data)
                return null;

            switch (role) {
            case Roles.DisplayRole when ItemTypeIsConvertible:
                return data;
            case Roles.DisplayRole when data is IDisplayable displayableItem:
                return displayableItem.DisplayValue;
            case Roles.DisplayRole:
                return null;
            case Roles.EditRole when ItemTypeIsConvertible:
                return data;
            case Roles.EditRole when data is IEditable { IsEditable: true } editableItem:
                return editableItem.EditValue;
            case Roles.EditRole:
                return null;
            case ItemRole when HasItemRole:
                return data;
            }

            if (!RoleMap.TryGetValue(role, out var property) || property is not { CanRead: true })
                return null;
            return property.GetValue(data);
        }

        public sealed override int Flags(ModelIndex index)
        {
            if (index is not { IsValid: true } || Data(index.Row) is not { } item)
                return ItemFlags.NoItemFlags;

            var flags = ItemFlags.NoItemFlags;
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

        /// <summary>
        /// Returns the number of rows for the specified parent index.
        /// </summary>
        /// <remarks>
        /// For the root level, this returns <see cref="ItemCount"/>. For valid parent indexes, it
        /// returns <c>0</c> because <see cref="ListModel{T}"/> is always a flat list.
        /// </remarks>
        public sealed override int RowCount(ModelIndex parent)
        {
            return parent?.IsValid == true ? 0 : ItemCount();
        }

        public sealed override bool SetData(ModelIndex index, object value, int role)
        {
            if (IsReadOnly)
                return false;
            if (index is not { IsValid: true } || index.Row < 0 || Data(index.Row) is not { } item)
                return false;

            switch (role) {
            case Roles.EditRole when ItemTypeIsConvertible: {
                    if (!SetData(index.Row, ValueConverter.ToValue<T>(value)))
                        return false;
                    DataChanged(index, index);
                    return true;
                }
            case Roles.EditRole when item is IEditable { IsEditable: true } editableItem:
                editableItem.EditValue = value;
                DataChanged(index, index);
                return true;
            case Roles.EditRole:
                return false;
            case ItemRole when HasItemRole && value is T newItem: {
                    if (!SetData(index.Row, newItem))
                        return false;
                    DataChanged(index, index);
                    return true;
                }
            }

            if (!RoleMap.TryGetValue(role, out var prop) || prop is not { CanWrite: true })
                return false;

            try {
                prop.SetValue(item, value);
                DataChanged(index, index);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public sealed override bool ClearItemData(ModelIndex index)
        {
            if (index is not { IsValid: true } || index.Row < 0)
                return false;
            return ClearItemData(index.Row);
        }

        protected void DataChanged(int index)
        {
            var idx = new ModelIndex(index, 0);
            DataChanged(idx, idx);
        }

        /// <summary>
        /// Begins an item insertion notification for the specified inclusive item range.
        /// </summary>
        /// <remarks>
        /// Call this immediately before inserting items into the backing collection. Pair it with
        /// <see cref="EndInsertItems"/> in a <c>try</c>/<c>finally</c> block.
        /// </remarks>
        protected void BeginInsertItems(int first, int last)
        {
            BeginInsertRows(ModelIndex.Empty, first, last);
        }

        /// <summary> Ends the current item insertion notification sequence. </summary>
        protected void EndInsertItems()
        {
            EndInsertRows();
        }

        /// <summary>
        /// Begins an item removal notification for the specified inclusive item range.
        /// </summary>
        /// <remarks>
        /// Call this immediately before removing items from the backing collection. Pair it with
        /// <see cref="EndRemoveItems"/> in a <c>try</c>/<c>finally</c> block.
        /// </remarks>
        protected void BeginRemoveItems(int first, int last)
        {
            BeginRemoveRows(ModelIndex.Empty, first, last);
        }

        /// <summary> Ends the current item removal notification sequence. </summary>
        protected void EndRemoveItems()
        {
            EndRemoveRows();
        }
    }
}
