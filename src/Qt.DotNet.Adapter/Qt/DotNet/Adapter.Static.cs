// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

namespace Qt.DotNet
{
    public partial class Adapter
    {
        public partial interface IStatic
        {
        }

        private static IStatic _Static = null;
        private static readonly ManualResetEventSlim _Ready = new(false);
        public static IStatic Static
        {
            get
            {
                if (!_Ready.Wait(3000))
                    Environment.FailFast("Adapter failed to initialize within 3 seconds.");
                return Volatile.Read(ref _Static);
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (Interlocked.CompareExchange(ref _Static, value, null) == null)
                    _Ready.Set();
            }
        }
    }
}
