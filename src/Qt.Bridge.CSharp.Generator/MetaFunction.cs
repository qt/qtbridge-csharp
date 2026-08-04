// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.Diagnostics;
using Qt.Bridge.Utils.Collections.Concurrent;

namespace Qt.Bridge.CodeGeneration
{
    public abstract class MetaFunction : IPrioritizable<int>
    {
        protected abstract string Eval(object src, Enum traits);
        public virtual int Priority => 0;
        protected virtual string Sanitize(string evalResult) => evalResult;

        private ConcurrentSet<string> Exceptions { get; } = new();

        protected string Unsafe(string value)
        {
            Exceptions.Add(value);
            return value;
        }

        private static ConcurrentPriorityList<MetaFunction, int> MetaFunctions { get; } = new();

        public static void Register<T>(T mFn) where T : MetaFunction
        {
            MetaFunctions.Add(mFn);
        }
        public static void Register<T>() where T : MetaFunction, new() => Register(new T());

        private static ConcurrentDictionary<
            (object Source, Enum Traits), string> Cache { get; } = new();

        private enum DefaultTrait { Default = 0 }

        public static string Evaluate<T>(T src, Enum traits = null)
            where T : class
        {
            traits ??= DefaultTrait.Default;
            if (Cache.TryGetValue((src, traits), out var cachedValue))
                return cachedValue;
            foreach (var mFn in MetaFunctions) {
                if (mFn.Eval(src, traits) is not { } value)
                    continue;
                if (!mFn.Exceptions.Contains(value))
                    value = mFn.Sanitize(value);
                Cache.TryAdd((src, traits), value);
                return value;
            }
            Debug.Assert(false, $"No MetaFunction matched for '{src}' with traits '{traits}'");
            return src.ToString();
        }

        public static Type TypeOf(string name) => Rule.TypeOf(name);
        public static Type TypeOf(Type t) => Rule.TypeOf(t);
        public static Type TypeOf<T>() => Rule.TypeOf<T>();

        protected const char Wrap = Placeholder.Wrap;
    }

    public static class MetaFunctionExtensions
    {
        public static bool HasTraits<T>(this Enum self, T traits)
            where T : struct, Enum
        {
            if (self.GetType() == typeof(T))
                return ((T)self).HasFlag(traits);
            return ((T)Enum.ToObject(typeof(T), Convert.ToInt64(self))).HasFlag(traits);
        }

        public static bool TryRegisterAsMetaFunction(this Type mFnType)
        {
            if (!mFnType.IsAssignableTo(typeof(MetaFunction)))
                return false;
            if (!mFnType.IsClass || mFnType.IsAbstract)
                return false;
            if (Activator.CreateInstance(mFnType) is not MetaFunction mFn)
                return false;
            MetaFunction.Register(mFn);
            return true;
        }

        public static string MFn(this object self, Enum traits = null)
        {
            return MetaFunction.Evaluate(self, traits);
        }
    }
}
