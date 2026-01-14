// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System;

namespace Qt.Quick
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = false)]
    public class QmlElementAttribute : Attribute
    {
        public string Name { get; set; }
        public bool Singleton { get; set; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class QmlElementAttribute<T> : QmlElementAttribute
    {
    }

    public interface IQmlElement
    {
        void QmlClassBegin();
        void QmlComponentComplete(object[] nestedElements);
    }
}
