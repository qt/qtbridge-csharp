// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using System.ComponentModel;
using Qt.Bridge.Models;
using Qt.DotNet;
using Qt.Quick;

namespace MTest_DynamicObject
{
    public class BuildTimeType : Model, INotifyPropertyChanged, IQmlElement
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void QmlClassBegin()
        {
        }

        public virtual void QmlComponentComplete(object[] nestedElements)
        {
        }

        internal static BuildTimeType BuildTimeTypeInstance { get; private set; }

        public BuildTimeType() => BuildTimeTypeInstance ??= this;

        protected BuildTimeType(bool load_time_obj) { }

        protected int intField = 0;

        public int IntProperty
        {
            get => intField;
            set
            {
                if (intField == value)
                    return;
                intField = value;
                PropertyChanged?.Invoke(this, new(nameof(IntProperty)));
                PropertyChanged?.Invoke(this, new(nameof(IntReadOnlyProperty)));
            }
        }

        public int IntReadOnlyProperty => IntProperty;

        public int IntWriteOnlyProperty { set => IntProperty = value; }

        public string StringProperty { get; set; }

        public DateTime DateTimeProperty { get; set; } = new(1995, 5, 20);

        public Uri UriProperty { get; set; }
            = new("https://www.qt.io/development/qt-framework/qt-bridges");

        public virtual object BuildTimeTypeObj => BuildTimeType.BuildTimeTypeInstance;

        public virtual object LoadTimeTypeObj => LoadTimeType.LoadTimeTypeInstance;

        public ulong UInt64FuncInt(int n)
        {
            if (n == 0)
                return 0;
            if (n == 1)
                return 1;
            ulong fN = 1, fN_1 = 1, fN_2 = 0;
            for (int i = 2; i < n; i++) {
                fN_2 = fN_1;
                fN_1 = fN;
                fN = fN_1 + fN_2;
            }
            return fN;
        }

        public BuildTimeType VoidFuncObject(LoadTimeType obj)
        {
            return obj;
        }

        public List<string> Columns { get; } = ["First Name", "LastName"];

        public class TreeNode
        {
            private static Dictionary<nint, TreeNode> Nodes { get; } = new();
            private static nint NextId { get; set; } = 1;

            public static TreeNode Find(nint id)
            {
                if (!Nodes.TryGetValue(id, out var node))
                    return null;
                return node;
            }

            public nint Id { get; private set; }
            public List<string> Values { get; set; } = new();

            public TreeNode(params string[] values)
            {
                Nodes[Id = NextId++] = this;
                Values = values.ToList();
            }

            public TreeNode Parent { get; set; }
            public List<TreeNode> Children { get; } = new();

            public TreeNode Insert(int index, TreeNode child)
            {
                if (child.Parent != null)
                    return this;
                try {
                    Children.Insert(index, child);
                } catch (ArgumentOutOfRangeException) {
                    return this;
                }
                child.Parent = this;
                return this;
            }

            public TreeNode Add(TreeNode child)
            {
                return Insert(Children.Count, child);
            }

            public TreeNode Add(params string[] values)
            {
                return Add(new TreeNode(values));
            }

            public int Index
            {
                get
                {
                    if (Parent == null)
                        return -1;
                    return Parent.Children.IndexOf(this);
                }
            }
        }

        public static TreeNode Root { get; } = new TreeNode()
            .Add(new TreeNode("Beatles")
                .Add("John", "Lennon")
                .Add("Paul", "McCartney")
                .Add("George", "Harrison")
                .Add("Ringo", "Starr"))
            .Add(new TreeNode("Rolling", "Stones")
                .Add("Mick", "Jagger")
                .Add("Keith", "Richards")
                .Add("Charlie", "Watts")
                .Add("Bill", "Wyman")
                .Add("Brian", "Jones"));

        public override ModelIndex Parent(ModelIndex index)
        {
            if (index is not { IsValid: true, Id: > 0 })
                return null;
            if (TreeNode.Find(index.Id) is not { } node)
                return ModelIndex.Empty;
            if (node.Parent == null || node.Parent == Root)
                return ModelIndex.Empty;
            return new ModelIndex(node.Parent.Index, 0, node.Parent.Id);
        }

        public override bool HasChildren(ModelIndex index)
        {
            if (index is not { IsValid: true, Id: > 0 })
                return false;
            if (TreeNode.Find(index.Id) is not { } node)
                return false;
            return node.Children.Any();
        }

        public override ModelIndex Index(int row, int column, ModelIndex index)
        {
            var node = index switch
            {
                { IsValid: true, Id: > 0 } => TreeNode.Find(index.Id),
                _ => Root
            };
            if (node == null)
                return ModelIndex.Empty;

            if (row < 0)
                return ModelIndex.Empty;
            if (row >= (node.Children.Any() ? node.Children.Count : 1))
                return ModelIndex.Empty;

            if (column < 0)
                return ModelIndex.Empty;
            if (column >= 2)
                return ModelIndex.Empty;

            if (node.Children.Any())
                node = node.Children[row];

            return new ModelIndex(row, column, node.Id);
        }

        public override int RowCount(ModelIndex parent)
        {
            var node = parent switch
            {
                { IsValid: true, Id: > 0 } => TreeNode.Find(parent.Id),
                _ => Root
            };
            if (node == null)
                return 0;
            return node.Children.Any() ? node.Children.Count : 1;
        }

        public override int ColumnCount(ModelIndex parent) => Columns.Count;

        private static Dictionary<int, string> RoleNamesById { get; } = new()
        {
            { Roles.DisplayRole, "display" },
            { Roles.EditRole, "edit" }
        };
        public override Dictionary<int, string> RoleNames() => RoleNamesById;

        public override int Flags(ModelIndex index) => base.Flags(index) | ItemFlags.ItemIsEditable;

        public override object Data(ModelIndex index, int role)
        {
            if (index is not { IsValid: true, Id: > 0 })
                return null;
            if (TreeNode.Find(index.Id) is not { } node)
                return null;
            if (index.Column < 0 || index.Column >= Columns.Count)
                return null;
            return role switch
            {
                Roles.DisplayRole or Roles.EditRole
                    when index.Column < node.Values.Count => node.Values[index.Column],
                Roles.DisplayRole or Roles.EditRole => "",
                _ => null
            };
        }

        public override bool SetData(ModelIndex index, object value, int role)
        {
            if (index is not { IsValid: true, Id: > 0 })
                return false;
            if (TreeNode.Find(index.Id) is not { } node || node.Values is not { } row)
                return false;
            if (index.Column < 0 || index.Column >= row.Count)
                return false;
            if (role != Roles.EditRole)
                return false;
            row[index.Column] = value.ToString();
            DataChanged(index, index, [Roles.DisplayRole, Roles.EditRole]);
            return true;
        }

        public override object HeaderData(int section, int orientation, int role)
        {
            return (orientation, role) switch
            {
                (HeaderOrientation.Horizontal, Roles.DisplayRole)
                    when 0 <= section && section < Columns.Count => Columns[section],
                _ => null
            };
        }

        public override bool InsertRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is not { IsValid: true, Id: > 0 })
                return false;
            if (TreeNode.Find(parent.Id) is not { } node)
                return false;
            if (row < 0 || row > node.Children.Count)
                return false;
            if (count < 1)
                return false;
            BeginInsertRows(parent, row, row + count - 1);
            try {
                var newNodes = Enumerable.Range(0, count)
                    .Select(_ => new TreeNode("(empty)", "(empty)"))
                    .ToList();
                foreach (var newNode in newNodes)
                    node.Insert(row, newNode);
            } finally {
                EndInsertRows();
            }
            return true;
        }

        public override bool RemoveRows(int row, int count, ModelIndex parent = null)
        {
            if (parent is not { IsValid: true, Id: > 0 })
                return false;
            if (TreeNode.Find(parent.Id) is not { } node)
                return false;
            if (row < 0 || row >= node.Children.Count)
                return false;
            if (count < 1 || row + count > node.Children.Count)
                return false;
            BeginRemoveRows(parent, row, row + count - 1);
            try {
                node.Children.RemoveRange(row, count);
            } finally {
                EndRemoveRows();
            }
            return true;
        }
    }
}
