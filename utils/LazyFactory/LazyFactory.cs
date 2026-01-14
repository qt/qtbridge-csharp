// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Qt.Bridge.Utils
{
    public sealed class LazyFactory : INotifyPropertyChanged
    {
        private readonly object criticalSection = new();
        private ConcurrentDictionary<(object, PropertyInfo), object> ObjectsCache { get; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public T Get<T>(Expression<Func<T>> propertyRef, Func<T> initFunc = null)
        {
            var (expression, propertyInfo) = ValidateAndDecompose(propertyRef);
            // decide if we're in the fallback path
            var isFallback = ExpressionContainsMethodCall(expression);
            // figure out the actual 'owner' object
            var owner = ExtractOwner(expression, propertyInfo.DeclaringType);

            lock (criticalSection) {
                // 1) Create the key
                var key = (owner, propertyInfo);

                // 2) Return any cached value
                if (ObjectsCache.TryGetValue(key, out var cached))
                    return (T)cached;

                // 3) In fallback mode, try reading the real CLR property first
                if (isFallback) {
                    var realValue = (T)propertyInfo.GetValue(owner);
                    if (!EqualityComparer<T>.Default.Equals(realValue, default)) {
                        ObjectsCache[key] = realValue;
                        return realValue;
                    }
                }

                // 4) If there's no initFunc, just return default(T), don't cache it
                if (initFunc == null)
                    return default;

                // 5) Otherwise, run the initFunc exactly once, cache & return it
                var initVal = initFunc();
                ObjectsCache[key] = initVal;
                return initVal;
            }
        }

        public void Set<T>(Expression<Func<T>> propertyRef, T value)
        {
            var (expression, propertyInfo) = ValidateAndDecompose(propertyRef);
            var owner = ExtractOwner(expression, propertyInfo.DeclaringType);
            ObjectsCache[(owner, propertyInfo)] = value;
            PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(propertyInfo.Name));
        }

        private static (Expression, PropertyInfo) ValidateAndDecompose<T>(Expression<Func<T>> exp)
        {
            if (exp?.Body is not MemberExpression memberExpression)
                throw new ArgumentException("Expected member lambda", nameof(exp));
            if (memberExpression.Member is not PropertyInfo propertyInfo)
                throw new ArgumentException("Invalid property reference", nameof(exp));

            return (memberExpression.Expression, propertyInfo);
        }

        // Determine whether the given expression tree contains any method-call nodes. Treat
        // any property-access expression that involves a method call as a "fallback" scenario:
        // before doing pure lazy init, we first pull the 'live' CLR property value off the
        // object via reflection, because the method call may be computing or returning a different
        // instance each time.
        private static bool ExpressionContainsMethodCall(Expression expression)
        {
            if (expression == null)
                return false;
            if (expression.NodeType == ExpressionType.Call)
                return true;

            return expression switch
            {
                MemberExpression me => ExpressionContainsMethodCall(me.Expression),
                UnaryExpression ue => ExpressionContainsMethodCall(ue.Operand),
                _ => false
            };
        }

        private static object ExtractOwner(Expression expression, Type type)
        {
            return RecursiveExtractOwner(expression, type);
        }

        // Recursively evaluates an expression to determine the actual instance whose property
        // is being accessed. This method is crucial for converting an expression tree into a
        // concrete object instance or Type. It ensures that different instances of the same class
        // do not collide in the cache and handles static and instance properties uniformly. For
        // static properties, it returns the declaring type. Otherwise, the return value depends
        // on the type of the expression:
        //
        // 1) If the expression is a 'ConstantExpression', return its Value.
        // 2) If the expression is a 'MemberExpression', recursively evaluate its Expression and
        //    reads the field or property via reflection.
        // 3) For any other type of expression, compiles the expression to a delegate and invokes
        //    it to handle more complex capture scenarios.
        private static object RecursiveExtractOwner(Expression expression, Type declaringType)
        {
            if (expression == null)
                return declaringType; // static property -> key on Type object

            switch (expression) {
            case ConstantExpression ce:
                if (ce.Value == null) {
                    throw new ArgumentException("Cannot extract instance from null constant",
                        nameof(expression));
                }
                return ce.Value;

            case MemberExpression me:
                var parent = RecursiveExtractOwner(me.Expression, declaringType);
                return me.Member switch
                {
                    FieldInfo fi => fi.GetValue(parent),
                    PropertyInfo pi => pi.GetValue(parent),
                    _ => throw new InvalidOperationException("Unsupported member type: "
                        + $"{me.Member.GetType().Name}")
                };

            default:
                // Fallback: compile & invoke to handle more captures
                var lambda = Expression.Lambda(expression);
                var fn = lambda.Compile();
                return fn.DynamicInvoke();
            }
        }
    }
}
