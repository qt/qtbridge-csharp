// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.DotNet
{
    public partial class Adapter
    {
        /// <summary>
        /// Get current ref counters. For debug/test purposes.
        /// </summary>
        /// <param name="objectCount">Object ref. count</param>
        /// <param name="delegateCount">Static method ref. count</param>
        /// <param name="eventCount">Event ref. count</param>
        public static void Stats(out int objectCount, out int delegateCount, out int eventCount)
        {
            objectCount = ObjectRefs.Count;
            delegateCount = DelegateRefs.Count;
            eventCount = Events.Count;
        }

        internal static MemberInfo GetMember(IntPtr funcPtr)
        {
            var members = DelegateRefs
                .Where(x => x.Value.Ref.FuncPtr == funcPtr)
                .Select(x => x.Value.Member);
            return members.First();

        }
    }
}
