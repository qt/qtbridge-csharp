// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only
#if DEBUG

using System.Diagnostics;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{

    /// <summary>
    /// An Extensions menu command that shows a diagnostic summary of the Qt Bridge project
    /// detection result for the active project or selected item.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ShowStatus(TraceSource traceSource, IQtBridgeProjectService projectSrv)
        : Command
    {
        private readonly TraceSource logger = Requires.NotNull(traceSource);
        private readonly IQtBridgeProjectService projectService = Requires.NotNull(projectSrv);

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.ShowStatus.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.DebugTemplate,
                IconSettings.IconAndText)
            };

        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            logger.TraceEvent(TraceEventType.Information, 0, "Qt Bridge extension command initialized.");
            return base.InitializeAsync(cancellationToken);
        }

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken ct)
        {
            var message = "No active Qt Bridge project context was found.";

            var activeProject = await context.GetActiveProjectAsync(ct);
            if (activeProject?.Path is { } projectPath && !string.IsNullOrWhiteSpace(projectPath)) {
                var metadata = await projectService.TryGetMetadataForPathAsync(projectPath, ct);
                if (metadata != null)
                    message = QtBridgeProjectSummaryFormatter.Format(metadata);
            } else {
                Uri? selectedPath = null;
                try {
                    selectedPath = await context.GetSelectedPathAsync(ct);
                } catch (Exception) {}

                if (TryGetLocalPath(selectedPath, out var selectionPath) && selectionPath != null) {
                    var data = await projectService.TryGetMetadataForPathAsync(selectionPath, ct);
                    if (data != null)
                        message = QtBridgeProjectSummaryFormatter.Format(data);
                }
            }

            await Extensibility.Shell().ShowPromptAsync(message, PromptOptions.OK, ct);
        }

        private static bool TryGetLocalPath(Uri? uri, out string? localPath)
        {
            localPath = null;
            if (uri is not { IsFile: true })
                return false;

            localPath = uri.LocalPath;
            return true;
        }
    }
}
#endif
