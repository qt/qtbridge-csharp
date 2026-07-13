// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

namespace MTest_DynamicObject
{
    [Qt.Ignore]
    public class LoadTimeType : BuildTimeType
    {
        internal static LoadTimeType LoadTimeTypeInstance { get; private set; }

        public LoadTimeType() : base(true) => LoadTimeTypeInstance ??= this;

        public override object BuildTimeTypeObj => BuildTimeType.BuildTimeTypeInstance;

        public override object LoadTimeTypeObj => LoadTimeType.LoadTimeTypeInstance;

        public override void QmlClassBegin()
        {
        }

        public override void QmlComponentComplete(object[] nestedElements)
        {
        }
    }
}
