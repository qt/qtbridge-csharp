// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

namespace Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext
{
    /// <summary>
    /// Provides access to Visual Studio project and document context needed to drive QML Language
    /// Server activation and configuration.
    /// </summary>
    internal interface IProjectContextService
    {
        /// <summary> Returns the full path of the currently active project, or null. </summary>
        Task<string?> GetActiveProjectPathAsync(CancellationToken cancellationToken);

        /// <summary> Returns the full path of the currently active document, or null. </summary>
        Task<string?> GetActiveDocumentPathAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the full paths of all projects currently loaded in the solution.
        /// </summary>
        Task<IReadOnlyList<string>> GetLoadedProjectPathsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the name of the active solution build configuration (e.g. <c>Debug</c>), or
        /// null if no solution is open.
        /// </summary>
        Task<string?> GetActiveConfigurationAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the platform name of the active solution build configuration
        /// (e.g. <c>x64</c>), or null if no solution is open or the platform cannot be
        /// determined.
        /// </summary>
        Task<string?> GetActivePlatformAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the full path of the project that owns the given file, as reported by the DTE
        /// project system. Returns null if the file is not part of any loaded project or if the
        /// DTE is unavailable.
        /// </summary>
        Task<string?> GetOwningProjectPathAsync(string filePath, CancellationToken ct);

        /// <summary>
        /// Subscribes to VS project and document context changes. The callback is invoked with a
        /// short debounce when the active project, solution, or active document changes. Dispose
        /// the returned handle to unsubscribe.
        /// </summary>
        IDisposable SubscribeToContextChanged(Action onContextChanged);
    }
}
