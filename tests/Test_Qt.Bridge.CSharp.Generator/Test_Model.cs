// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

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
    }
}
