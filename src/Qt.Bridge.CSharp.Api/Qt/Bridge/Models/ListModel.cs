// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.DotNet;

namespace Qt.Bridge.Models
{
    using Text;

    public abstract class ListModel : Model
    {
        public sealed override ModelIndex Index(int row, int column, ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Sibling(int row, int column, ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override int ColumnCount(ModelIndex parent)
        {
            throw new NotImplementedException();
        }

        public sealed override ModelIndex Parent(ModelIndex index)
        {
            throw new NotImplementedException();
        }

        public sealed override bool HasChildren(ModelIndex parent)
        {
            return base.HasChildren(parent);
        }
    }

    public abstract class ListModel<T> : ListModel
    {
        public abstract int ItemCount();
        public abstract T Data(int index);

        private Dictionary<int, PropertyInfo> _RoleMap = null;
        private Dictionary<int, string> _RoleNames = null;
        protected Dictionary<int, PropertyInfo> RoleMap
        {
            get
            {
                if (_RoleMap == null) {
                    var type = typeof(T);
                    _RoleMap = new() { { Roles.UserRole, null } };
                    _RoleNames = new() { { Roles.UserRole, "item" } };
                    if (!ValueConverter.IsConvertible(type)) {
                        int i = 0;
                        foreach (var prop in type.GetProperties()) {
                            ++i;
                            _RoleMap[Roles.UserRole + i] = prop;
                            _RoleNames[Roles.UserRole + i] = prop.Name
                                .ConvertCase(CaseStyle.Pascal, CaseStyle.Camel);
                        }
                    }
                }
                return _RoleMap;
            }
        }

        public sealed override Dictionary<int, string> RoleNames()
        {
            if (!RoleMap.Any())
                return null;
            return _RoleNames;
        }

        public sealed override object Data(ModelIndex index, int role)
        {
            if (!RoleMap.TryGetValue(role, out var property))
                return null;
            if (Data(index.Row) is not { } data)
                return null;
            if (property == null)
                return data;
            return property.GetValue(data);
        }

        public sealed override int RowCount(ModelIndex parent)
        {
            return ItemCount();
        }

        protected void BeginInsertItems(int first, int last)
        {
            BeginInsertRows(ModelIndex.Empty, first, last);
        }

        protected void EndInsertItems()
        {
            EndInsertRows();
        }
    }
}
