// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test_Qt.Bridge.CSharp.Generator
{
    using Support;

    [TestClass]
    public class Test_CppKeywords
    {
        private const string Source = """
            using System;
            using System.Collections.Generic;
            namespace template
            {
                public class noexcept
                {
                    public enum typedef { _asm, auto }
                    public event EventHandler Protected;
                    public int Default;
                    public List<typedef> Friend { get; set; }
                    public void Break(bool constexpr)
                    { }
                }
            }
            """;

        [TestMethod]
        public async Task CppKeywords()
        {
            using var result = await TestCodeGenerator.GenerateAsync([Source]);
            Assert.Contains("class template_::noexcept_ :", result.CombinedText);
            Assert.Contains("EVENT_Protected Q_SIGNAL void protected_(QObject *qEvArgs);", result.CombinedText);
            Assert.Contains("Q_PROPERTY(qint32 default_ READ default_ WRITE setDefault)", result.CombinedText);
            Assert.Contains("Q_PROPERTY(System::Collections::Generic::List_1__noexcept__typedef_ *friend_ READ friend_ WRITE setFriend)", result.CombinedText);
            Assert.Contains("Q_INVOKABLE void break_(bool constexpr_) const;", result.CombinedText);
            Assert.Contains("template_::noexcept_::noexcept_() :", result.CombinedText);
            Assert.Contains("QDotNetObject(constructor<template_::noexcept_>().invoke(nullptr))", result.CombinedText);
            Assert.Contains("d(new template_::noexcept_Private(this))", result.CombinedText);
            Assert.Contains("i(new template_::noexcept_Init(this, d))", result.CombinedText);
            Assert.Contains("template_::noexcept_::~noexcept_()", result.CombinedText);
            Assert.Contains("void template_::noexcept_::break_(bool constexpr_) const", result.CombinedText);
            Assert.Contains("class template_::noexcept__typedef_Enum : public QObject", result.CombinedText);
            Assert.Contains("QML_NAMED_ELEMENT(noexcept__typedef_)", result.CombinedText);
            Assert.Contains("QML_UNCREATABLE(\"Type 'noexcept__typedef_' is an enum.\")", result.CombinedText);
            Assert.Contains("z_asm = 0", result.CombinedText);
            Assert.Contains("auto_ = 1", result.CombinedText);
            Assert.Contains("Q_ENUM(Values)", result.CombinedText);
        }
    }
}
