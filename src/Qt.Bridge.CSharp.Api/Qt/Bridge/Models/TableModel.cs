// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Bridge.Utils;
using Qt.DotNet;
using Qt.Quick;

namespace Qt.Bridge.Models
{
    /// <summary>
    /// Represents a flat two-dimensional model with no parent-child hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="TableModel"/> or <see cref="TableModel{T}"/> when your data is naturally
    /// arranged as rows and columns, such as a spreadsheet, a database result set, or any grid of
    /// values. This base class fixes the hierarchical parts of <see cref="Model"/> so subclasses
    /// can focus on table-specific behavior.
    /// </para>
    /// <para>
    /// In practical terms, every item in the model is addressed by a row and a column. Items do not
    /// have children, and valid parent indexes are treated as having no rows and no columns. If you
    /// need tree-like data, derive from <see cref="Model"/> instead.
    /// </para>
    /// <para>
    /// Most applications should derive from <see cref="TableModel{T}"/> rather than directly from
    /// <see cref="TableModel"/>, because the generic form provides automatic role handling, editing
    /// support, and row/column change helpers for a strongly typed cell value.
    /// </para>
    /// </remarks>
    public abstract class TableModel : Model
    {
        [Qt.Ignore]
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
            => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index)
            => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override ModelIndex Parent(ModelIndex index) => ModelIndex.Empty;

        [Qt.Ignore]
        public sealed override bool HasChildren(ModelIndex parent)
            => parent?.IsValid != true && RowCount(parent) > 0 && ColumnCount(parent) > 0;
    }

    /// <summary>
    /// Provides a strongly typed base class for implementing a table model.
    /// </summary>
    /// <typeparam name="T">
    /// The cell value type exposed by the model. This can be a simple value such as <see
    /// cref="string"/> or <see cref="int"/>, or a richer object type that exposes multiple values
    /// through properties.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// Derive from <see cref="TableModel{T}"/> when each cell in your table can be represented as a
    /// value of type <typeparamref name="T"/>. Implement <see cref="Rows"/>, <see cref="Columns"/>,
    /// and the row/column indexer to describe the table shape and read or write cell values.
    /// </para>
    /// <para>
    /// If <typeparamref name="T"/> is a simple convertible type such as <see cref="string"/> or
    /// <see cref="int"/>, the model automatically exposes that value through the built-in display
    /// and edit roles. If <typeparamref name="T"/> is a custom type, public instance properties of
    /// that type are exposed automatically as named roles, and the whole item can also be exposed
    /// through the <c>item</c> role.
    /// </para>
    /// <para>
    /// Custom item types can opt into additional behavior by implementing <see
    /// cref="IDisplayable"/>, <see cref="IEditable"/>, or <see cref="IModelItem"/>: <see
    /// cref="IDisplayable"/> controls the value shown by default, <see cref="IEditable"/> enables
    /// in-place editing through the built-in edit role, and <see cref="IModelItem"/> controls
    /// whether a cell is enabled or selectable.
    /// </para>
    /// <para>
    /// Row and column insert/remove operations follow the same begin/end notification contract as
    /// <see cref="Model"/>. The public overrides are sealed and handled by this base class. To add
    /// or remove rows or columns, implement the corresponding <c>Can*</c> and protected mutation
    /// methods such as <see cref="CanInsertRows"/> and <see cref="InsertRows(int, int)"/>.
    /// </para>
    /// <![CDATA[
    /// ```csharp
    /// public class ScoreTable : TableModel<int>
    /// {
    ///     private readonly int[,] values =
    ///     {
    ///         { 10, 20, 30 },
    ///         { 40, 50, 60 }
    ///     };
    ///
    ///     protected override int Rows => values.GetLength(0);
    ///     protected override int Columns => values.GetLength(1);
    ///
    ///     protected override int this[int row, int col]
    ///     {
    ///         get => values[row, col];
    ///         set => values[row, col] = value;
    ///     }
    ///
    ///     protected override string ColumnHeader(int column) => column switch
    ///     {
    ///         0 => "A",
    ///         1 => "B",
    ///         2 => "C",
    ///         _ => string.Empty
    ///     };
    /// }
    /// ```
    /// ]]>
    /// </remarks>
    public abstract class TableModel<T> : TableModel
    {
        /// <summary>
        /// Gets the number of rows in the table.
        /// </summary>
        /// <remarks>
        /// Return the current root-level row count. The base class handles valid parent indexes for
        /// you and reports zero child rows automatically.
        /// </remarks>
        protected abstract int Rows { get; }

        /// <summary>
        /// Gets the number of columns in the table.
        /// </summary>
        /// <remarks>
        /// Return the current root-level column count. The base class handles valid parent indexes
        /// for you and reports zero child columns automatically.
        /// </remarks>
        protected abstract int Columns { get; }

        /// <summary>
        /// Gets or sets the value stored at the specified row and column.
        /// </summary>
        /// <remarks>
        /// This is the primary hook for reading and writing cell data. For simple value types, the
        /// base class uses this indexer to serve both display and edit requests. For custom item
        /// types, the returned object is also used to resolve property-based roles.
        /// </remarks>
        protected abstract T this[int row, int col] { get; set; }

        /// <summary>
        /// Clears the value at the specified cell.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the cell value was cleared; otherwise, <see
        /// langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Override this when your model supports a "clear cell" operation that is distinct from
        /// setting a normal value. The default implementation returns <see langword="false"/>. Call
        /// one of the <see cref="DataChanged(int, int)"/> overloads after a successful clear so
        /// connected views refresh.
        /// </remarks>
        protected virtual bool ClearItemData(int row, int col) => false;

        /// <summary>
        /// Returns the label for a row header.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <c>R1</c>, <c>R2</c>, and so on. Override this to
        /// provide application-specific row labels.
        /// </remarks>
        protected virtual string RowHeader(int row) => $"R{row + 1}";

        /// <summary>
        /// Returns the label for a column header.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <c>C1</c>, <c>C2</c>, and so on. Override this to
        /// provide domain-specific column names such as field names or spreadsheet letters.
        /// </remarks>
        protected virtual string ColumnHeader(int column) => $"C{column + 1}";

        /// <summary>
        /// Returns whether rows can be inserted at the specified position.
        /// </summary>
        /// <remarks>
        /// Use this to validate row insertion requests before the model begins its structural
        /// change notification sequence. The default implementation returns <see
        /// langword="false"/>.
        /// </remarks>
        protected virtual bool CanInsertRows(int row, int count) => false;

        /// <summary>
        /// Inserts rows at the specified position.
        /// </summary>
        /// <remarks>
        /// This method is called only after <see cref="CanInsertRows"/> succeeds. The base class
        /// performs the required begin/end notification calls around this method, so your
        /// implementation should perform the already-validated backing-store update rather than
        /// re-checking whether the operation is allowed.
        /// </remarks>
        protected virtual bool InsertRows(int row, int count) => false;

        /// <summary>
        /// Returns whether rows can be removed from the specified position.
        /// </summary>
        /// <remarks>
        /// Use this to validate row removal requests before the model begins its structural change
        /// notification sequence. The default implementation returns <see langword="false"/>.
        /// </remarks>
        protected virtual bool CanRemoveRows(int row, int count) => false;

        /// <summary>
        /// Removes rows from the specified position.
        /// </summary>
        /// <remarks>
        /// This method is called only after <see cref="CanRemoveRows"/> succeeds. The base class
        /// performs the required begin/end notification calls around this method, so your
        /// implementation should perform the already-validated backing-store update rather than
        /// re-checking whether the operation is allowed.
        /// </remarks>
        protected virtual bool RemoveRows(int row, int count) => false;

        /// <summary>
        /// Returns whether columns can be inserted at the specified position.
        /// </summary>
        /// <remarks>
        /// Use this to validate column insertion requests before the model begins its structural
        /// change notification sequence. The default implementation returns <see
        /// langword="false"/>.
        /// </remarks>
        protected virtual bool CanInsertColumns(int column, int count) => false;

        /// <summary>
        /// Inserts columns at the specified position.
        /// </summary>
        /// <remarks>
        /// This method is called only after <see cref="CanInsertColumns"/> succeeds. The base class
        /// performs the required begin/end notification calls around this method, so your
        /// implementation should perform the already-validated backing-store update rather than
        /// re-checking whether the operation is allowed.
        /// </remarks>
        protected virtual bool InsertColumns(int column, int count) => false;

        /// <summary>
        /// Returns whether columns can be removed from the specified position.
        /// </summary>
        /// <remarks>
        /// Use this to validate column removal requests before the model begins its structural
        /// change notification sequence. The default implementation returns <see
        /// langword="false"/>.
        /// </remarks>
        protected virtual bool CanRemoveColumns(int column, int count) => false;

        /// <summary>
        /// Removes columns from the specified position.
        /// </summary>
        /// <remarks>
        /// This method is called only after <see cref="CanRemoveColumns"/> succeeds. The base class
        /// performs the required begin/end notification calls around this method, so your
        /// implementation should perform the already-validated backing-store update rather than
        /// re-checking whether the operation is allowed.
        /// </remarks>
        protected virtual bool RemoveColumns(int column, int count) => false;

        /// <summary>
        /// Gets whether the model should reject all edit operations.
        /// </summary>
        /// <remarks>
        /// When this property returns <see langword="true"/>, the model does not expose the
        /// built-in edit role and <see cref="SetData(ModelIndex, object, int)"/> always returns
        /// <see langword="false"/>.
        /// </remarks>
        protected virtual bool IsReadOnly => false;

        /// <summary>
        /// Gets whether custom item types should expose the whole cell object through the
        /// <c>item</c> role.
        /// </summary>
        /// <remarks>
        /// This applies only when <typeparamref name="T"/> is not a simple convertible value.
        /// Returning <see langword="false"/> hides the automatically added <c>item</c> role while
        /// still exposing property-based roles.
        /// </remarks>
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

        /// <summary>
        /// Returns the number of rows for the specified parent index.
        /// </summary>
        /// <remarks>
        /// For the root level, this returns <see cref="Rows"/>. For valid parent indexes, it
        /// returns <c>0</c> because <see cref="TableModel{T}"/> is always a flat table rather
        /// than a hierarchical model.
        /// </remarks>
        public sealed override int RowCount(ModelIndex idx) => idx is { IsValid: true } ? 0 : Rows;

        /// <summary>
        /// Returns the number of columns for the specified parent index.
        /// </summary>
        /// <remarks>
        /// For the root level, this returns <see cref="Columns"/>. For valid parent indexes, it
        /// returns <c>0</c> because <see cref="TableModel{T}"/> does not expose child items.
        /// </remarks>
        public sealed override int ColumnCount(ModelIndex idx) => idx is { IsValid: true } ? 0 : Columns;

        /// <summary>
        /// Returns the capability flags for the specified cell.
        /// </summary>
        /// <remarks>
        /// The base implementation derives enabled/selectable state from <see cref="IModelItem"/>
        /// when available and derives editability from <see cref="IsReadOnly"/>,
        /// <typeparamref name="T"/>, and <see cref="IEditable"/>.
        /// </remarks>
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

        /// <summary>
        /// Returns the role names inferred from <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// Depending on the item type, this can include the built-in <c>display</c> and
        /// <c>edit</c> roles, the <c>item</c> role for the whole cell object, and property-based
        /// roles generated from public instance properties.
        /// </remarks>
        public sealed override Dictionary<int, string> RoleNames() => RoleNamesById;

        /// <summary>
        /// Returns the value for the specified cell and role.
        /// </summary>
        /// <remarks>
        /// For simple value types, the built-in display and edit roles return the cell value
        /// directly. For custom item types, the base class uses <see cref="IDisplayable"/>,
        /// <see cref="IEditable"/>, the optional <c>item</c> role, and public instance properties
        /// to resolve role values.
        /// </remarks>
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

        /// <summary>
        /// Updates the value for the specified cell and role.
        /// </summary>
        /// <remarks>
        /// For editable simple value types, this updates the cell through the row/column indexer.
        /// For custom item types, it updates either <see cref="IEditable.EditValue"/>, the full
        /// item via the <c>item</c> role, or a writable public property. Successful updates
        /// automatically notify connected views.
        /// </remarks>
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

        /// <summary>
        /// Returns the header value for the specified section.
        /// </summary>
        /// <remarks>
        /// The base implementation exposes only display values for horizontal and vertical
        /// headers, using <see cref="ColumnHeader"/> and <see cref="RowHeader"/>.
        /// </remarks>
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

        /// <summary>
        /// Inserts rows at the specified root-level position.
        /// </summary>
        /// <remarks>
        /// The base class rejects valid parent indexes, validates the request with
        /// <see cref="CanInsertRows"/>, and performs the required structural change notifications
        /// around <see cref="InsertRows(int, int)"/>.
        /// </remarks>
        public sealed override bool InsertRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanInsertRows(row, count))
                return false;
            BeginInsertRows(parent, row, row + count - 1);
            bool ok;
            try {
                ok = InsertRows(row, count);
            } finally {
                EndInsertRows();
            }
            return ok;
        }

        /// <summary>
        /// Removes rows from the specified root-level position.
        /// </summary>
        /// <remarks>
        /// The base class rejects valid parent indexes, validates the request with
        /// <see cref="CanRemoveRows"/>, and performs the required structural change notifications
        /// around <see cref="RemoveRows(int, int)"/>.
        /// </remarks>
        public sealed override bool RemoveRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanRemoveRows(row, count))
                return false;
            BeginRemoveRows(parent, row, row + count - 1);
            bool ok;
            try {
                ok = RemoveRows(row, count);
            } finally {
                EndRemoveRows();
            }
            return ok;
        }

        /// <summary>
        /// Inserts columns at the specified root-level position.
        /// </summary>
        /// <remarks>
        /// The base class rejects valid parent indexes, validates the request with
        /// <see cref="CanInsertColumns"/>, and performs the required structural change
        /// notifications around <see cref="InsertColumns(int, int)"/>.
        /// </remarks>
        public sealed override bool InsertColumns(int column, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanInsertColumns(column, count))
                return false;
            BeginInsertColumns(parent, column, column + count - 1);
            bool ok;
            try {
                ok = InsertColumns(column, count);
            } finally {
                EndInsertColumns();
            }
            return ok;
        }

        /// <summary>
        /// Removes columns from the specified root-level position.
        /// </summary>
        /// <remarks>
        /// The base class rejects valid parent indexes, validates the request with
        /// <see cref="CanRemoveColumns"/>, and performs the required structural change
        /// notifications around <see cref="RemoveColumns(int, int)"/>.
        /// </remarks>
        public sealed override bool RemoveColumns(int column, int count, ModelIndex parent = null)
        {
            if (parent is { IsValid: true })
                return false;
            if (!CanRemoveColumns(column, count))
                return false;
            BeginRemoveColumns(parent, column, column + count - 1);
            bool ok;
            try {
                ok = RemoveColumns(column, count);
            } finally {
                EndRemoveColumns();
            }
            return ok;
        }

        /// <summary>
        /// Clears the value for the specified cell.
        /// </summary>
        /// <remarks>
        /// This forwards a valid model index to <see cref="ClearItemData(int, int)"/> and rejects
        /// invalid indexes.
        /// </remarks>
        public sealed override bool ClearItemData(ModelIndex index)
        {
            if (index is not { IsValid: true })
                return false;
            return ClearItemData(index.Row, index.Column);
        }

        /// <summary>
        /// Notifies that a single cell value has changed.
        /// </summary>
        /// <remarks>
        /// Call this after updating the backing store outside of <see cref="SetData"/> so views
        /// and delegates refresh the cell.
        /// </remarks>
        protected void DataChanged(int row, int column)
        {
            DataChanged(row, column, row, column);
        }

        /// <summary>
        /// Notifies that a rectangular cell range has changed.
        /// </summary>
        /// <remarks>
        /// Use this overload when one operation updates multiple cells, such as recalculating a
        /// block of formulas or replacing a rectangular selection.
        /// </remarks>
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
