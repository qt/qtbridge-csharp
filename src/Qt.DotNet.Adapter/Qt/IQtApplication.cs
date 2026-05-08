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
                IQtApplication QtApplication_Get();
            }

            public static IQtApplication QtApplication() => Static.QtApplication_Get();
        }
    }

    public interface IQtApplication
    {
        void SetName(string name);
        void SetVersion(string version);
        void SetOrganizationName(string name);
        void SetOrganizationDomain(string domain);
        void SetDisplayName(string name);
        void SetWindowIcon(string qrcUrl);
    }
}
