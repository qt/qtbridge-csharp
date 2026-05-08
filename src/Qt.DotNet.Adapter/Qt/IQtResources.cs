// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt
{
    namespace DotNet
    {
        public partial class Adapter
        {
            public partial interface IStatic
            {
                IQtResources QtResources_Get();
            }

            public static IQtResources QtResources() => Static.QtResources_Get();
        }
    }

    public interface IQtResources
    {
        bool Exists(string qrcUrl);
        int Size(string qrcUrl);
        int Read(string qrcUrl, IntPtr destination, int destinationLength);
    }
}
