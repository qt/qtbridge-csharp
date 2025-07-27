/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Qt.DotNet.Utils
{
    public class LazyFactory : INotifyPropertyChanged
    {
        private ConcurrentDictionary<(object, PropertyInfo), object> Objs { get; } = new();

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly object criticalSection = new();

        public T Get<T>(Expression<Func<T>> propertyRef, Func<T> initFunc = null)
        {
            if (propertyRef?.Body is not MemberExpression lazyPropertyExpr)
                throw new ArgumentException("Expected member lambda", nameof(propertyRef));
            if (lazyPropertyExpr?.Member is not PropertyInfo lazyProperty)
                throw new ArgumentException("Invalid property reference", nameof(propertyRef));
            object owner = lazyProperty.DeclaringType;
            if (lazyPropertyExpr.Expression is ConstantExpression { Value: not null } lazyThis)
                owner = lazyThis.Value;
            lock (criticalSection) {
                if (initFunc != null)
                    return (T)Objs.GetOrAdd((owner, lazyProperty), k => initFunc());
                else
                    return (T)Objs.GetOrAdd((owner, lazyProperty), k => default(T));
            }
        }

        public void Set<T>(Expression<Func<T>> propertyRef, T value)
        {
            if (propertyRef?.Body is not MemberExpression lazyPropertyExpr)
                throw new ArgumentException("Expected member lambda", nameof(propertyRef));
            if (lazyPropertyExpr?.Member is not PropertyInfo lazyProperty)
                throw new ArgumentException("Invalid property reference", nameof(propertyRef));
            object owner = lazyProperty.DeclaringType;
            if (lazyPropertyExpr.Expression is ConstantExpression { Value: not null } lazyThis)
                owner = lazyThis.Value;
            if (Objs.TryGetValue((owner, lazyProperty), out var current) && current.Equals(value))
                return;
            Objs[(owner, lazyProperty)] = value;
            PropertyChanged?.Invoke(owner, new PropertyChangedEventArgs(lazyProperty.Name));
        }
    }
}
