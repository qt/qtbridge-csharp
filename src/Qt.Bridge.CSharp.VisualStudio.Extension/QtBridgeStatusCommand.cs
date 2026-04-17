// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System.Diagnostics;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;

namespace Qt.Bridge.CSharp.VisualStudio.Extension
{

    /// <summary>
    /// An Extensions menu command that shows a diagnostic summary of the Qt Bridge project
    /// detection result for the active project or selected item.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class QtBridgeStatusCommand(
        TraceSource traceSource,
        IQtBridgeProjectService projectService) : Command
    {
        private readonly TraceSource logger = Requires.NotNull(traceSource);
        private readonly IQtBridgeProjectService projectService = Requires.NotNull(projectService);

        public override CommandConfiguration CommandConfiguration =>
            new("%QtBridge.ShowStatus.DisplayName%")
            {
                Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.Extension,
                IconSettings.IconAndText),
                Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu]
            };

        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            logger.TraceEvent(TraceEventType.Information, 0, "Qt Bridge extension command initialized.");
            return base.InitializeAsync(cancellationToken);
        }

        public override async Task ExecuteCommandAsync(
            IClientContext context,
            CancellationToken cancellationToken)
        {
            var message = "No active Qt Bridge project context was found.";

            var activeProject = await context.GetActiveProjectAsync(cancellationToken);
            if (activeProject?.Path is { } projectPath && !string.IsNullOrWhiteSpace(projectPath)) {
                var metadata = await projectService.TryGetMetadataForPathAsync(projectPath,
                    cancellationToken);
                if (metadata != null)
                    message = QtBridgeProjectSummaryFormatter.Format(metadata);
            } else {
                Uri? selectedPath = null;
                try {
                    selectedPath = await context.GetSelectedPathAsync(cancellationToken);
                } catch (Exception) {}

                if (TryGetLocalPath(selectedPath, out var selectionPath) && selectionPath != null) {
                    var metadata = await projectService.TryGetMetadataForPathAsync(selectionPath,
                        cancellationToken);
                    if (metadata != null)
                        message = QtBridgeProjectSummaryFormatter.Format(metadata);
                }
            }

            await Extensibility.Shell().ShowPromptAsync(
                message,
                PromptOptions.OK,
                cancellationToken);
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
