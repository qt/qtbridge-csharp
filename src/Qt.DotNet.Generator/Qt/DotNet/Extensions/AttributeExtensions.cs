/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.DotNet.Extensions
{
    using CodeGeneration;
    using Qt.Quick;
    using System.Linq;
    using static CodeGeneration.Rule;

    public static class AttributeExtensions
    {
        public static IEnumerable<CustomAttributeData> QtAttributeData<TAttrib>(this object self)
            where TAttrib : Attribute
        {
            var attribType = TypeOf<TAttrib>();
            return self.QtAttributeData()
                .Where(a => a.AttributeType.IsAssignableTo(attribType));
        }

        public static IEnumerable<CustomAttributeData> QtAttributeData(this object self)
        {
            var attribData = self switch
            {
                Assembly dll => dll.GetCustomAttributesData(),
                Type type when type.IsRootNode() => type.Assembly.GetCustomAttributesData(),
                Type type => type.GetCustomAttributesData(),
                MemberInfo memberInfo => memberInfo.GetCustomAttributesData(),
                _ => []
            };
            var adapterAssembly = TypeOf<Adapter>().Assembly;
            return attribData.Where(x => x.AttributeType.Assembly == adapterAssembly);
        }

        public static object Property(this CustomAttributeData self, string propertyName)
        {
            var namedArguments = self?.NamedArguments ?? new List<CustomAttributeNamedArgument>();
            if (namedArguments.All(arg => arg.MemberName != propertyName))
                throw new ArgumentException(nameof(propertyName));
            return namedArguments.FirstOrDefault(arg => arg.MemberName == propertyName)
                .TypedValue.Value;
        }
    }
}
