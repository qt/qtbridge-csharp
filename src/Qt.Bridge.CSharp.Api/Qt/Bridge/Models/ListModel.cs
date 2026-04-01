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

        private Dictionary<int, PropertyInfo> _RoleMap = null;
        private Dictionary<int, string> _RoleNames = null;

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
                if (_RoleMap == null) {
                    var type = typeof(T);
                    _RoleMap = new() { { Roles.UserRole, null } };
                    _RoleNames = new() { { Roles.UserRole, "item" } };
                    if (!ValueConverter.IsConvertible(type)) {
                        int i = 0;
                        foreach (var prop in type.GetProperties()) {
                            ++i;
                            _RoleMap[Roles.UserRole + i] = prop;
                            _RoleNames[Roles.UserRole + i] = prop.Name.ToQmlPropertyName();
                        }
                    }
                }
                return _RoleMap;
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
            if (!RoleMap.Any())
                return null;
            return _RoleNames;
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
            if (!RoleMap.TryGetValue(role, out var property))
                return null;
            if (Data(index.Row) is not { } data)
                return null;
            if (property == null)
                return data;
            return property.GetValue(data);
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
