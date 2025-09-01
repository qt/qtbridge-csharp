/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Qt.DotNet.CodeGeneration
{
    using Extensions;
    using Utils;
    using Utils.Collections.Concurrent;

    public class DependencyGraph : IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>
    {
        public const string RootName = "<Module>";

        private LazyFactory lazy = new();
        private MetadataLoadContext loader;

        private DependencyGraph(MetadataLoadContext loader)
        {
            this.loader = loader;
        }

        public static async Task CreateAsync(
            MetadataLoadContext loader, Assembly source, IEnumerable<Type> excludedTypes)
        {
            Rules.SourceGraph = new DependencyGraph(loader);
            foreach (var excludedType in excludedTypes)
                Rules.SourceGraph.ExcludedTypes.Add(excludedType);
            if (!await Rules.SourceGraph.BuildAsync(source))
                Rules.SourceGraph = null;
        }

        private ConcurrentDictionary<Type, ConcurrentSet<MemberInfo>> Nodes { get; } = new();
        private ConcurrentDictionary<Type, ConcurrentSet<Type>> Edges { get; } = new();
        public Type Root { get; private set; }

        public IEnumerable<Type> Connected(Type fromType)
        {
            return Edges.TryGetValue(fromType, out var toNodes) ? toNodes : Array.Empty<Type>();
        }

        private Assembly GetAssembly(string name) => loader.LoadFromAssemblyName(name);

        private Assembly AdapterAssembly => lazy.Get(() => AdapterAssembly, ()
            => GetAssembly("Qt.DotNet.Adapter"));

        public Type TypeOf(string name) => loader.CoreAssembly.GetType(name);
        public Type TypeOf(Type t) => TypeOf($"{t.FullName}, {t.Assembly.GetName().Name}");
        public Type TypeOf<T>() => TypeOf(typeof(T));

        private Type TypeOfDelegate => lazy.Get(() => TypeOfDelegate, ()
            => TypeOf<Delegate>());
        private Type TypeOfTask => lazy.Get(() => TypeOfTask, ()
            => TypeOf<Task>());
        private Type TypeOfIEquatable => lazy.Get(() => TypeOfIEquatable, ()
            => TypeOf("System.IEquatable`1"));
        private Type AttribAsync => lazy.Get(() => AttribAsync, ()
            => TypeOf<AsyncStateMachineAttribute>());
        private Type AttribCompilerGenerated => lazy.Get(() => AttribCompilerGenerated, ()
            => TypeOf<CompilerGeneratedAttribute>());
        private Type AttribExclude => lazy.Get(() => AttribExclude, ()
            => TypeOf<Qt.ExcludeAttribute>());
        private Type AttribIgnore => lazy.Get(() => AttribIgnore, ()
            => TypeOf<Qt.IgnoreAttribute>());

        public ConcurrentSet<Type> ExcludedTypes => lazy.Get(() => ExcludedTypes, () => new()
        {
            TypeOf<Array>(),
            TypeOf<Type>(),
            TypeOf<Task>(),
            TypeOf<IDeserializationCallback>(),
            TypeOf<IFormattable>(),
            TypeOf<ISerializable>(),
            TypeOf<ISpanFormattable>(),
            TypeOf<SerializationInfo>(),
            TypeOf<StreamingContext>(),
            TypeOf<Delegate>(),
        });
        public ConcurrentSet<Type> ExcludedBaseTypes { get; } = new();

        public ConcurrentSet<Type> BuiltInTypes => lazy.Get(() => BuiltInTypes, () => new()
        {
            TypeOf<DateTime>(),
            TypeOf<decimal>(),
            TypeOf<Enum>(),
            TypeOf<EventArgs>(),
            TypeOf<IComparable>(),
            TypeOf<IConvertible>(),
            TypeOf<IDisposable>(),
            TypeOf<INotifyPropertyChanged>(),
            TypeOf<string>(),
            TypeOf<ValueType>(),
            TypeOf(typeof(void))
        });

        private bool IsConstructedTypeOfGenericType(Type type, Type genericType)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == genericType;
        }

        private bool IsBuiltIn(Type type)
        {
            if (type.QtAttributeData<IncludeAttribute>().Any())
                return false;
            if (IsConstructedTypeOfGenericType(type, TypeOfIEquatable))
                return true;
            if (BuiltInTypes.Contains(type))
                return true;
            if (type.IsPrimitive)
                return true;
            if (type.Assembly == AdapterAssembly)
                return true;
            return false;
        }

        private bool IsSame(Type type, Type baseType)
        {
            if (type == baseType)
                return true;
            if (!type.IsConstructedGenericType || !baseType.IsGenericTypeDefinition)
                return false;
            return type.GetGenericTypeDefinition() == baseType;
        }

        private bool IsDerived(Type type, Type baseType)
        {
            if (type.IsAssignableTo(baseType))
                return true;
            if (!type.IsConstructedGenericType || !baseType.IsGenericTypeDefinition)
                return false;
            if (type.GenericTypeArguments.Length != baseType.GetGenericArguments().Length)
                return false;
            if (!baseType.MakeGenericType(type.GenericTypeArguments).IsAssignableFrom(type))
                return false;
            return true;
        }

        private bool IsExcluded(Type type)
        {
            if (type.QtAttributeData<IncludeAttribute>().Any())
                return false;
            if (type.IsGenericTypeDefinition
                || type.IsGenericParameter
                || type.IsGenericTypeParameter
                || type.IsGenericMethodParameter
                || type.ContainsGenericParameters) {
                return true;
            }
            if (type == TypeOfTask)
                return true;
            if (type.IsByRef || type.IsByRefLike || type.IsPointer)
                return true;
            if (ExcludedTypes.Any(x => IsSame(type, x)))
                return true;
            if (ExcludedBaseTypes.Any(x => IsDerived(type, x)))
                return true;
            if (IsIgnored(type))
                return true;
            if (type.IsAssignableTo(TypeOfDelegate))
                return true;
            return false;
        }

        private bool IsIgnored(Type type)
            => IsIgnored(type?.GetCustomAttributesData());

        private bool IsIgnored(MemberInfo info)
            => IsIgnored(info?.GetCustomAttributesData());

        private bool IsIgnored(IEnumerable<CustomAttributeData> attribs)
        {
            if (attribs == null)
                return false;
            if (!attribs.Any(x => x.AttributeType == AttribIgnore))
                return false;
            return true;
        }

        private bool IsValidMember(MemberInfo i)
        {
            if (i.QtAttributeData<IncludeAttribute>().Any())
                return true;
            if (IsExcluded(i.ReflectedType))
                return false;
            if (IsIgnored(i))
                return false;
            if (i.DeclaringType?.Assembly == AdapterAssembly)
                return false;
            if (i.IsOverrideOf(AdapterAssembly))
                return false;
            return true;
        }

        private async Task<bool> BuildAsync(Assembly assembly)
        {
            if ((Root = assembly.GetRootNode()) == null)
                return false;
            Nodes[Root] = new();

            foreach (var attrib in assembly.GetCustomAttributesData()) {
                if (attrib.AttributeType != AttribExclude)
                    continue;
                bool inherited = false;
                foreach (var namedArg in attrib.NamedArguments) {
                    if (namedArg.MemberName != "Inherited")
                        continue;
                    if (namedArg.TypedValue.ArgumentType != TypeOf<bool>())
                        continue;
                    if (namedArg.TypedValue.Value is not bool isInherited)
                        continue;
                    if (!isInherited)
                        continue;
                    inherited = true;
                    break;
                }
                foreach (var arg in attrib.ConstructorArguments) {
                    if (arg.Value is not IEnumerable<CustomAttributeTypedArgument> ignoreTypes)
                        continue;
                    foreach (var ignoreTypeData in ignoreTypes) {
                        var ignoreType = ignoreTypeData.Value switch
                        {
                            Type type => type,
                            string typeName => TypeOf(typeName),
                            _ => null
                        };
                        if (ignoreType == null)
                            continue;
                        if (inherited)
                            ExcludedBaseTypes.Add(ignoreType);
                        else
                            ExcludedTypes.Add(ignoreType);
                    }
                }
            }

            await Task.WhenAll(
                assembly.ExportedTypes
                .Where(x => x.DeclaringType == null)
                .Select(x => Task.Run(async () => await AddEdgeAsync(Root, x))));

            if (!Edges.Any())
                await AddEdgeAsync(Root, TypeOf<object>());

            return Nodes.Any();
        }

        private async Task<bool> AddEdgeAsync(Type fromType, Type type)
        {
            if (fromType == type)
                return true;

            if (IsExcluded(type))
                return false;

            if (IsBuiltIn(type))
                return true;

            if (!Nodes.ContainsKey(type) && !await AddTypeAsync(type))
                return false;

            Edges.TryAdd(fromType, new());
            Edges[fromType].Add(type);
            return true;
        }

        private async Task<bool> AddTypeAsync(Type type)
        {
            if (IsBuiltIn(type))
                return true;

            if (IsExcluded(type))
                return false;

            var typeMembers = new ConcurrentSet<MemberInfo>();
            if (!Nodes.TryAdd(type, typeMembers))
                return true;

            if (type.IsAssignableTo(TypeOfDelegate)) {
                await Task.WhenAll(type.DelegateSignature()
                    .Select(x => Task.Run(async () => await AddEdgeAsync(type, x))));
                return true;
            }

            if (type.IsEnum)
                return true;

            var members = await Task.WhenAll(
                type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(x => IsValidMember(x))
                .Select(x => Task.Run(async () => await (x switch
                {
                    ConstructorInfo y => AddConstructorAsync(y),
                    EventInfo y => AddEventAsync(y),
                    FieldInfo y => AddFieldAsync(y),
                    MethodInfo y => AddMethodAsync(y),
                    PropertyInfo y => AddPropertyAsync(y),
                    _ => Task.FromResult(false)
                }) ? x : null)));
            foreach (var member in members.Where(x => x != null))
                typeMembers.Add(member);
            return true;
        }

        private bool HasAttributes(MemberInfo info, params Type[] attribs)
        {
            if (info.GetCustomAttributesData() is not { Count: > 0 } infoAttribs)
                return false;
            return infoAttribs.Any(x => attribs.Contains(x.AttributeType));
        }

        private bool IsValidMethod(MethodBase info)
        {
            return !HasAttributes(info, AttribAsync, AttribCompilerGenerated)
                && !info.ContainsGenericParameters
                && !info.IsGenericMethod
                && !info.IsGenericMethodDefinition
                && (info.IsConstructor || !info.IsSpecialName);
        }

        private async Task<bool> AddConstructorAsync(ConstructorInfo info)
        {
            if (!IsValidMethod(info))
                return false;
            var result = await Task.WhenAll(
                info.GetParameters()
                .Select(x => Task.Run(async () => await AddParameterAsync(x, info.ReflectedType))));
            return !result.Contains(false);
        }

        private async Task<bool> AddMethodAsync(MethodInfo info, Type reflectedType = null)
        {
            reflectedType ??= info.ReflectedType;
            if (!IsValidMethod(info))
                return false;
            var result = await Task.WhenAll(
                info.GetParameters()
                .Select(x => Task.Run(async () => await AddParameterAsync(x, reflectedType)))
                .Append(Task.Run(async () => await AddEdgeAsync(reflectedType, info.ReturnType))));
            return !result.Contains(false);
        }

        private async Task<bool> AddParameterAsync(ParameterInfo info, Type reflectedType)
        {
            if (info.IsOut || info.ParameterType.IsByRef)
                return false;
            return await AddEdgeAsync(reflectedType, info.ParameterType);
        }

        private async Task<bool> AddEventAsync(EventInfo info)
        {
            var handler = info.EventHandlerType.GetMethod("Invoke");
            if (handler == null)
                return false;
            return await AddMethodAsync(handler, info.ReflectedType);
        }

        private async Task<bool> AddPropertyAsync(PropertyInfo info)
        {
            return await AddEdgeAsync(info.ReflectedType, info.PropertyType);
        }

        private async Task<bool> AddFieldAsync(FieldInfo info)
        {
            if (info.IsSpecialName)
                return false;
            return await AddEdgeAsync(info.ReflectedType, info.FieldType);
        }

        public IEnumerable<MemberInfo> NodeSet() => Nodes
            .SelectMany(n => n.Value.Prepend(n.Key)).Distinct();

        public IEnumerable<T> NodeSet<T>() => NodeSet().Where(n => n is T).Cast<T>();

        #region IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>
        public bool ContainsKey(Type t)
            => ((IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>)Nodes).ContainsKey(t);

        public bool TryGetValue(Type t, [MaybeNullWhen(false)] out ConcurrentSet<MemberInfo> mi)
            => ((IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>)Nodes).TryGetValue(t, out mi);

        public IEnumerator<KeyValuePair<Type, ConcurrentSet<MemberInfo>>> GetEnumerator()
            => ((IEnumerable<KeyValuePair<Type, ConcurrentSet<MemberInfo>>>)Nodes).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable)Nodes).GetEnumerator();

        public IEnumerable<Type> Keys
            => ((IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>)Nodes).Keys;

        public IEnumerable<ConcurrentSet<MemberInfo>> Values
            => ((IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>)Nodes).Values;

        public int Count
            => ((IReadOnlyCollection<KeyValuePair<Type, ConcurrentSet<MemberInfo>>>)Nodes).Count;

        public ConcurrentSet<MemberInfo> this[Type key]
            => ((IReadOnlyDictionary<Type, ConcurrentSet<MemberInfo>>)Nodes)[key];
        #endregion
    }

    public static class DependencyGraphNode
    {
        public static Type GetRootNode(this Assembly assembly) => assembly.GetType(DependencyGraph.RootName);
        public static bool IsRootNode(this MemberInfo node) => node.Name == DependencyGraph.RootName;
    }
}
