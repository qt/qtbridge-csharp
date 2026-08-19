// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Qt.DotNet;
using Qt.Quick;

namespace Qt.Bridge.Models
{
    /// <summary>
    /// Represents a hierarchical model with parent-child relationships.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Derive from <see cref="TreeModel"/> when a tree requires custom index identity, multiple
    /// columns, or custom role lookup. For the usual single-column, node-based case, prefer
    /// <see cref="TreeModel{T}"/>. Use <see cref="NodeTreeModel{T}"/> to adapt an existing
    /// application tree whose nodes cannot inherit <see cref="TreeNode{T}"/>.
    /// </para>
    /// <note type="important">
    /// A non-generic tree model cannot safely provide implementations of <see cref="Model.Index"/>,
    /// <see cref="Model.Parent"/>, or <see cref="Model.Sibling"/>. Those methods must convert a
    /// <see cref="ModelIndex.Id"/> back to the application's node and find that node's current
    /// parent and row. The required identity and navigation rules belong to the application's tree,
    /// so subclasses must supply them.
    /// </note>
    /// <para>
    /// The following outline shows the essential members of a two-column tree. The
    /// application supplies <c>FindNode</c>, which resolves the stable id stored in
    /// each <see cref="ModelIndex"/> back to its node.
    /// </para>
    /// <![CDATA[
    /// ```csharp
    /// public sealed class FileTree : TreeModel
    /// {
    ///     private readonly List<FileNode> roots = [];
    ///
    ///     public override int RowCount(ModelIndex parent) => GetChildren(parent).Count;
    ///     public override int ColumnCount(ModelIndex parent) => 2;
    ///
    ///     public override ModelIndex Index(int row, int column, ModelIndex parent)
    ///     {
    ///         var children = GetChildren(parent);
    ///         if (row < 0 || row >= children.Count || column is < 0 or >= 2)
    ///             return ModelIndex.Empty;
    ///
    ///         var node = children[row];
    ///         return new ModelIndex(row, column, node.Id);
    ///     }
    ///
    ///     public override ModelIndex Parent(ModelIndex index)
    ///     {
    ///         var parent = index is { IsValid: true } ? FindNode(index.Id)?.Parent : null;
    ///         return parent is null
    ///             ? ModelIndex.Empty
    ///             : new ModelIndex(RowOf(parent), 0, parent.Id);
    ///     }
    ///
    ///     public override object Data(ModelIndex index, int role)
    ///     {
    ///         if (role != Roles.DisplayRole || index is not { IsValid: true }
    ///             || FindNode(index.Id) is not { } node)
    ///             return null;
    ///         return index.Column == 0 ? node.Name : node.Size;
    ///     }
    ///
    ///     private IReadOnlyList<FileNode> GetChildren(ModelIndex parent) => parent switch
    ///     {
    ///         { IsValid: false } => roots,
    ///         { Id: var id } when FindNode(id) is { } node => node.Children,
    ///         _ => []
    ///     };
    /// }
    /// ```
    /// ]]>
    /// </remarks>
    public abstract class TreeModel : Model;

    /// <summary>
    /// Provides a strongly typed base class for adapting an existing single-column tree.
    /// </summary>
    /// <typeparam name="T">The reference type used for tree nodes.</typeparam>
    /// <remarks>
    /// <para>
    /// Derive from <see cref="NodeTreeModel{T}"/> when each item in a hierarchy is represented
    /// by a node object. Implement the root and child lookup members to describe the tree, and
    /// implement <see cref="ParentOf(T)"/> and <see cref="IndexOf(T)"/> so the model can
    /// navigate from an item back to its parent.
    /// </para>
    /// <para>
    /// The base class assigns each node a stable internal model id using reference identity. A
    /// node must therefore occur at most once in the tree and must remain the same object while
    /// it is exposed by the model. Report structural changes with the protected begin/end helpers
    /// before and after changing the backing tree, so connected views can update correctly.
    /// </para>
    /// <para>
    /// <typeparamref name="T"/> is constrained to a reference type because a tree index must
    /// identify one stable node object, and <see langword="null"/> represents the absence of a
    /// parent for root nodes. Value types do not provide either convention reliably when copied or
    /// boxed.
    /// </para>
    /// <para>
    /// Single-column means that each node provides one displayed item or value; it does not limit
    /// the depth of the hierarchy. Use a multi-column tree when each node needs sibling values,
    /// such as a file name, size, and modified date.
    /// </para>
    /// <para>
    /// Public instance properties on <typeparamref name="T"/> are exposed automatically as
    /// named values, called roles, that are available to QML. <see cref="IDisplayable"/>,
    /// <see cref="IEditable"/>, and
    /// <see cref="IModelItem"/> provide the same display, editing, and flag conventions as
    /// <see cref="ListModel{T}"/> and <see cref="TableModel{T}"/>.
    /// </para>
    /// <note type="important">
    /// This model exposes exactly one column for each node. For a hierarchical model with multiple
    /// columns, derive from the non-generic <see cref="TreeModel"/> and implement its index,
    /// parent, column, and value lookup behavior for the application's data structure.
    /// </note>
    /// <![CDATA[
    /// ```csharp
    /// public sealed class FolderNode(string name)
    /// {
    ///     public string Name { get; } = name;
    ///     public FolderNode Parent { get; private set; }
    ///     public List<FolderNode> Children { get; } = [];
    ///
    ///     public FolderNode Add(FolderNode child)
    ///     {
    ///         child.Parent = this;
    ///         Children.Add(child);
    ///         return this;
    ///     }
    /// }
    ///
    /// public sealed class FolderTree : NodeTreeModel<FolderNode>
    /// {
    ///     private readonly List<FolderNode> roots =
    ///     [
    ///         new FolderNode("Projects")
    ///             .Add(new FolderNode("Qt"))
    ///             .Add(new FolderNode("CSharp"))
    ///     ];
    ///
    ///     protected override int RootCount => roots.Count;
    ///     protected override FolderNode RootAt(int row) => roots[row];
    ///     protected override int ChildCount(FolderNode parent) => parent.Children.Count;
    ///     protected override FolderNode ChildAt(FolderNode parent, int row) =>
    ///         parent.Children[row];
    ///     protected override FolderNode ParentOf(FolderNode node) => node.Parent;
    ///     protected override int IndexOf(FolderNode node) =>
    ///         node.Parent is null ? roots.IndexOf(node) : node.Parent.Children.IndexOf(node);
    /// }
    /// ```
    /// ]]>
    /// </remarks>
    public abstract class NodeTreeModel<T> : TreeModel where T : class
    {
        private const int ItemRole = Roles.UserRole;

        private readonly ConditionalWeakTable<T, NodeIdentity> nodeIds = [];
        private readonly Dictionary<nint, WeakReference<T>> nodesById = [];
        private nint nextNodeId = 1;
        private Dictionary<int, PropertyInfo> roleMap;
        private Dictionary<int, string> roleNames;

        /// <summary> Gets the number of root-level nodes. </summary>
        protected abstract int RootCount { get; }

        /// <summary> Returns the root-level node at <paramref name="row"/>. </summary>
        protected abstract T RootAt(int row);

        /// <summary> Returns the number of children of <paramref name="parent"/>. </summary>
        protected abstract int ChildCount(T parent);

        /// <summary>
        /// Returns the child of <paramref name="parent"/> at <paramref name="row"/>.
        /// </summary>
        protected abstract T ChildAt(T parent, int row);

        /// <summary>
        /// Returns the parent of <paramref name="node"/>, or <see langword="null"/> for a root
        /// node.
        /// </summary>
        protected abstract T ParentOf(T node);

        /// <summary>
        /// Returns the current row of <paramref name="node"/> under its parent.
        /// </summary>
        protected abstract int IndexOf(T node);

        /// <summary> Gets whether the model should reject all edit operations. </summary>
        protected virtual bool IsReadOnly => false;

        /// <summary>
        /// Gets whether the complete node is exposed through the <c>item</c> role.
        /// </summary>
        protected virtual bool HasItemRole => true;

        /// <summary>
        /// Replaces the node at the specified row under <paramref name="parent"/>.
        /// </summary>
        protected virtual bool SetNode(T parent, int row, T value) => false;

        /// <summary> Clears the value associated with <paramref name="node"/>. </summary>
        protected virtual bool ClearNodeData(T node) => false;

        private static bool NodeTypeIsConvertible => ValueConverter.IsConvertible(typeof(T));
        private static bool NodeTypeIsModelItem => typeof(T).IsAssignableTo(typeof(IModelItem));
        private static bool NodeTypeIsDisplayable =>
            NodeTypeIsConvertible || typeof(T).IsAssignableTo(typeof(IDisplayable));
        private static bool NodeTypeIsEditable =>
            NodeTypeIsConvertible || typeof(T).IsAssignableTo(typeof(IEditable));

        private Dictionary<int, PropertyInfo> RoleMap
        {
            get
            {
                if (roleMap != null)
                    return roleMap;

                roleMap = [];
                if (NodeTypeIsConvertible)
                    return roleMap;

                var properties = RoleProperties.ToArray();
                for (var i = 0; i < properties.Length; ++i)
                    roleMap[ItemRole + i + 1] = properties[i];
                return roleMap;
            }
        }

        /// <summary> Gets the public node properties exposed as automatic QML roles. </summary>
        protected virtual IEnumerable<PropertyInfo> RoleProperties
            => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        /// <inheritdoc/>
        public sealed override int RowCount(ModelIndex parent)
        {
            if (parent is not { IsValid: true })
                return RootCount;
            return TryGetNode(parent, out var node) ? ChildCount(node) : 0;
        }

        /// <inheritdoc/>
        public sealed override int ColumnCount(ModelIndex parent) => 1;

        /// <inheritdoc/>
        public sealed override bool HasChildren(ModelIndex parent) => RowCount(parent) > 0;

        /// <inheritdoc/>
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
        {
            if (column != 0 || row < 0)
                return ModelIndex.Empty;

            T node;
            if (parent is { IsValid: true }) {
                if (!TryGetNode(parent, out var parentNode) || row >= ChildCount(parentNode))
                    return ModelIndex.Empty;
                node = ChildAt(parentNode, row);
            } else {
                if (row >= RootCount)
                    return ModelIndex.Empty;
                node = RootAt(row);
            }

            return node == null ? ModelIndex.Empty : new ModelIndex(row, column, GetNodeId(node));
        }

        /// <inheritdoc/>
        public sealed override ModelIndex Parent(ModelIndex index)
        {
            if (!TryGetNode(index, out var node) || ParentOf(node) is not { } parent)
                return ModelIndex.Empty;

            var row = IndexOf(parent);
            return row < 0 ? ModelIndex.Empty : new ModelIndex(row, 0, GetNodeId(parent));
        }

        /// <inheritdoc/>
        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index) =>
            index is { IsValid: true } ? Index(row, column, Parent(index)) : ModelIndex.Empty;

        /// <inheritdoc/>
        public sealed override Dictionary<int, string> RoleNames()
        {
            if (roleNames != null)
                return roleNames.Count != 0 ? roleNames : null;

            roleNames = [];
            if (NodeTypeIsDisplayable)
                roleNames[Roles.DisplayRole] = "display";
            if (NodeTypeIsEditable && !IsReadOnly)
                roleNames[Roles.EditRole] = "edit";
            if (HasItemRole)
                roleNames[ItemRole] = "item";
            foreach (var entry in RoleMap)
                roleNames[entry.Key] = entry.Value.Name.ToQmlPropertyName();
            return roleNames.Count != 0 ? roleNames : null;
        }

        /// <inheritdoc/>
        public sealed override object Data(ModelIndex index, int role)
        {
            if (!TryGetNode(index, out var node))
                return null;

            return role switch
            {
                Roles.DisplayRole when NodeTypeIsConvertible => node,
                Roles.DisplayRole when node is IDisplayable displayable =>
                    displayable.DisplayValue,
                Roles.EditRole when NodeTypeIsConvertible => node,
                Roles.EditRole when node is IEditable { IsEditable: true } editable =>
                    editable.EditValue,
                ItemRole when HasItemRole => node,
                _ when RoleMap.TryGetValue(role, out var property) && property.CanRead =>
                    property.GetValue(node),
                _ => null
            };
        }

        /// <inheritdoc/>
        public sealed override int Flags(ModelIndex index)
        {
            if (!TryGetNode(index, out var node))
                return ItemFlags.NoItemFlags;

            var flags = NodeTypeIsModelItem
                ? ItemFlags.NoItemFlags
                : base.Flags(index);
            if (node is IModelItem { IsEnabled: true })
                flags |= ItemFlags.ItemIsEnabled;
            if (node is IModelItem { IsSelectable: true })
                flags |= ItemFlags.ItemIsSelectable;
            if (!IsReadOnly && (NodeTypeIsConvertible || node is IEditable { IsEditable: true }))
                flags |= ItemFlags.ItemIsEditable;
            return flags;
        }

        /// <inheritdoc/>
        public sealed override bool SetData(ModelIndex index, object value, int role)
        {
            if (IsReadOnly || !TryGetNode(index, out var node))
                return false;

            switch (role) {
            case Roles.EditRole when NodeTypeIsConvertible: {
                var parent = ParentOf(node);
                var row = IndexOf(node);
                if (row < 0 || !SetNode(parent, row, ValueConverter.ToValue<T>(value)))
                    return false;
                RebindNodeId(index.Id, node, NodeAt(parent, row));
                DataChanged(index, index);
                return true;
                }
            case Roles.EditRole when node is IEditable { IsEditable: true } editable:
                editable.EditValue = value;
                DataChanged(index, index);
                return true;
            case Roles.EditRole:
                return false;
            case ItemRole when HasItemRole && value is T replacement: {
                var parent = ParentOf(node);
                var row = IndexOf(node);
                if (row < 0 || !SetNode(parent, row, replacement))
                    return false;
                RebindNodeId(index.Id, node, NodeAt(parent, row));
                DataChanged(index, index);
                return true;
                }
            }

            if (!RoleMap.TryGetValue(role, out var property) || !property.CanWrite)
                return false;

            try {
                property.SetValue(node, value);
                DataChanged(index, index);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        /// <inheritdoc/>
        public sealed override bool ClearItemData(ModelIndex index) =>
            TryGetNode(index, out var node) && ClearNodeData(node);

        /// <summary> Notifies that the data for <paramref name="node"/> changed. </summary>
        protected void DataChanged(T node)
        {
            if (TryCreateIndex(node, out var index))
                DataChanged(index, index);
        }

        private bool TryGetNode(ModelIndex index, out T node)
        {
            node = null;
            if (index is not { IsValid: true, Column: 0 }
                || !nodesById.TryGetValue(index.Id, out var nodeReference)) {
                return false;
            }
            if (nodeReference.TryGetTarget(out node))
                return true;

            nodesById.Remove(index.Id);
            return false;
        }

        private nint GetNodeId(T node)
        {
            if (nodeIds.TryGetValue(node, out var identity))
                return identity!.Id;

            var id = nextNodeId++;
            nodeIds.Add(node, new(id));
            nodesById.Add(id, new(node));
            if ((id & 0xff) == 0)
                PruneCollectedNodes();
            return id;
        }

        private T NodeAt(T parent, int row) =>
            parent is null ? RootAt(row) : ChildAt(parent, row);

        private void RebindNodeId(nint id, T oldNode, T replacement)
        {
            if (replacement is null || ReferenceEquals(oldNode, replacement))
                return;

            nodeIds.Remove(oldNode);
            if (nodeIds.TryGetValue(replacement, out var replacementIdentity))
                nodesById.Remove(replacementIdentity!.Id);
            nodeIds.Remove(replacement);
            nodeIds.Add(replacement, new(id));
            nodesById[id] = new(replacement);
        }

        private void PruneCollectedNodes()
        {
            foreach (var (id, nodeReference) in nodesById.ToArray()) {
                if (!nodeReference.TryGetTarget(out _))
                    nodesById.Remove(id);
            }
        }

        /// <summary> Creates an index for an existing node. </summary>
        protected bool TryCreateIndex(T node, out ModelIndex index)
        {
            index = ModelIndex.Empty;
            if (node == null)
                return false;
            var row = IndexOf(node);
            if (row < 0)
                return false;
            index = new ModelIndex(row, 0, GetNodeId(node));
            return true;
        }

        private sealed class NodeIdentity(nint id)
        {
            public nint Id { get; } = id;
        }
    }

    /// <summary>
    /// Provides the hierarchy state shared by nodes used with <see cref="TreeModel{T}"/>.
    /// </summary>
    /// <typeparam name="T">The concrete node type.</typeparam>
    /// <remarks>
    /// Derive application node types from this class to avoid repeating parent and child storage.
    /// Use <see cref="Add(T)"/> only while constructing a detached tree. Once a node is in a
    /// <see cref="TreeModel{T}"/>, use that model's mutation methods so connected views receive
    /// the required structural change notifications.
    /// </remarks>
    public abstract class TreeNode<T> where T : TreeNode<T>
    {
        private readonly List<T> children = [];

        /// <summary> Gets the parent node, or <see langword="null"/> for a root node. </summary>
        public T Parent { get; private set; }

        /// <summary> Gets the child nodes. </summary>
        public IReadOnlyList<T> Children => children;

        /// <summary>
        /// Appends <paramref name="child"/> while constructing a detached tree.
        /// </summary>
        /// <returns> This node, so calls can be chained. </returns>
        public T Add(T child)
        {
            InsertChild(children.Count, child);
            return (T)this;
        }

        internal void InsertChild(int row, T child)
        {
            ArgumentNullException.ThrowIfNull(child);
            if (child.Parent != null)
                throw new ArgumentException("The child already has a parent.", nameof(child));
            children.Insert(row, child);
            child.Parent = (T)this;
        }

        internal void RemoveChild(int row)
        {
            var child = children[row];
            children.RemoveAt(row);
            child.Parent = null;
        }

        internal int IndexOfChild(T child) => children.IndexOf(child);
    }

    /// <summary>
    /// Provides a single-column tree model backed by <see cref="TreeNode{T}"/> nodes.
    /// </summary>
    /// <typeparam name="T">The concrete node type.</typeparam>
    /// <remarks>
    /// <para>
    /// Create this model directly when application nodes can inherit <see cref="TreeNode{T}"/>.
    /// The parameterless constructor creates an empty root collection. Alternatively, pass an
    /// <see cref="IList{T}"/> to the constructor to use an existing root collection. Navigation
    /// and identity handling are supplied by the base class.
    /// </para>
    /// <para>
    /// After the model is used by a view, change its structure only through
    /// <see cref="AddRoot(T)"/>, <see cref="InsertRoot(int, T)"/>, <see cref="AddChild(T, T)"/>,
    /// <see cref="InsertChild(T, int, T)"/>, and <see cref="Remove(T)"/>. These methods notify
    /// connected views about the change.
    /// </para>
    /// <para>
    /// Public properties declared by <typeparamref name="T"/> become named values available to
    /// QML automatically. The inherited <see cref="TreeNode{T}.Parent"/> and
    /// <see cref="TreeNode{T}.Children"/> properties describe navigation state, so they are not
    /// exposed as QML values.
    /// </para>
    /// <para>
    /// Single-column means that each node provides one displayed item or value; it does not limit
    /// the depth of the hierarchy. Use the non-generic <see cref="TreeModel"/> when each node
    /// needs sibling values, such as a file name, size, and modified date.
    /// </para>
    /// <note type="important">
    /// This convenience model exposes exactly one column for each node. Use the non-generic
    /// <see cref="TreeModel"/> when each node needs multiple columns.
    /// </note>
    /// <![CDATA[
    /// ```csharp
    /// public sealed class FolderNode(string name) : TreeNode<FolderNode>
    /// {
    ///     public string Name { get; } = name;
    /// }
    ///
    /// var folders = new TreeModel<FolderNode>();
    /// var projects = folders.AddRoot(new FolderNode("Projects"));
    /// folders.AddChild(projects, new FolderNode("Qt"));
    /// ```
    /// ]]>
    /// </remarks>
    public class TreeModel<T>(IList<T> rootNodes) : NodeTreeModel<T> where T : TreeNode<T>
    {
        private readonly IList<T> roots = rootNodes
            ?? throw new ArgumentNullException(nameof(rootNodes));

        /// <summary> Initializes a model with an empty root collection. </summary>
        public TreeModel() : this([])
        {}

        /// <summary> Gets a live, read-only view of the current root nodes. </summary>
        public IReadOnlyList<T> RootNodes { get; } = new ReadOnlyCollection<T>(rootNodes);

        /// <inheritdoc/>
        protected sealed override int RootCount => roots.Count;

        /// <inheritdoc/>
        protected sealed override T RootAt(int row) => roots[row];

        /// <inheritdoc/>
        protected sealed override int ChildCount(T parent) => parent.Children.Count;

        /// <inheritdoc/>
        protected sealed override T ChildAt(T parent, int row) => parent.Children[row];

        /// <inheritdoc/>
        protected sealed override T ParentOf(T node) => node.Parent;

        /// <inheritdoc/>
        protected sealed override int IndexOf(T node) =>
            node.Parent?.IndexOfChild(node) ?? roots.IndexOf(node);

        /// <inheritdoc/>
        protected override IEnumerable<PropertyInfo> RoleProperties
            => base.RoleProperties.Where(property => property.DeclaringType != typeof(TreeNode<T>));

        /// <summary> Appends a root node and notifies connected views. </summary>
        public T AddRoot(T node) => InsertRoot(roots.Count, node);

        /// <summary>
        /// Inserts a root node at the specified row and notifies connected views.
        /// </summary>
        public T InsertRoot(int row, T node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (row < 0 || row > roots.Count)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (node.Parent != null || roots.Contains(node))
                throw new ArgumentException("The node is already part of a tree.", nameof(node));

            BeginInsertRows(ModelIndex.Empty, row, row);
            try {
                roots.Insert(row, node);
            } finally {
                EndInsertRows();
            }
            return node;
        }

        /// <summary> Appends a child node and notifies connected views. </summary>
        public T AddChild(T parent, T child) => InsertChild(parent, parent.Children.Count, child);

        /// <summary>
        /// Inserts a child node at the specified row and notifies connected views.
        /// </summary>
        public T InsertChild(T parent, int row, T child)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(child);
            if (row < 0 || row > parent.Children.Count)
                throw new ArgumentOutOfRangeException(nameof(row));
            if (child.Parent != null || roots.Contains(child))
                throw new ArgumentException("The child is already part of a tree.", nameof(child));
            if (!ContainsNode(parent) || !TryCreateIndex(parent, out var parentIndex))
                throw new ArgumentException("The parent is not part of this model.", nameof(parent));

            BeginInsertRows(parentIndex, row, row);
            try {
                parent.InsertChild(row, child);
            } finally {
                EndInsertRows();
            }
            return child;
        }

        /// <summary> Removes a node and its descendants from the model. </summary>
        public bool Remove(T node)
        {
            ArgumentNullException.ThrowIfNull(node);
            if (!ContainsNode(node))
                return false;
            var parent = node.Parent;
            var row = IndexOf(node);
            if (row < 0)
                return false;

            var parentIndex = ModelIndex.Empty;
            if (parent != null && !TryCreateIndex(parent, out parentIndex))
                return false;

            BeginRemoveRows(parentIndex, row, row);
            try {
                if (parent is null)
                    roots.RemoveAt(row);
                else
                    parent.RemoveChild(row);
            } finally {
                EndRemoveRows();
            }
            return true;
        }

        private bool ContainsNode(T candidate)
        {
            var pending = new Stack<T>(roots);
            while (pending.TryPop(out var node)) {
                if (ReferenceEquals(node, candidate))
                    return true;
                foreach (var child in node.Children)
                    pending.Push(child);
            }
            return false;
        }
    }
}
