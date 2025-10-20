/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.Runtime.CompilerServices;
using System.Diagnostics;
using Refit;

namespace ColorPalette
{
    public abstract class AbstractResource
    {
        internal RestService Service { get; set; }

        internal abstract void ResetConnection();

        protected void ErrorMsg(string message, [CallerFilePath] string source = "")
        {
            Debug.WriteLine($"[{Path.GetFileNameWithoutExtension(source)}] {message}");
        }

        protected void HandleRequestException(
            Exception e, Action<ApiException> fnApiEx, Action<Exception> fnEx = null,
            [CallerFilePath] string source = "")
        {
            var q = new Queue<Exception>();
            q.Enqueue(e);
            while (q.TryDequeue(out var ex)) {
                switch (ex) {
                    case ApiException apiEx:
                        fnApiEx?.Invoke(apiEx);
                        break;
                    case AggregateException aggEx:
                        foreach (var inEx in aggEx.InnerExceptions)
                            q.Enqueue(inEx);
                        break;
                    default:
                        if (fnEx != null)
                            fnEx.Invoke(ex);
                        else
                            ErrorMsg($"Request error: {ex.Message}", source);
                        break;
                }
            }
        }
    }
}
