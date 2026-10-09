// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        internal static IntPtr GetRefPtrToObject(object obj, bool weakRef = false)
        {
            var objHandle = GCHandle.Alloc(obj, weakRef ? GCHandleType.Weak : GCHandleType.Normal);
            var objRefPtr = GCHandle.ToIntPtr(objHandle);
            ObjectRefs.TryAdd(objRefPtr, new ObjectRef(objHandle));
            if (obj != null) {
                lock (IndexLock)
                    ++RefCounts.GetValue(obj, _ => new StrongBox<int>()).Value;
            }
            return objRefPtr;
        }

        // Drops one reference to the target. Returns 'true' if that was the last one.
        private static bool ReleaseTarget(object target)
        {
            lock (IndexLock) {
                if (!RefCounts.TryGetValue(target, out var count))
                    return true;
                if (--count.Value > 0)
                    return false;
                RefCounts.Remove(target);
                return true;
            }
        }

        // Frees all delegates bound to the target, such as its instance methods.
        private static void FreeDelegatesOf(object target)
        {
            HashSet<DelegateKey> keys;
            lock (IndexLock) {
                if (!DelegateKeysByTarget.Remove(target, out keys))
                    return;
            }
            foreach (var key in keys) {
                if (DelegatesByMember.TryGetValue(key, out var delegateRef))
                    FreeDelegateRef(delegateRef.FuncPtr);
            }
        }

        internal static ObjectRef GetObjectRefFromPtr(IntPtr objRefPtr)
        {
            if (objRefPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(objRefPtr));
            if (!ObjectRefs.TryGetValue(objRefPtr, out var objRef))
                return null;
            if (!objRef.IsValid)
                throw new ObjectDisposedException(nameof(objRefPtr));
            return objRef;
        }

        /// <summary>
        /// Create a new reference (GCHandle) to the given object
        /// </summary>
        /// <param name="objRefPtr">Native reference to target object.</param>
        /// <param name="weakRef">'true' to create a weak ref.; 'false' otherwise (default)</param>
        /// <returns>Native object reference</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IntPtr AddObjectRef(IntPtr objRefPtr, bool weakRef = false)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.AddObjectRef(AddObjectRef);
#endif
            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));
            return GetRefPtrToObject(objRef.Target, weakRef);
        }

        /// <summary>
        /// Release object reference, as well as any associated instance method and event references
        /// </summary>
        /// <param name="objRefPtr">Native reference to target object.</param>
        /// <returns>'true' if object ref. was released successfully; 'false' otherwise</returns>
        /// <exception cref="ArgumentException"></exception>
        public static void FreeObjectRef(IntPtr objRefPtr)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.FreeObjectRef(FreeObjectRef);
#endif
            if (!ObjectRefs.TryRemove(objRefPtr, out var objRef)) {
                Debug.WriteLine(@$"
ADAPTER::FreeObjectRef: WARNING Invalid object reference: 0x{objRefPtr:x16}");
                return;
            }
            RemoveAllEventHandlers(objRef);

            // Delegates bound to the target stay valid until its last reference is freed.
            if (objRef.Target is { } target && ReleaseTarget(target))
                FreeDelegatesOf(target);

            objRef.Handle.Free();
        }

        /// <summary>
        /// Release reference to a type (i.e. instance of System.Type)
        /// </summary>
        /// <param name="typeName">Fully qualified name of type</param>
        /// <exception cref="ArgumentException"></exception>
        public static void FreeTypeRef(string typeName)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.FreeTypeRef(FreeTypeRef);
#endif
            var type = Type.GetType(typeName)
                ?? throw new ArgumentException($"Type '{typeName}' not found", nameof(typeName));

            var typeRefs = ObjectRefs
                .Where(x => ReferenceEquals(x.Value.Target, type))
                .ToList();
            foreach (var typeRef in typeRefs)
                FreeObjectRef(typeRef.Key);

            FreeDelegatesOf(type);
        }

        public static void FreeDelegateRef(IntPtr delRefPtr)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.FreeDelegateRef(FreeDelegateRef);
#endif
            if (SafeMethodsBySafePtr.TryRemove(delRefPtr, out var safeDelegate)) {
                SafeMethodsByUnsafePtr.TryRemove(safeDelegate.UnsafeFuncPtr, out _);
                safeDelegate.Ref.Handle.Free();
                return;
            }
            if (!DelegateRefs.TryRemove(delRefPtr, out var delegateRef))
                return;

            var key = new DelegateKey(delegateRef.Target, delegateRef.Member, delegateRef.Access);
            DelegatesByMember.TryRemove(KeyValuePair.Create(key, delegateRef.Ref));
            lock (IndexLock) {
                if (DelegateKeysByTarget.TryGetValue(delegateRef.Target, out var keys)) {
                    if (keys.Remove(key) && keys.Count == 0)
                        DelegateKeysByTarget.Remove(delegateRef.Target);
                }
            }
            delegateRef.Ref.Handle.Free();

            if (!SafeMethodsByUnsafePtr.TryRemove(delRefPtr, out var safeDelegateRef))
                return;
            SafeMethodsBySafePtr.TryRemove(safeDelegateRef.FuncPtr, out _);
            safeDelegateRef.Handle.Free();
        }

        public static string GetStableAssemblyQualifiedName(IntPtr typeRefPtr)
        {
#if DEBUG
            _ = new Delegates.GetStableAssemblyQualifiedName(GetStableAssemblyQualifiedName);
#endif
            var objRef = GetObjectRefFromPtr(typeRefPtr);
            if (objRef?.Target is not Type type)
                throw new ArgumentException("Invalid type reference", nameof(typeRefPtr));
            return StableTypeNameBuilder.Build(type);
        }

        public static IntPtr GetObject(IntPtr objRefPtr, string path)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.GetObject(GetObject);
#endif
            var memberNames = path.Split('.',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));

            var obj = objRef.Target;
            foreach (var memberName in memberNames) {
                if (obj?.GetType()?.GetMember(memberName) is not MemberInfo[] members)
                    throw new ArgumentException("Invalid object reference", memberName);
                foreach (var member in members) {
                    if (member is PropertyInfo prop) {
                        obj = prop.GetValue(obj);
                        break;
                    } else if (member is FieldInfo field) {
                        obj = field.GetValue(obj);
                        break;
                    }
                    throw new ArgumentException("Invalid object reference", memberName);
                }
            }
            return GetRefPtrToObject(obj);
        }
    }
}
