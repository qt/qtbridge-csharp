// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

using Refit;
using Qt.Bridge.Models;
using Qt.Bridge.Text;
using Qt.DotNet;

namespace ColorPalette
{
    public sealed partial class PaginatedResource
    {
        public Model Data { get; set; }

        public PaginatedResource()
        {
            Data = new(this);
        }

        public class Model : ListModel
        {
            private Dictionary<int, string> RoleMap { get; set; }
            private PaginatedResource ResourcePage { get; set; }

            private const string ResourceRole = "resource";

            internal Model(PaginatedResource resPage)
            {
                ResourcePage = resPage;
                RoleMap = ResourceRoles
                    .Select((r, i) => new
                    {
                        Role = r.ConvertCase(CaseStyle.Pascal, CaseStyle.Camel),
                        Index = Roles.UserRole + 1 + i
                    })
                    .Prepend(new { Role = ResourceRole, Index = Roles.UserRole })
                    .ToDictionary(x => x.Index, x => x.Role);
            }

            public object this[int index]
            {
                get
                {
                    if (index < 0 || index >= ResourcePage.Resources.Count)
                        return null;
                    return ResourcePage.Resources[index];
                }
            }

            public object this[int index, string role]
            {
                get
                {
                    if (this[index] is not ResourceData resource)
                        return null;
                    if (role == ResourceRole)
                        return resource;
                    return resource[role.ConvertCase(CaseStyle.Camel, CaseStyle.Pascal)];
                }
            }

            public object this[int index, int role]
            {
                get
                {
                    if (index < 0 || index >= ResourcePage.Resources.Count)
                        return null;
                    if (!RoleMap.TryGetValue(role, out var roleName))
                        return null;
                    return this[index, roleName];
                }
            }

            public int Find(string role, object value)
            {
                for (int i = 0; i < ResourcePage.Resources.Count; ++i) {
                    if (this[i, role].Equals(value))
                        return i;
                }
                return -1;
            }

            public override Dictionary<int, string> RoleNames()
            {
                return RoleMap;
            }

            public override object Data(ModelIndex index, int role)
            {
                return this[index.Row, role];
            }

            public override int RowCount(ModelIndex parent)
            {
                return ResourcePage.Resources.Count;
            }
        }
    }
}
