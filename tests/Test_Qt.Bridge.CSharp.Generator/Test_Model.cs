// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.Bridge.Models;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_Model
    {
        public TestContext TestContext { get; set; }

        private static readonly Assembly ApiAssembly = typeof(Model).Assembly;
        private static readonly Assembly AdapterAssembly = typeof(ModelIndex).Assembly;

        private sealed class NameListModel(params string[] items)
            : ListModel<string>
        {
            private readonly List<string> items = items.ToList();

            public override int ItemCount() => items.Count;
            public override string Data(int index) => items[index];
        }

        private sealed class EditableNameListModel(params string[] items)
            : ListModel<string>
        {
            private readonly List<string> items = items.ToList();

            public override int ItemCount() => items.Count;
            public override string Data(int index) => items[index];

            protected override bool SetData(int index, string value)
            {
                if (index < 0 || index >= items.Count)
                    return false;
                items[index] = value;
                return true;
            }

            public Dictionary<int, string> GetRoleNames() => RoleNames();
            public object GetData(ModelIndex index, int role) => Data(index, role);
            public bool SetEdit(ModelIndex index, object value) => SetData(index, value, Roles.EditRole);
        }

        private sealed class PersonItem : IModelItem, IDisplayable, IEditable
        {
            public string Name { get; set; }
            public bool IsEnabled { get; init; }
            public bool IsSelectable { get; init; }
            public bool IsEditable { get; init; }
            public object DisplayValue => $"Display:{Name}";
            public object EditValue
            {
                get => Name;
                set => Name = value?.ToString();
            }
        }

        private sealed class PersonListModel(params PersonItem[] items)
            : ListModel<PersonItem>
        {
            private readonly List<PersonItem> items = items.ToList();

            public override int ItemCount() => items.Count;
            public override PersonItem Data(int index) => items[index];

            protected override bool SetData(int index, PersonItem value)
            {
                if (index < 0 || index >= items.Count)
                    return false;
                items[index] = value;
                return true;
            }

            public Dictionary<int, string> GetRoleNames() => RoleNames();
            public object GetData(ModelIndex index, int role) => Data(index, role);
            public bool SetByRole(ModelIndex index, object value, int role) => SetData(index, value, role);
            public int GetFlags(ModelIndex index) => Flags(index);
        }

        private sealed class NumberTableModel(int rows, int columns)
            : TableModel<int>
        {
            private readonly int[,] items = new int[rows, columns];

            protected override int Rows => items.GetLength(0);
            protected override int Columns => items.GetLength(1);

            protected override int this[int row, int col]
            {
                get => items[row, col];
                set => items[row, col] = value;
            }
        }

        private sealed class TreeNode(string name)
        {
            public string Name { get; set; } = name;
            public TreeNode Parent { get; private set; }
            public List<TreeNode> Children { get; } = [];

            public TreeNode Add(TreeNode child)
            {
                child.Parent = this;
                Children.Add(child);
                return this;
            }
        }

        private sealed class NameTreeModel(params TreeNode[] roots)
            : NodeTreeModel<TreeNode>
        {
            private readonly List<TreeNode> roots = [.. roots];

            protected override int RootCount => roots.Count;
            protected override TreeNode RootAt(int row) => roots[row];
            protected override int ChildCount(TreeNode parent) => parent.Children.Count;
            protected override TreeNode ChildAt(TreeNode parent, int row) => parent.Children[row];
            protected override TreeNode ParentOf(TreeNode node) => node.Parent;
            protected override int IndexOf(TreeNode node) =>
                node.Parent is null ? roots.IndexOf(node) : node.Parent.Children.IndexOf(node);

            public Dictionary<int, string> GetRoleNames() => RoleNames();
            public object GetData(ModelIndex index, int role) => Data(index, role);
            public bool SetByRole(ModelIndex index, object value, int role) =>
                SetData(index, value, role);
        }

        private sealed class ReplaceableTreeModel(params TreeNode[] roots)
            : NodeTreeModel<TreeNode>
        {
            private readonly List<TreeNode> roots = [.. roots];

            protected override int RootCount => roots.Count;
            protected override TreeNode RootAt(int row) => roots[row];
            protected override int ChildCount(TreeNode parent) => parent.Children.Count;
            protected override TreeNode ChildAt(TreeNode parent, int row) => parent.Children[row];
            protected override TreeNode ParentOf(TreeNode node) => node.Parent;
            protected override int IndexOf(TreeNode node) =>
                node.Parent is null ? roots.IndexOf(node) : node.Parent.Children.IndexOf(node);

            protected override bool SetNode(TreeNode parent, int row, TreeNode value)
            {
                if (parent is not null || row < 0 || row >= roots.Count)
                    return false;
                roots[row] = value;
                return true;
            }

            public object GetData(ModelIndex index, int role) => Data(index, role);
            public bool SetByRole(ModelIndex index, object value, int role) =>
                SetData(index, value, role);
        }

        private sealed class FolderNode(string name)
            : Qt.Bridge.Models.TreeNode<FolderNode>
        {
            public string Name { get; set; } = name;
        }

        private const string SourceWithIgnoredModelOverride = """
            using System.Collections.Generic;
            using Qt;
            using Qt.Bridge.Models;
            using Qt.DotNet;

            namespace Test
            {
                public class MyItemModel : Model
                {
                    public override int RowCount(ModelIndex parent) => 0;
                    public override int ColumnCount(ModelIndex parent) => 0;
                    public override ModelIndex Index(int row, int column, ModelIndex parent) => default;
                    public override ModelIndex Parent(ModelIndex index) => default;
                    public override object Data(ModelIndex index, int role) => null;

                    public override Dictionary<int, string> RoleNames() => new();

                    [Qt.Ignore]
                    public override bool CanFetchMore(ModelIndex parent) => false;
                }
            }
            """;

        private const string SourceWithListModelSubclass = """
            using System.Collections.Generic;
            using Qt.Bridge.Models;
            using Qt.DotNet;

            namespace Test
            {
                public class PersonName
                {
                    public string FirstName { get; set; }
                }

                public class NameListModel : ListModel<PersonName>
                {
                    private readonly List<PersonName> items =
                    [
                        new() { FirstName = "Ada" },
                        new() { FirstName = "Linus" }
                    ];

                    public override int ItemCount() => items.Count;
                    public override PersonName Data(int index) => items[index];
                }
            }
            """;

        private const string SourceWithTableModelSubclass = """
            using Qt.Bridge.Models;
            using Qt.DotNet;

            namespace Test
            {
                public class NumberTableModel : TableModel<int>
                {
                    protected override int Rows => 2;
                    protected override int Columns => 3;

                    protected override int this[int row, int col]
                    {
                        get => row * 10 + col;
                        set { }
                    }
                }
            }
            """;

        private const string SourceWithTreeModelSubclass = """
            using Qt.Bridge.Models;

            namespace Test
            {
                public class Node
                {
                    public Node Parent { get; set; }
                }

                public class NodeTreeModel : Qt.Bridge.Models.NodeTreeModel<Node>
                {
                    protected override int RootCount => 0;
                    protected override Node RootAt(int row) => null;
                    protected override int ChildCount(Node parent) => 0;
                    protected override Node ChildAt(Node parent, int row) => null;
                    protected override Node ParentOf(Node node) => node.Parent;
                    protected override int IndexOf(Node node) => 0;
                }
            }
            """;

        private const string SourceWithDefaultTreeModelSubclass = """
            using Qt.Bridge.Models;

            namespace Test
            {
                public class Node : TreeNode<Node>
                {
                    public string Name { get; set; }
                }

                public class DefaultNodeTreeModel : TreeModel<Node>
                { }
            }
            """;

        [TestMethod]
        public async Task Ignored_ModelOverride_IsNotGenerated()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithIgnoredModelOverride],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            result.SelectedFiles = result.Sink.Files.Keys
                .Where(file => !file.Contains("metadata_loader")).ToList();
            var combined = result.CombinedText;

            Assert.Contains("roleNames", combined,
                "Non-ignored override RoleNames must appear in the generated output.");
            Assert.DoesNotContain("canFetchMore", combined,
                "Ignored override CanFetchMore must not appear in the generated output.");
        }

        [TestMethod]
        public async Task Ignored_ListModelBaseStubs_IsNotGeneratedFor_ListModelSubclass()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithListModelSubclass],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/namelistmodel.cpp",
                out var cpp), "Expected generated cpp for Test::NameListModel was not found.");

            Assert.Contains("Test::NameListModel::rowCount", cpp,
                "ListModel subclass must still generate rowCount().");
            Assert.Contains("Test::NameListModel::roleNames", cpp,
                "ListModel subclass must still generate roleNames().");
            Assert.Contains("Test::NameListModel::data", cpp,
                "ListModel subclass must still generate data().");

            Assert.DoesNotContain("Test::NameListModel::index(", cpp,
                "ListModel base stub Index() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NameListModel::parent(", cpp,
                "ListModel base stub Parent() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NameListModel::sibling(", cpp,
                "ListModel base stub Sibling() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NameListModel::columnCount(", cpp,
                "ListModel base stub ColumnCount() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NameListModel::hasChildren(", cpp,
                "ListModel base stub HasChildren() must be skipped when marked [Qt.Ignore].");
        }

        [TestMethod]
        public async Task Ignored_TableModelBaseStubs_IsNotGeneratedFor_TableModelSubclass()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithTableModelSubclass],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/numbertablemodel.cpp",
                out var cpp), "Expected generated cpp for Test::NumberTableModel was not found.");

            Assert.Contains("Test::NumberTableModel::rowCount", cpp,
                "TableModel subclass must still generate rowCount().");
            Assert.Contains("Test::NumberTableModel::columnCount", cpp,
                "TableModel subclass must still generate columnCount().");
            Assert.Contains("Test::NumberTableModel::data", cpp,
                "TableModel subclass must still generate data().");

            Assert.DoesNotContain("Test::NumberTableModel::index(", cpp,
                "TableModel base stub Index() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NumberTableModel::parent(", cpp,
                "TableModel base stub Parent() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NumberTableModel::sibling(", cpp,
                "TableModel base stub Sibling() must be skipped when marked [Qt.Ignore].");
            Assert.DoesNotContain("Test::NumberTableModel::hasChildren(", cpp,
                "TableModel base stub HasChildren() must be skipped when marked [Qt.Ignore].");
        }

        [TestMethod]
        public async Task TreeModelBase_Overrides_AreGeneratedFor_TreeModelSubclass()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithTreeModelSubclass],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/nodetreemodel.cpp",
                out var cpp), "Expected generated cpp for Test::NodeTreeModel was not found.");

            Assert.Contains("#include <QAbstractItemModel>", cpp,
                "TreeModel subclasses must use QAbstractItemModel.");
            Assert.Contains("Test::NodeTreeModel::index(", cpp,
                "TreeModel subclass must generate index().");
            Assert.Contains("Test::NodeTreeModel::parent(", cpp,
                "TreeModel subclass must generate parent().");
            Assert.Contains("Test::NodeTreeModel::rowCount", cpp,
                "TreeModel subclass must generate rowCount().");
        }

        [TestMethod]
        public async Task DefaultTreeModelBase_Overrides_AreGeneratedFor_TreeModelSubclass()
        {
            using var result = await TestCodeGenerator.GenerateAsync(
                [SourceWithDefaultTreeModelSubclass],
                sourceRefs: [ApiAssembly, AdapterAssembly],
                ct: TestContext.CancellationTokenSource.Token);

            Assert.IsTrue(result.Sink.Files.TryGetValue("source/cpp/test/defaultnodetreemodel.cpp",
                out var cpp), "Expected generated cpp for Test::DefaultNodeTreeModel was not found.");

            Assert.Contains("#include <QAbstractItemModel>", cpp,
                "Default TreeModel subclasses must use QAbstractItemModel.");
            Assert.Contains("Test::DefaultNodeTreeModel::index(", cpp,
                "Default TreeModel subclasses must generate index().");
            Assert.Contains("Test::DefaultNodeTreeModel::parent(", cpp,
                "Default TreeModel subclasses must generate parent().");
        }

        [TestMethod]
        public void ListModelBaseStubs_Return_SensibleModelValues()
        {
            var model = new NameListModel("John", "Jane");

            Assert.AreSame(ModelIndex.Empty, model.Index(0, 0, ModelIndex.Empty));
            Assert.AreSame(ModelIndex.Empty, model.Sibling(0, 0, ModelIndex.Empty));
            Assert.AreSame(ModelIndex.Empty, model.Parent(ModelIndex.Empty));
            Assert.AreEqual(2, model.RowCount(ModelIndex.Empty));
            Assert.AreEqual(0, model.RowCount(new ModelIndex(0, 0)));
            Assert.AreEqual(1, model.ColumnCount(ModelIndex.Empty));
            Assert.AreEqual(0, model.ColumnCount(new ModelIndex(0, 0)));
            Assert.IsTrue(model.HasChildren(ModelIndex.Empty));
            Assert.IsFalse(model.HasChildren(new ModelIndex(0, 0)));
        }

        [TestMethod]
        public void GenericListModel_ConvertibleType_Exposes_Display_Edit_And_ItemRoles()
        {
            var model = new EditableNameListModel("Jane");
            var index = new ModelIndex(0, 0);

            var roles = model.GetRoleNames();
            Assert.AreEqual("display", roles[0]);
            Assert.AreEqual("edit", roles[2]);
            Assert.AreEqual("item", roles[0x0100]);

            Assert.AreEqual("Jane", model.GetData(index, 0));
            Assert.AreEqual("Jane", model.GetData(index, 2));
            Assert.AreEqual("Jane", model.GetData(index, 0x0100));

            Assert.IsTrue(model.SetEdit(index, "Grace"));
            Assert.AreEqual("Grace", model.GetData(index, 0));
        }

        [TestMethod]
        public void GenericListModel_CustomType_Uses_ItemInterfaces_And_PropertyRoles()
        {
            var item = new PersonItem {
                Name = "John",
                IsEnabled = true,
                IsSelectable = false,
                IsEditable = true
            };
            var model = new PersonListModel(item);
            var index = new ModelIndex(0, 0);

            var roles = model.GetRoleNames();
            var nameRole = roles.Single(x => x.Value == "name").Key;

            Assert.AreEqual("display", roles[0]);
            Assert.AreEqual("edit", roles[2]);
            Assert.AreEqual("item", roles[0x0100]);
            Assert.AreEqual("John", model.GetData(index, 2));
            Assert.AreEqual("Display:John", model.GetData(index, 0));
            Assert.AreEqual("John", model.GetData(index, nameRole));
            Assert.AreEqual(item, model.GetData(index, 0x0100));

            Assert.AreEqual(32 | 2, model.GetFlags(index));

            Assert.IsTrue(model.SetByRole(index, "Grace", 2));
            Assert.AreEqual("Grace", item.Name);

            Assert.IsTrue(model.SetByRole(index, "Jane", nameRole));
            Assert.AreEqual("Jane", item.Name);
        }

        [TestMethod]
        public void TableModelBaseStubs_Return_SensibleModelValues()
        {
            var model = new NumberTableModel(2, 3);

            Assert.AreSame(ModelIndex.Empty, model.Index(0, 0, ModelIndex.Empty));
            Assert.AreSame(ModelIndex.Empty, model.Sibling(0, 1, ModelIndex.Empty));
            Assert.AreSame(ModelIndex.Empty, model.Parent(ModelIndex.Empty));
            Assert.AreEqual(3, model.ColumnCount(ModelIndex.Empty));
            Assert.AreEqual(0, model.ColumnCount(new ModelIndex(0, 0)));
            Assert.IsTrue(model.HasChildren(ModelIndex.Empty));
            Assert.IsFalse(model.HasChildren(new ModelIndex(0, 0)));
        }

        [TestMethod]
        public void TreeModel_NavigatesHierarchy_AndExposesNodeProperties()
        {
            var root = new TreeNode("Root");
            var child = new TreeNode("Child");
            root.Add(child);
            var model = new NameTreeModel(root);

            var rootIndex = model.Index(0, 0, ModelIndex.Empty);
            var childIndex = model.Index(0, 0, rootIndex);
            var roles = model.GetRoleNames();
            var nameRole = roles.Single(x => x.Value == "name").Key;

            Assert.IsTrue(rootIndex.IsValid);
            Assert.IsTrue(childIndex.IsValid);
            Assert.AreSame(ModelIndex.Empty, model.Parent(rootIndex));
            Assert.AreEqual(rootIndex.Id, model.Parent(childIndex).Id);
            Assert.AreEqual(1, model.RowCount(ModelIndex.Empty));
            Assert.AreEqual(1, model.RowCount(rootIndex));
            Assert.AreEqual(1, model.ColumnCount(rootIndex));
            Assert.IsTrue(model.HasChildren(rootIndex));
            Assert.AreEqual("Child", model.GetData(childIndex, nameRole));
            Assert.AreEqual(child, model.GetData(childIndex, 0x0100));

            Assert.IsTrue(model.SetByRole(childIndex, "Renamed", nameRole));
            Assert.AreEqual("Renamed", child.Name);
        }

        [TestMethod]
        public void NodeTreeModel_WholeNodeReplacement_UpdatesTheExistingIndex()
        {
            var original = new TreeNode("Original");
            var replacement = new TreeNode("Replacement");
            var model = new ReplaceableTreeModel(original);
            var index = model.Index(0, 0, ModelIndex.Empty);

            Assert.IsTrue(model.SetByRole(index, replacement, 0x0100));
            Assert.AreSame(replacement, model.GetData(index, 0x0100));
        }

        [TestMethod]
        public void DefaultTreeModel_ManagesTreeNodeHierarchy_AndHidesNavigationRoles()
        {
            var model = new TreeModel<FolderNode>();
            var root = model.AddRoot(new FolderNode("Projects"));
            var child = model.AddChild(root, new FolderNode("Qt"));
            var rootIndex = model.Index(0, 0, ModelIndex.Empty);
            var childIndex = model.Index(0, 0, rootIndex);
            var roles = model.RoleNames();
            var nameRole = roles.Single(x => x.Value == "name").Key;

            Assert.AreSame(root, child.Parent);
            Assert.HasCount(1, root.Children);
            Assert.AreEqual("Qt", model.Data(childIndex, nameRole));
            Assert.IsFalse(roles.ContainsValue("parent"));
            Assert.IsFalse(roles.ContainsValue("children"));

            Assert.IsTrue(model.Remove(child));
            Assert.AreEqual(0, model.RowCount(rootIndex));
            Assert.IsFalse(model.Remove(child));
        }

        [TestMethod]
        public void DefaultTreeModel_SupportsDetachedTrees_PositionalInserts_AndExternalRoots()
        {
            var detachedRoot = new FolderNode("Detached");
            var detachedChild = new FolderNode("Child");
            Assert.AreSame(detachedRoot, detachedRoot.Add(detachedChild));
            Assert.AreSame(detachedRoot, detachedChild.Parent);

            List<FolderNode> roots = [];
            var model = new TreeModel<FolderNode>(roots);
            model.AddRoot(detachedRoot);
            var last = model.AddRoot(new FolderNode("Last"));
            var first = model.InsertRoot(0, new FolderNode("First"));
            var beforeChild = model.InsertChild(detachedRoot, 0, new FolderNode("Before"));

            Assert.AreSame(first, model.RootNodes[0]);
            Assert.AreSame(detachedRoot, model.RootNodes[1]);
            Assert.AreSame(last, model.RootNodes[2]);
            Assert.AreSame(beforeChild, detachedRoot.Children[0]);
            Assert.AreSame(detachedChild, detachedRoot.Children[1]);
            Assert.AreSame(first, roots[0]);

            Assert.ThrowsExactly<ArgumentException>(() => model.AddRoot(detachedChild));
            Assert.ThrowsExactly<ArgumentException>(() =>
                model.AddChild(detachedRoot, detachedChild));
            Assert.ThrowsExactly<ArgumentException>(() =>
                model.InsertChild(new FolderNode("Outside"), 0, new FolderNode("Child")));
        }

        [TestMethod]
        public void DefaultTreeModel_ReportsStructuralChanges()
        {
            var model = new TreeModel<FolderNode>();
            List<Model.EventAction> actions = [];
            model.ModelChanged += (_, args) =>
            {
                actions.Add(args.Action);
                args.Synchronized = true;
            };

            var root = model.AddRoot(new FolderNode("Root"));
            var child = model.AddChild(root, new FolderNode("Child"));
            Assert.IsTrue(model.Remove(child));

            CollectionAssert.AreEqual(new Model.EventAction[]
            {
                Model.EventAction.BeginInsertRows,
                Model.EventAction.EndInsertRows,
                Model.EventAction.BeginInsertRows,
                Model.EventAction.EndInsertRows,
                Model.EventAction.BeginRemoveRows,
                Model.EventAction.EndRemoveRows
            }, actions);
        }

        [TestMethod]
        public void DefaultTreeModel_RemovedIndexedSubtree_CanBeCollected()
        {
            var model = new TreeModel<FolderNode>();
            var childReference = RemoveIndexedSubtree(model);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(childReference.IsAlive,
                "Removing a subtree must release the model's references to its indexed nodes.");
        }

        private static WeakReference RemoveIndexedSubtree(TreeModel<FolderNode> model)
        {
            var root = model.AddRoot(new FolderNode("Root"));
            var child = model.AddChild(root, new FolderNode("Child"));
            var rootIndex = model.Index(0, 0, ModelIndex.Empty);
            _ = model.Index(0, 0, rootIndex);
            var childReference = new WeakReference(child);

            Assert.IsTrue(model.Remove(root));
            return childReference;
        }
    }
}
