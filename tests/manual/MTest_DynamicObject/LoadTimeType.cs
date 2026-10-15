// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace MTest_DynamicObject
{

    [Qt.Export(Options = Qt.ExportAs.Metadata)]
    public enum LoadTimeConsts { Foo = 41, Bar }

    [Qt.Export(Options = Qt.ExportAs.Metadata)]
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

        public new LoadTimeConsts EnumFuncLoadTime(LoadTimeConsts x) => x;
        public new BuildTimeConsts EnumFuncBuildTime(BuildTimeConsts x) => x;
    }
}
