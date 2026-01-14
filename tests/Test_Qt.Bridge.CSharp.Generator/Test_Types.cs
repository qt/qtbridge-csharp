// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.DotNet;
    using Support;

    [TestClass]
    public class Test_Types
    {
        private const string Source = """
            using System;
            using System.ComponentModel;
            using System.Text;
            using Qt.DotNet;

            namespace MyApp
            {
                public class MyClass : INotifyPropertyChanged
                {
                    public event PropertyChangedEventHandler PropertyChanged;

                    public int MyIntField;
                    public double MyDoubleField;
                    public bool MyBoolField;
                    public char MyCharField;
                    public decimal MyDecimalField;
                    public string MyStringField;
                    public ModelIndex MyModelIndexField;
                    public DateTime MyDateTimeField;
                    public Uri MyUriField;
                    public object MyObjectField;
                    public MyOtherClass MyOtherClassField;

                    public int MyIntProperty { get; set; }
                    public double MyDoubleProperty { get; set; }
                    public bool MyBoolProperty { get; set; }
                    public char MyCharProperty { get; set; }
                    public decimal MyDecimalProperty { get; set; }
                    public string MyStringProperty { get; set; }
                    public ModelIndex MyModelIndexProperty { get; set; }
                    public DateTime MyDateTimeProperty { get; set; }
                    public Uri MyUriProperty { get; set; }
                    public object MyObjectProperty { get; set; }
                    public MyOtherClass MyOtherClassProperty { get; set; }

                    public MyClass()
                    { }

                    public MyClass(
                        int myIntArg,
                        double myDoubleArg,
                        bool myBoolArg,
                        char myCharArg,
                        decimal myDecimalArg,
                        string myStringArg,
                        ModelIndex myModelIndexArg,
                        DateTime myDateTimeArg,
                        Uri myUriArg,
                        object myObjectArg,
                        MyOtherClass myOtherClassArg)
                    { }

                    public void MyFunc(
                        int myIntArg,
                        double myDoubleArg,
                        bool myBoolArg,
                        char myCharArg,
                        decimal myDecimalArg,
                        string myStringArg,
                        ModelIndex myModelIndexArg,
                        DateTime myDateTimeArg,
                        Uri myUriArg,
                        object myObjectArg,
                        MyOtherClass myOtherClassArg)
                    { }

                    public object this[
                        int myIntArg,
                        double myDoubleArg,
                        bool myBoolArg,
                        char myCharArg,
                        decimal myDecimalArg,
                        string myStringArg,
                        ModelIndex myModelIndexArg,
                        DateTime myDateTimeArg,
                        Uri myUriArg,
                        object myObjectArg,
                        MyOtherClass myOtherClassArg]
                    {
                        get
                        {
                            return null;
                        }
                    }
                }

                public class MyOtherClass
                { }
            }
            """;

        [TestMethod]
        public async Task Types()
        {
            _ = await TestCodeGenerator.GenerateAsync([Source],
                sourceRefs: [
                    typeof(ModelIndex).Assembly,
                    typeof(Uri).Assembly,
                    typeof(INotifyPropertyChanged).Assembly]);
        }
    }
}
