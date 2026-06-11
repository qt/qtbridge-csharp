// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{
    internal sealed partial class WhatsNew
    {
        private static readonly Guid BrowserOwner = new("4074B4BA-0555-43D9-961D-37FEF8DF72A6");
        private static readonly List<VsWebBrowser> Browsers = [];
        private static readonly object BrowserUsersLock = new();

        private static void OpenBrowserWithFile(string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var tmp = Package.GetGlobalService(typeof(SVsWebBrowsingService));
            if (tmp is not IVsWebBrowsingService service)
                return;

            var startUrl = new Uri(path).AbsoluteUri;
            var browserOwner = BrowserOwner;

            var browserUser = new VsWebBrowser(startUrl);
            const uint flags =
                  (uint)__VSCREATEWEBBROWSER.VSCWB_AutoShow
                | (uint)__VSCREATEWEBBROWSER.VSCWB_NoHistory
                | (uint)__VSCREATEWEBBROWSER.VSCWB_StartCustom
                | (uint)__VSCREATEWEBBROWSER.VSCWB_ReuseExisting;

            ErrorHandler.ThrowOnFailure(service.CreateWebBrowser(
                flags,
                ref browserOwner,
                "Qt Bridge for C# - Welcome",
                startUrl,
                browserUser,
                out _,
                out var frame));
            lock (BrowserUsersLock) {
                Browsers.Add(browserUser);
            }
            ErrorHandler.ThrowOnFailure(frame.Show());
        }

        private sealed class VsWebBrowser(string welcomeUrl) : IVsWebBrowserUser
        {
            public int TranslateUrl(uint dwReserved, string lpszUrlIn, out string pbstrUrlOut)
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                pbstrUrlOut = lpszUrlIn;

                if (!Uri.TryCreate(lpszUrlIn, UriKind.Absolute, out var uri))
                    return VSConstants.S_OK;

                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) {
                    VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
                    pbstrUrlOut = welcomeUrl;
                    return VSConstants.S_OK;
                }

                if (uri.Scheme != "qtbridge")
                    return VSConstants.S_OK;

                ExecuteQtBridgeAction(uri.Host);
                pbstrUrlOut = welcomeUrl;
                return VSConstants.S_OK;
            }

            private static void ExecuteQtBridgeAction(string action)
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                switch (action) {
                case "create-project":
                    if (Package.GetGlobalService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte)
                        dte.ExecuteCommand("File.NewProject");
                    break;
                case "open-settings":
                    VisualStudioVersion.OpenSettings();
                    break;
                }
            }

            public int Disconnect()
            {
                lock (BrowserUsersLock) {
                    Browsers.Remove(this);
                }
                return VSConstants.S_OK;
            }

            public int Resize(int cx, int cy) => VSConstants.S_OK;
            public int GetCmdUIGuid(out Guid pguidCmdUi)
                => ErrorNotImplemented(out pguidCmdUi);
            public int GetCustomURL(uint nPage, out string pbstrUrl)
                => ErrorNotImplemented(out pbstrUrl);
            public int GetExternalObject(out object ppDispatch)
                => ErrorNotImplemented(out ppDispatch);
            public int GetOptionKeyPath(uint dwReserved, out string pbstrKey)
                => ErrorNotImplemented(out pbstrKey);
            public int FilterDataObject(IDataObject pDataObjIn, out IDataObject ppDataObjOut)
                => ErrorNotImplemented(out ppDataObjOut);
            public int GetDropTarget(IDropTarget pDropTgt, out IDropTarget ppDropTgt)
                => ErrorNotImplemented(out ppDropTgt);

            public int GetCustomMenuInfo(object pUnkCmdReserved,
                object pDispReserved,
                uint dwType,
                uint dwPosition,
                out Guid pguidCmdGroup,
                out int pdwMenuId)
            {
                pguidCmdGroup = Guid.Empty;
                pdwMenuId = 0;
                return VSConstants.E_NOTIMPL;
            }

            public int TranslateAccelarator(MSG[] lpmsg) => VSConstants.E_NOTIMPL;

            private static int ErrorNotImplemented<T>(out T value)
            {
                value = default!;
                return VSConstants.E_NOTIMPL;
            }
        }
    }
}
