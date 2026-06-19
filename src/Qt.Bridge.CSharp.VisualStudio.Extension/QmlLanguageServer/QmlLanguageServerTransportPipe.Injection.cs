// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Text;
using System.Threading.Channels;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.QmlLanguageServer
{
    internal sealed partial class QmlLanguageServerTransportPipe
    {
        private sealed class InjectedMessageQueue
        {
            private readonly TaskCompletionSource<bool> serverInitializedSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            private readonly Channel<byte[]> notifications = CreateChannel();
            private readonly Channel<byte[]> serverRequests = CreateChannel();
            private readonly object requestIdsLock = new();
            private readonly HashSet<string> requestIds = [];
            private long nextRequestId;

            public Task ServerInitialized => serverInitializedSource.Task;
            public bool IsServerInitialized => ServerInitialized.IsCompleted;

            public void EnqueueNotification(byte[] message)
            {
                notifications.Writer.TryWrite(message);
            }

            public void EnqueueServerRequest(string requestIdPfx, Func<string, string> buildRequest)
            {
                var requestId = requestIdPfx + Interlocked.Increment(ref nextRequestId);
                lock (requestIdsLock)
                    requestIds.Add(requestId);

                var message = Encoding.UTF8.GetBytes(FrameLspMessage(buildRequest(requestId)));
                serverRequests.Writer.TryWrite(message);
            }

            public void MarkServerInitialized() => serverInitializedSource.TrySetResult(true);

            public ValueTask<bool> WaitToReadNotificationsAsync(CancellationToken ct) =>
                notifications.Reader.WaitToReadAsync(ct);

            public bool TryReadNotification(out byte[] message) =>
                notifications.Reader.TryRead(out message!);

            public ValueTask<bool> WaitToReadServerRequestsAsync(CancellationToken ct) =>
                serverRequests.Reader.WaitToReadAsync(ct);

            public bool TryReadServerRequest(out byte[] message) =>
                serverRequests.Reader.TryRead(out message!);

            public bool ShouldSuppressClientMessage(byte[] message, string? method)
            {
                return IsInjectedRequestResponse(message, method)
                    || IsVsRefreshEchoNotification(message, method);
            }

            public void Complete()
            {
                notifications.Writer.TryComplete();
                serverRequests.Writer.TryComplete();
            }

            private bool IsInjectedRequestResponse(byte[] message, string? method)
            {
                if (method != null)
                    return false;

                var body = LspByteBuffer.TryExtractBody(message);
                if (body == null)
                    return false;

                lock (requestIdsLock) {
                    foreach (var id in requestIds) {
                        if (body.IndexOf($"\"id\":\"{id}\"", StringComparison.Ordinal) < 0)
                            continue;
                        requestIds.Remove(id);
                        return true;
                    }
                }
                return false;
            }

            private static bool IsVsRefreshEchoNotification(byte[] message, string? method)
            {
                if (!string.Equals(method, "NotificationReceived", StringComparison.Ordinal))
                    return false;
                var body = LspByteBuffer.TryExtractBody(message);
                return body?.IndexOf(
                    "\"MethodName\":\"workspace/semanticTokens/refresh\"",
                    StringComparison.Ordinal) >= 0;
            }

            private static Channel<byte[]> CreateChannel() =>
                Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
        }
    }
}
