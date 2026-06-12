// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qt.DotNet;

namespace Test_Qt.Bridge.CSharp.Generator
{
    [TestClass]
    public class Test_StableTypeNameBuilder
    {
        private static readonly string CoreLib = typeof(int).Assembly.GetName().Name;
        private static readonly string TestAssembly = typeof(Test_StableTypeNameBuilder)
            .Assembly.GetName().Name;

        [TestMethod]
        public void Build_SimpleType_UsesAssemblySimpleName()
        {
            Assert.AreEqual($"System.Int32, {CoreLib}", StableTypeNameBuilder.Build(typeof(int)));
        }

        [TestMethod]
        public void Build_GenericType_UsesAssemblyQualifiedArguments()
        {
            Assert.AreEqual($"System.Action`1[[System.Int32, {CoreLib}]], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(Action<int>)));
        }

        [TestMethod]
        public void Build_NestedGenericType_PreservesGenericTypeGrammar()
        {
            Assert.AreEqual("System.Collections.Generic.Dictionary`2"
                + $"[[System.String, {CoreLib}],"
                + $"[System.Action`1[[System.Int32[], {CoreLib}]], {CoreLib}]], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(Dictionary<string, Action<int[]>>)));
        }

        [TestMethod]
        public void Build_NestedBclGenericType_PreservesGenericTypeGrammar()
        {
            Assert.AreEqual("System.Collections.Generic.List`1"
                + "[[System.Collections.Generic.List`1"
                + $"[[System.Int32, {CoreLib}]], {CoreLib}]], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(List<List<int>>)));
        }

        [TestMethod]
        public void Build_GenericTypeWithLocalTypeArgument_PreservesArgumentNamespace()
        {
            Assert.AreEqual("System.Collections.Generic.IDictionary`2"
                + $"[[{typeof(Foo).FullName}, {TestAssembly}],"
                + $"[System.Collections.Generic.List`1[[System.Int32, {CoreLib}]], {CoreLib}]], "
                + CoreLib, StableTypeNameBuilder.Build(typeof(IDictionary<Foo, List<int>>)));
        }

        [TestMethod]
        public void Build_NamespaceCollision_UsesFullTypeName()
        {
            Assert.AreEqual($"{typeof(Foo).FullName}, {TestAssembly}",
                StableTypeNameBuilder.Build(typeof(Foo)));
            Assert.AreEqual($"{typeof(AnotherFoo.Foo).FullName}, {TestAssembly}",
                StableTypeNameBuilder.Build(typeof(AnotherFoo.Foo)));
        }

        [TestMethod]
        public void Build_ArrayType_AppendsArrayRankBeforeAssemblyName()
        {
            Assert.AreEqual($"System.Int32[], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(int[])));
        }

        [TestMethod]
        public void Build_MultidimensionalArrayType_AppendsArrayRankBeforeAssemblyName()
        {
            Assert.AreEqual($"System.Int32[,,], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(int[,,])));
        }

        [TestMethod]
        public void Build_NullableType_UsesGenericTypeGrammar()
        {
            Assert.AreEqual($"System.Nullable`1[[System.Int32, {CoreLib}]], {CoreLib}",
                StableTypeNameBuilder.Build(typeof(int?)));
        }

        [TestMethod]
        public void Build_GenericTypeDefinition_DoesNotAddGenericArguments()
        {
            Assert.AreEqual($"System.Nullable`1, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(Nullable<>)));
        }

        [TestMethod]
        public void Build_OpenGenericBclType_DoesNotAddGenericArguments()
        {
            Assert.AreEqual($"System.Collections.Generic.IList`1, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(IList<>)));

            Assert.AreEqual($"System.Collections.Generic.List`1, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(List<>)));
        }

        [TestMethod]
        public void Build_OpenGenericLocalType_DoesNotAddGenericArguments()
        {
            Assert.AreEqual($"{typeof(GenericType<>).FullName}, {TestAssembly}",
                StableTypeNameBuilder.Build(typeof(GenericType<>)));
        }

        [TestMethod]
        public void Build_GenericTypeParameter_UsesParameterName()
        {
            Assert.AreEqual("T", StableTypeNameBuilder.Build(typeof(GenericType<>)
                .GetGenericArguments()[0]));
        }

        [TestMethod]
        public void Build_GenericParameterArray_UsesParameterNameWithArraySuffix()
        {
            var fieldType = typeof(GenericType<>)
#if NET10_0_OR_GREATER
                .GetField(nameof(GenericType<>.ArrayField))
#else // Remove when minimum required framework is .NET 10.0
                .GetField(nameof(GenericType<bool>.ArrayField))
#endif
                ?.FieldType;

            Assert.AreEqual($"T[], {TestAssembly}", StableTypeNameBuilder.Build(fieldType));
        }

        [TestMethod]
        public void Build_OpenConstructedGenericType_PreservesGenericParameters()
        {
            var genericParameter = typeof(GenericType<>).GetGenericArguments()[0];
            var listOfT = typeof(List<>).MakeGenericType(genericParameter);
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(genericParameter, listOfT);

            Assert.AreEqual("System.Collections.Generic.Dictionary`2"
                + $"[[T],[System.Collections.Generic.List`1[[T]], {CoreLib}]], {CoreLib}",
                StableTypeNameBuilder.Build(dictionaryType));
        }

        [TestMethod]
        public void Build_PointerType_AppendsPointerSuffixBeforeAssemblyName()
        {
            Assert.AreEqual($"System.Int32*, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(int).MakePointerType()));
        }

        [TestMethod]
        public void Build_ByRefType_AppendsByRefSuffixBeforeAssemblyName()
        {
            Assert.AreEqual($"System.Int32&, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(int).MakeByRefType()));
        }

        [TestMethod]
        public void Build_IntPtr_UsesSimpleTypeName()
        {
            Assert.AreEqual($"System.IntPtr, {CoreLib}",
                StableTypeNameBuilder.Build(typeof(IntPtr)));
        }

        [TestMethod]
        public void Build_ValueTuple_UsesGenericTypeGrammar()
        {
            Assert.AreEqual($"System.ValueTuple`2[[System.Int32, {CoreLib}],[System.String, "
                + $"{CoreLib}]], " + CoreLib,
                StableTypeNameBuilder.Build(typeof((int, string))));
        }

        [TestMethod]
        public void Build_ExpandoObject_UsesSimpleTypeName()
        {
            Assert.AreEqual("System.Dynamic.ExpandoObject, System.Linq.Expressions",
                StableTypeNameBuilder.Build(typeof(ExpandoObject)));
        }

        [TestMethod]
        public void Build_AnonymousType_DoesNotIncludeRuntimeVersionMetadata()
        {
            var typeName = StableTypeNameBuilder.Build(new { X = 1, Y = "test" }.GetType());

            Assert.Contains(TestAssembly, typeName);
            Assert.DoesNotContain("Version=", typeName);
            Assert.DoesNotContain("Culture=", typeName);
            Assert.DoesNotContain("PublicKeyToken=", typeName);
        }

        [TestMethod]
        public void Build_DoesNotIncludeRuntimeVersionMetadata()
        {
            var typeName = StableTypeNameBuilder.Build(typeof(Action<int>));

            Assert.DoesNotContain("Version=", typeName);
            Assert.DoesNotContain("Culture=", typeName);
            Assert.DoesNotContain("PublicKeyToken=", typeName);
        }

        public class GenericType<T>
        {
            public T[] ArrayField;
        }

        public struct Foo;
    }

    namespace AnotherFoo
    {
        public struct Foo;
    }
}
