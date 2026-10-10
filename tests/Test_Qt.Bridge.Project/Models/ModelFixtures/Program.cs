// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Qt.Bridge.Models;
using Qt.DotNet;
using Qt.Quick;

[assembly: Qt.Generate(Packages = "QuickTest", Libraries = "Qt6::QuickTest")]

namespace Test_QtQuickTest
{
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class TestTable : TableModel<int>
    {
        private int rowCount = 2;
        private int columnCount = 2;

        protected override int Rows => rowCount;
        protected override int Columns => columnCount;

        protected override int this[int row, int col]
        {
            get => col - row;
            set { }
        }

        protected override bool CanInsertRows(int row, int count) => row >= 0 && count > 0;

        protected override bool InsertRows(int row, int count)
        {
            if (!CanInsertRows(row, count))
                return false;
            rowCount = Math.Max(rowCount, row) + count;
            return true;
        }

        protected override bool CanInsertColumns(int column, int count)
            => column >= 0 && count > 0;

        protected override bool InsertColumns(int column, int count)
        {
            if (!CanInsertColumns(column, count))
                return false;
            columnCount = Math.Max(columnCount, column) + count;
            return true;
        }
    }

    public sealed class FolderNode(string name) : TreeNode<FolderNode>
    {
        public string Name { get; } = name;
    }

    [QmlElement]
    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class MetadataTree : TreeModel<FolderNode>
    {
        public MetadataTree()
        {
            var projects = AddRoot(new FolderNode("Projects"));
            AddChild(projects, new FolderNode("Qt"));
        }
    }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class Person(string name, int age)
    {
        public string Name { get; } = name;
        public int Age { get; } = age;
    }

    [QmlElement]
    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class CollectionModels : IQmlElement
    {
        public int[] Numbers { get; } = [2, 3, 5];

        public List<Person> People { get; } =
        [
            new("Ada", 36),
            new("Grace", 85)
        ];

        public void QmlClassBegin()
        { }

        public void QmlComponentComplete(object[] nestedElements)
        { }
    }

    [QmlElement]
    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class PlainElement
    {
        public int Value() => 42;
    }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class NotifyingCollection : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Notifies its property changes, so its generated wrapper caches the collection it returns.
        public ObservableCollection<Person> Items { get; } = [new("Ada", 36), new("Grace", 85)];
    }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class AdapterReferences
    {
        // Object references the adapter holds on behalf of native code.
        public int ObjectRefCount()
        {
            Adapter.Stats(out var objectCount, out _, out _);
            return objectCount;
        }
    }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class FreedMemory
    {
        private readonly List<nint> blocks = [];

        // Overwrite recently freed native memory, so the use of a deleted object no longer goes
        // unnoticed by chance. The heap hands out free blocks in no particular order, so this
        // takes many blocks of each small size, and keeps them until Release(), as freeing them
        // would write heap bookkeeping into them again.
        public void Overwrite()
        {
            var filler = new byte[512];
            Array.Fill(filler, (byte)0xA5);
            for (var size = 16; size <= filler.Length; size += 8) {
                for (var i = 0; i < 2048; ++i) {
                    var block = Marshal.AllocHGlobal(size);
                    Marshal.Copy(filler, 0, block, size);
                    blocks.Add(block);
                }
            }
        }

        public void Release()
        {
            blocks.ForEach(Marshal.FreeHGlobal);
            blocks.Clear();
        }
    }

    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class MetadataPersonCollection : ObservableCollection<Person>
    { }

    [QmlElement]
    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class MetadataObservable
    {
        public MetadataPersonCollection People { get; } = [];

        public void AddPerson(string name) => People.Add(new(name, People.Count));
        public void RemoveFirstPerson() => People.RemoveAt(0);
        public void ClearPeople() => People.Clear();

        public MetadataScriptedPeople Scripted { get; } = [];

        public ObservableCollection<Person> Items { get; } = [];

        public void Add(string name) => Items.Add(new(name, Items.Count));
        public void RemoveFirst() => Items.RemoveAt(0);
    }

    // Raises collection changes the way custom INotifyCollectionChanged implementations may:
    // without a position, at a position out of range, or with arguments of a type of their own.
    public abstract class ScriptedPeople : ObservableCollection<Person>
    {
        public void AddUnpositioned(string name)
        {
            var person = new Person(name, Count);
            Items.Add(person);
            OnCollectionChanged(new(NotifyCollectionChangedAction.Add, person));
        }

        public void AddOutOfRange(string name)
        {
            var person = new Person(name, Count);
            Items.Add(person);
            OnCollectionChanged(new(NotifyCollectionChangedAction.Add, person, Count + 5));
        }

        public void AddWithCustomArgs(string name)
        {
            var person = new Person(name, Count);
            Items.Add(person);
            OnCollectionChanged(new CustomCollectionChangedEventArgs(person, Count - 1));
        }

        public void ReplaceFirstUnpositioned(string name)
        {
            var replaced = Items[0];
            var person = new Person(name, 0);
            Items[0] = person;
            OnCollectionChanged(new(NotifyCollectionChangedAction.Replace, person, replaced));
        }

        public void RemoveFirstUnpositioned()
        {
            var person = Items[0];
            Items.RemoveAt(0);
            OnCollectionChanged(new(NotifyCollectionChangedAction.Remove, person));
        }

        public void MoveFirstToLast() => Move(0, Count - 1);
    }

    public sealed class CustomCollectionChangedEventArgs(object item, int index)
        : NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);

    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public sealed class MetadataScriptedPeople : ScriptedPeople
    { }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class SourceScriptedPeople : ScriptedPeople
    { }

    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public sealed class SourceObservable
    {
        public SourceScriptedPeople Scripted { get; } = [];
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            Qml.WaitForExit();
            return 0;
        }
    }
}

namespace Test_ModelRemoveRanges
{
    [Qt.Export(Options = Qt.ExportAs.SourceCode)]
    public class RemoveRangeTableModel : TableModel<string>
    {
        private List<List<string>> Items { get; } = Enumerable.Range(0, 10)
            .Select(row => Enumerable.Range(0, 10)
                .Select(col => $"R{row}C{col}")
                .ToList())
            .ToList();

        public int LastRowRemoveFirst { get; private set; } = -1;
        public int LastRowRemoveLast { get; private set; } = -1;
        public int LastColumnRemoveFirst { get; private set; } = -1;
        public int LastColumnRemoveLast { get; private set; } = -1;

        public RemoveRangeTableModel()
        {
            ModelChanged += (_, args) => {
                if (args.Action == EventAction.BeginRemoveRows) {
                    LastRowRemoveFirst = args.First;
                    LastRowRemoveLast = args.Last;
                }

                if (args.Action == EventAction.BeginRemoveColumns) {
                    LastColumnRemoveFirst = args.First;
                    LastColumnRemoveLast = args.Last;
                }
            };
        }

        protected override int Rows => Items.Count;
        protected override int Columns => Items.Count > 0 ? Items[0].Count : 0;

        protected override string this[int row, int col]
        {
            get => Items[row][col];
            set => Items[row][col] = value;
        }

        protected override bool CanRemoveRows(int row, int count)
            => row >= 0 && count > 0 && row + count <= Items.Count;

        protected override bool RemoveRows(int row, int count)
        {
            if (!CanRemoveRows(row, count))
                return false;
            Items.RemoveRange(row, count);
            return true;
        }

        protected override bool CanRemoveColumns(int column, int count)
            => column >= 0 && count > 0 && column + count <= Columns;

        protected override bool RemoveColumns(int column, int count)
        {
            if (!CanRemoveColumns(column, count))
                return false;
            foreach (var row in Items)
                row.RemoveRange(column, count);
            return true;
        }

        public void ResetLastRemoveRanges()
        {
            LastRowRemoveFirst = -1;
            LastRowRemoveLast = -1;
            LastColumnRemoveFirst = -1;
            LastColumnRemoveLast = -1;
        }

        public bool RemoveRowsViaModelApi(int row, int count)
            => RemoveRows(row, count, ModelIndex.Empty);

        public bool RemoveColumnsViaModelApi(int column, int count)
            => RemoveColumns(column, count, ModelIndex.Empty);
    }
}
