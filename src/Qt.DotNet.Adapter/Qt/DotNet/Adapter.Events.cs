// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.DotNet
{
    public partial class Adapter
    {
        /// <summary>
        /// Subscribe to notifications of given event from given object
        /// </summary>
        /// <param name="objRefPtr">Native reference to target object.</param>
        /// <param name="eventName">Name of event</param>
        /// <param name="context">Opaque pointer to context data</param>
        /// <param name="eventHandler">Pointer to callback function</param>
        /// <exception cref="ArgumentException"></exception>
        public static void AddEventHandler(
            IntPtr objRefPtr,
            string eventName,
            IntPtr context,
            Delegates.NativeEventHandler eventHandler)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.AddEventHandler(AddEventHandler);
#endif
            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));
            var eventInfo = objRef.Target.GetType().GetEvent(eventName)
                ?? throw new ArgumentException($"Event '{eventName}' not found", nameof(eventName));

            var key = new EventKey(objRef, eventName, context);
            if (Events.TryGetValue(key, out var eventRelay)) {
                eventRelay.Handler = eventHandler;
            } else {
                eventRelay = new EventRelay(objRef.Target, eventInfo, context, eventHandler);
                Events.TryAdd(key, eventRelay);
                lock (IndexLock) {
                    AddEventKey(EventKeysBySource, objRef, key);
                    AddEventKey(EventKeysByContext, context, key);
                }
            }
        }

        /// <summary>
        /// Unsubscribe to notifications of given event from given object
        /// </summary>
        /// <param name="objRefPtr">Native reference to target object.</param>
        /// <param name="eventName">Name of event</param>
        /// <param name="context">Opaque pointer to context data</param>
        /// <exception cref="ArgumentException"></exception>
        public static void RemoveEventHandler(
            IntPtr objRefPtr,
            string eventName,
            IntPtr context)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.RemoveEventHandler(RemoveEventHandler);
#endif
            var objRef = GetObjectRefFromPtr(objRefPtr);
            if (objRef == null)
                throw new ArgumentException("Invalid object reference", nameof(objRefPtr));
            RemoveEventHandler(objRef, eventName, context);
        }

        private static void RemoveEventHandler(
            ObjectRef objRef,
            string eventName,
            IntPtr context)
        {
            var key = new EventKey(objRef, eventName, context);
            if (!Events.TryRemove(key, out var evRelay))
                return;
            evRelay.Enabled = false;
            lock (IndexLock) {
                RemoveEventKey(EventKeysBySource, objRef, key);
                RemoveEventKey(EventKeysByContext, context, key);
            }
        }

        private static void AddEventKey<TIndex>(
            Dictionary<TIndex, HashSet<EventKey>> index, TIndex indexKey, EventKey key)
        {
            if (!index.TryGetValue(indexKey, out var keys))
                index[indexKey] = keys = [];
            keys.Add(key);
        }

        private static void RemoveEventKey<TIndex>(
            Dictionary<TIndex, HashSet<EventKey>> index, TIndex indexKey, EventKey key)
        {
            if (index.TryGetValue(indexKey, out var keys) && keys.Remove(key) && keys.Count == 0)
                index.Remove(indexKey);
        }

        private static List<EventKey> EventKeysOf<TIndex>(
            Dictionary<TIndex, HashSet<EventKey>> index, TIndex indexKey)
        {
            lock (IndexLock)
                return index.TryGetValue(indexKey, out var keys) ? [.. keys] : [];
        }


        /// <summary>
        /// Unsubscribe to notifications of all events for a given context object
        /// </summary>
        /// <param name="context">Native context reference.</param>
        public static void RemoveAllEventHandlersByContext(IntPtr context)
        {
#if DEBUG
            // Compile-time signature check of delegate vs. method
            _ = new Delegates.RemoveAllEventHandlersByContext(RemoveAllEventHandlersByContext);
#endif
            foreach (var key in EventKeysOf(EventKeysByContext, context))
                RemoveEventHandler(key.Source, key.Name, key.Context);
        }

        private static void RemoveAllEventHandlers(ObjectRef objRef)
        {
            foreach (var key in EventKeysOf(EventKeysBySource, objRef))
                RemoveEventHandler(key.Source, key.Name, key.Context);
        }
    }
}
