// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Threading.Channels;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerTransportPipe
    {
        private sealed class InjectedMessageQueue
        {
            private readonly TaskCompletionSource<bool> serverInitializedSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly Channel<byte[]> notifications =
                Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });

            public bool IsServerInitialized => serverInitializedSource.Task.IsCompleted;

            public void EnqueueNotification(byte[] message)
            {
                notifications.Writer.TryWrite(message);
            }

            public void MarkServerInitialized() => serverInitializedSource.TrySetResult(true);

            public ValueTask<bool> WaitToReadNotificationsAsync(CancellationToken ct) =>
                notifications.Reader.WaitToReadAsync(ct);

            public bool TryReadNotification(out byte[] message) =>
                notifications.Reader.TryRead(out message!);

            public void Complete() => notifications.Writer.TryComplete();
        }
    }
}
