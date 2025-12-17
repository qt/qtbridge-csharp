/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Qt.Bridge.CodeGeneration;
    using Qt.Bridge.CodeGeneration.MetaFunctions;
    using Support;

    [TestClass]
    public class Test_ArgumentGeneration
    {
        private sealed class ParameterHelper : Parameter
        {
            public string RunEval(object src, TraitFlags traits) => Eval(src, traits);
        }

        [TestMethod]
        public void Parameter_Name_FallsBack_To_argPosition_When_Missing()
        {
            var memoryStream = new MemoryStream();
            InMemoryAssemblyBuilder.Build(
                assemblyConfig: new AssemblyConfig
                {
                    AssemblyName = "ArgPositionTestAssembly",
                    TypeName = "ArgPositionTestType",
                    MethodName = "ArgPositionTestMethod",
                },
                returnConfig: new ReturnConfig
                {
                    EncodeType = returnType => returnType.Void()
                },
                parameterConfig: new ParameterConfig
                {
                    Names = ["intParam", "", null],
                    EncodeTypes = parameters =>
                    {
                        parameters.AddParameter().Type().String();
                        parameters.AddParameter().Type().String();
                        parameters.AddParameter().Type().String();
                    }
                },
                memoryStream,
                includeParamRows: true);
            memoryStream.Position = 0;

            using var metadataLoadContext = MetadataResolver.CreateLoadContext();
            var assembly = metadataLoadContext.LoadFromStream(memoryStream);

            var type = assembly.GetType("ArgPositionTestType", throwOnError: false);
            Assert.IsNotNull(type);
            var method = type
                .GetMethod("ArgPositionTestMethod", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual("intParam", parameters[0].Name);
            Assert.IsTrue(string.IsNullOrEmpty(parameters[1].Name));
            Assert.IsTrue(string.IsNullOrEmpty(parameters[2].Name));

            var parameterHelper = new ParameterHelper();
            Assert.AreEqual("intParam", parameterHelper.RunEval(parameters[0], TraitFlags.Default));
            Assert.AreEqual("arg1", parameterHelper.RunEval(parameters[1], TraitFlags.Default));
            Assert.AreEqual("arg2", parameterHelper.RunEval(parameters[2], TraitFlags.Default));
        }

        [TestMethod]
        public void Parameter_Name_FallsBack_To_argPosition_Without_ParamTableEntries()
        {
            var memoryStream = new MemoryStream();
            InMemoryAssemblyBuilder.Build(
                assemblyConfig: new AssemblyConfig
                {
                    AssemblyName = "ArgPositionTestAssembly",
                    TypeName = "ArgPositionTestType",
                    MethodName = "ArgPositionTestMethod",
                },
                returnConfig: new ReturnConfig
                {
                    EncodeType = returnType => returnType.Void()
                },
                parameterConfig: new ParameterConfig
                {
                    Names = ["firstParam", "secondParam", "thirdParam"],
                    EncodeTypes = parameters =>
                    {
                        parameters.AddParameter().Type().String();
                        parameters.AddParameter().Type().String();
                        parameters.AddParameter().Type().String();
                    }
                },
                memoryStream,
                includeParamRows: false);
            memoryStream.Position = 0;

            using var metadataLoadContext = MetadataResolver.CreateLoadContext();
            var assembly = metadataLoadContext.LoadFromStream(memoryStream);

            var type = assembly.GetType("ArgPositionTestType", throwOnError: false);
            Assert.IsNotNull(type);
            var method = type
                .GetMethod("ArgPositionTestMethod", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method);

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length);
            Assert.IsTrue(string.IsNullOrEmpty(parameters[0].Name));
            Assert.IsTrue(string.IsNullOrEmpty(parameters[1].Name));
            Assert.IsTrue(string.IsNullOrEmpty(parameters[2].Name));

            var parameterHelper = new ParameterHelper();
            Assert.AreEqual("arg0", parameterHelper.RunEval(parameters[0], TraitFlags.Default));
            Assert.AreEqual("arg1", parameterHelper.RunEval(parameters[1], TraitFlags.Default));
            Assert.AreEqual("arg2", parameterHelper.RunEval(parameters[2], TraitFlags.Default));
        }
    }
}
