/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Qt.DotNet;
using Qt.Quick;

namespace Qt.Bridge.Extensions
{
    using CodeGeneration;
    using static CodeGeneration.Rule;

    public static class TypeExtensionsForGenerationRules
    {
        public static bool IsBuiltIn(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return type.IsPrimitive || type.Is<decimal>()
                || type.Is(typeof(void)) || type.Is<string>() || type.Is<object>()
                || type.Is<ModelIndex>() || type.Is<DateTime>() || type.Is<Uri>();
        }

        public static bool IsValue(this Type type)
        {
            return IsBuiltIn(type) || type.IsEnum;
        }

        public static bool IsObject(this Type type)
        {
            return type.Is<object>() || !type.IsValue();
        }

        public static string FormatName(this Type type, string separator,
            Func<string, string> formatPart = null, Func<string, string> afterFormat = null,
            Func<Type, string> nameOf = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            formatPart ??= x => x;
            afterFormat ??= x => x;
            nameOf ??= x =>
            {
                StringBuilder name = new();
                if (x.IsArray) {
                    var rank = x.GetArrayRank();
                    name.Append("Array");
                    if (rank > 1)
                        name.Append($"{rank}D");
                    name.Append("_");
                    x = x.GetElementType();
                }
                name.Append(x.Name.Split('`')[0]);
                return name.ToString();
            };
            Stack<Type> nestingTypes = new();
            while (type != null) {
                nestingTypes.Push(type);
                type = type.DeclaringType; ;
            }
            return afterFormat(string.Join(separator, nestingTypes
                .Select(x => formatPart(nameOf(x)))));
        }

        public static string FormatNamespace(this Type type, string separator,
            Func<string, string> formatPart = null, Func<string, string> afterFormat = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type.Namespace is not { Length: > 0 })
                return string.Empty;
            formatPart ??= x => x;
            afterFormat ??= x => x;
            return afterFormat(string.Join(separator, type.Namespace.Split('.')
                .Select(x => formatPart(x))));
        }

        public static bool Implements(this Type self, Type iface)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (iface == null)
                return false;
            return self.GetInterfaces().Contains(iface);
        }
        public static bool Implements(this Type self, string name) => self.Implements(TypeOf(name));
        public static bool Implements<T>(this Type self) => self.Implements(TypeOf<T>());

        public static bool IsDefaultConstructible(this Type self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            return self.GetConstructors()
                .Any(ctor => ctor.IsPublic && ctor.GetParameters().Length == 0);
        }

        public static bool IsQmlElement(this Type self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            if (self.IsGenericType)
                return false;
            if (!self.IsDefaultConstructible())
                return false;
            if (self.IsAssignableTo(TypeOf<EventArgs>()))
                return false;
            return self.Assembly == SourceGraph.Root.Assembly
                || self.QtAttributeData<QmlElementAttribute>().Any();
        }

        public static bool IsQmlSingleton(this Type self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            return self.QtAttributeData<QmlElementAttribute>()
                .Any(a => a.TryProperty(nameof(QmlElementAttribute.Singleton), out bool ok) && ok);
        }

        private static readonly Regex QmlRegex = new("^[A-Z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        public static string QmlElementName(this Type self)
        {
            if (self == null)
                throw new ArgumentNullException(nameof(self));
            return self.QtAttributeData<QmlElementAttribute>()
                .Select(a =>
                    a.TryProperty<string>(nameof(QmlElementAttribute.Name), out var n) ? n : null)
                .FirstOrDefault(name => name is { Length: > 0 } && QmlRegex.IsMatch(name));
        }

        public static bool IsList(this Type self, out Type itemType)
        {
            itemType = null;

            var genericLists = new[]
            {
                TypeOf("System.Collections.Generic.IReadOnlyList`1"),
                TypeOf("System.Collections.Generic.IList`1"),
            };

            var iListOf = self.GetInterfaces()
                .FirstOrDefault(t => t.IsGenericType
                    && genericLists.Contains(t.GetGenericTypeDefinition()));
            if (iListOf != null) {
                itemType = iListOf.GetGenericArguments()[0];
                return true;
            }

            if (self.Implements<IList>()) {
                itemType = TypeOf<object>();
                return true;
            }

            return false;
        }

        public static bool IsObservableList(this Type self, out Type itemType)
        {
            ArgumentNullException.ThrowIfNull(self);

            itemType = null;
            if (!self.IsList(out itemType))
                return false;

            if (self.ImplementsINotifyCollectionChanged())
                return true;

            try {
                var def = self.IsGenericTypeDefinition
                    ? self
                    : self.IsGenericType ? self.GetGenericTypeDefinition() : null;
                return def?.FullName is "System.Collections.ObjectModel.ObservableCollection`1"
                    or "System.Collections.ObjectModel.ReadOnlyObservableCollection`1";
            } catch { /* ignore */ }

            return false;
        }

        private static bool ImplementsINotifyCollectionChanged(this Type self)
        {
            ArgumentNullException.ThrowIfNull(self);

            const string ns = "System.Collections.Specialized";
            const string eventHandler = ns + ".NotifyCollectionChangedEventHandler";

            // Strongly-typed fast path, if available
            try {
                var type = TypeOf($"{ns}.INotifyCollectionChanged");
                if (type != null && type.IsAssignableFrom(self))
                    return true;
            } catch { /* ignore */ }

            // name-based fallback
            if (self.GetInterfaces().Any(i => i.FullName == $"{ns}.INotifyCollectionChanged"))
                return true;

            // event-signature fallback
            var ev = self.GetEvent("CollectionChanged");
            return ev != null && (ev.EventHandlerType?.FullName?.Contains(eventHandler) ?? false);
        }
    }
}
