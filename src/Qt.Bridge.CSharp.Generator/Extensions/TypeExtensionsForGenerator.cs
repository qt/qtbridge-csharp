// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.Bridge.CodeGeneration.Extensions
{
    public static class TypeExtensionsForGenerator
    {
        public static IEnumerable<Type> DelegateSignature(this Type type)
        {
            if (type.GetMethod("Invoke") is not { } invoke)
                return [];
            return invoke.GetParameters()
                .Select(x => x.ParameterType)
                .Prepend(invoke.ReturnType);
        }

        public static bool IsDerivedFrom(this Type type, Type baseType)
        {
            if (type.IsAssignableTo(baseType))
                return true;
            if (!baseType.IsGenericTypeDefinition)
                return false;
            var tested = new HashSet<Type>();
            var tests = new Queue<Type>([type]);
            while (tests.TryDequeue(out var test)) {
                if (tested.Contains(test))
                    continue;
                tested.Add(test);
                if (test.BaseType != null && !tested.Contains(test.BaseType))
                    tests.Enqueue(test.BaseType);
                foreach (var iface in test.GetInterfaces().Where(t => !tested.Contains(t)))
                    tests.Enqueue(iface);
                if (!test.IsConstructedGenericType)
                    continue;
                if (test.GenericTypeArguments.Length != baseType.GetGenericArguments().Length)
                    continue;
                if (baseType.MakeGenericType(test.GenericTypeArguments).IsAssignableFrom(test))
                    return true;
            }
            return false;
        }

        public static bool IsNestedIn(this Type type, Type outerType)
        {
            while (type.DeclaringType != null) {
                if (type.DeclaringType == outerType)
                    return true;
                type = type.DeclaringType;
            }
            return false;
        }

        public static bool IsInteger(this Type type)
        {
            return Type.GetTypeCode(type)
                is TypeCode.SByte or TypeCode.Byte
                or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32
                or TypeCode.Int64 or TypeCode.UInt64;
        }

        private const Options DefaultExportOptions = ExportAs.SourceCode;
        private const string OptionsPropertyName = nameof(ExportAttribute.Options);


        /// <summary>
        /// Type export options
        /// </summary>
        /// <returns>Bitmask of <see cref="Options"/> flags</returns>
        public static Options ExportOptions(this Type type)
        {

            var options = type.QtAttributeData<ExportAttribute>()
                .Concat(type.Assembly.QtAttributeData<ExportAttribute>())
                .Where(opt => opt.HasProperty(OptionsPropertyName))
                .Select(opt => opt.TryProperty(OptionsPropertyName, out Options x) ? x : default)
                .FirstOrDefault();
            return options == default ? DefaultExportOptions : options;
        }

        /// <summary>
        /// Check if type should be exported as metadata
        /// </summary>
        /// <remarks>
        /// This function only checks the export options attribute, if any. It does not take into
        /// consideration any type selection attributes.
        /// </remarks>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="bool">true</see> if the type should be exported as metadata;</item>
        /// <item><see cref="bool">false</see> otherwise.</item>
        /// </list>
        /// </returns>
        public static bool ExportAsMetadata(this Type type)
        {
            var options = ExportOptions(type);
            return options.HasFlag(Options.Export) && !options.HasFlag(Options.ExportAsSourceCode);
        }

        /// <summary>
        /// Check if type should be exported as source code
        /// </summary>
        /// <remarks>
        /// This function only checks the export options attribute, if any. It does not take into
        /// consideration any type selection attributes.
        /// </remarks>
        /// <returns>
        /// <list type="bullet">
        /// <item><see cref="bool">true</see> if the type should be exported as source code;</item>
        /// <item><see cref="bool">false</see> otherwise.</item>
        /// </list>
        /// </returns>
        public static bool ExportAsSourceCode(this Type type)
        {
            var options = ExportOptions(type);
            return options.HasFlag(Options.Export) && options.HasFlag(Options.ExportAsSourceCode);
        }
    }
}
