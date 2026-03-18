// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: Qt.Generate(Packages = "Test", Libraries = "Qt6::Test")]

namespace BindingsAndInterfaces
{
    internal class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("BindingsAndInterfaces managed app ready");
            return 0;
        }
    }

    public interface ITextTransformation
    {
        string Transform(string text);
        Uri GetUri(int n);
        void SetUri(Uri uri);
        int GetNumber();
    }

    public sealed class TransformedTextSource : INotifyPropertyChanged
    {
        readonly ITextTransformation transformation;
        string text = "";

        public TransformedTextSource()
            : this(null)
        {
        }

        public TransformedTextSource(ITextTransformation transformation)
        {
            this.transformation = transformation;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Text
        {
            get => text;
            set
            {
                text = value;
                if (transformation is ITextTransformation) {
                    text = transformation.Transform(value) ?? text;
                    transformation.SetUri(new("https://qt.io/developers"));
                    var n = transformation.GetNumber();
                    if (transformation.GetUri(n) is { } uri)
                        text += $" ({uri})";
                }
                NotifyPropertyChanged();
            }
        }
    }
}
