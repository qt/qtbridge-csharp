/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Reflection;

namespace Qt.Bridge.Extensions
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
            var adapterAssembly = TypeOf<Qt.DotNet.Adapter>().Assembly;
            var apiAssembly = TypeOf<TypeCast>().Assembly;
            return attribData
                .Where(x => x.AttributeType.Assembly == adapterAssembly
                    || x.AttributeType.Assembly == apiAssembly);
        }

        public static bool HasProperty(this CustomAttributeData self, string name)
        {
            var namedArguments = self?.NamedArguments ?? new List<CustomAttributeNamedArgument>();
            return namedArguments.Any(arg => arg.MemberName == name);
        }

        public static object Property(this CustomAttributeData self, string name)
        {
            var namedArguments = self?.NamedArguments ?? new List<CustomAttributeNamedArgument>();
            if (!self.HasProperty(name))
                throw new ArgumentException($"Property '{name}' not found.", nameof(name));
            return namedArguments.FirstOrDefault(arg => arg.MemberName == name)
                .TypedValue.Value;
        }

        public static bool TryProperty(this CustomAttributeData self, string name, out object value)
        {
            value = null;
            if (!self.HasProperty(name))
                return false;
            value = self.Property(name);
            return true;
        }

        public static T Property<T>(this CustomAttributeData self, string name)
        {
            var namedArguments = self?.NamedArguments ?? new List<CustomAttributeNamedArgument>();
            if (!self.HasProperty(name))
                throw new ArgumentException($"Property '{name}' not found.", nameof(name));
            var arg = namedArguments.FirstOrDefault(arg => arg.MemberName == name).TypedValue;
            if (arg.ArgumentType != TypeOf<T>() || arg.Value is not T argValue)
                return default;
            return argValue;
        }

        public static bool TryProperty<T>(this CustomAttributeData self, string name, out T value)
        {
            value = default;
            if (!self.HasProperty(name))
                return false;
            value = self.Property<T>(name);
            return true;
        }
    }
}
