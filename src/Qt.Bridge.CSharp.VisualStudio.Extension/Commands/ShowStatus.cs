// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only
#if DEBUG

using System.Diagnostics;
using System.Text;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Qt.Bridge.CSharp.VisualStudio.Core.ProjectSystem;
using Qt.Bridge.CSharp.VisualStudio.Extension.Diagnostics;
using Qt.Bridge.CSharp.VisualStudio.Extension.VisualStudioContext;

namespace Qt.Bridge.CSharp.VisualStudio.Extension.Commands
{

    /// <summary>
    /// An Extensions menu command that shows the Qt Bridge project detection result for the
    /// active project or selected item, and the evaluated project data it is based on.
    /// </summary>
    [VisualStudioContribution]
    internal sealed class ShowStatus(
        TraceSource traceSource,
        IQtBridgeProjectService projectSrv,
        QtBridgeProjectCapabilityDetector capabilityDetector,
        IExtensionLog extensionLog)
        : Command
    {
        private readonly TraceSource logger = Requires.NotNull(traceSource);
        private readonly IQtBridgeProjectService projectService = Requires.NotNull(projectSrv);
        private readonly QtBridgeProjectCapabilityDetector detector =
            Requires.NotNull(capabilityDetector);
        private readonly IExtensionLog log = Requires.NotNull(extensionLog);

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
            var path = (await context.GetActiveProjectAsync(ct))?.Path;
            if (string.IsNullOrWhiteSpace(path)) {
                Uri? selectedPath = null;
                try {
                    selectedPath = await context.GetSelectedPathAsync(ct);
                } catch (Exception) {}

                if (TryGetLocalPath(selectedPath, out var selectionPath))
                    path = selectionPath;
            }

            var metadata = string.IsNullOrWhiteSpace(path)
                ? null : await projectService.TryGetMetadataForPathAsync(path!, ct);
            if (metadata == null) {
                var reason = string.IsNullOrWhiteSpace(path)
                    ? "No single project or file is selected."
                    : $"No project file was found for '{path}'.";
                var failure = reason + " Select one project or one of its files.";
                log.Info($"Show Status at {DateTime.Now:HH:mm:ss.fff}: {failure}");
                await Extensibility.Shell().ShowPromptAsync(failure, PromptOptions.OK, ct);
                return;
            }

            var lookup = await detector.GetEvaluatedProjectAsync(metadata.ProjectFilePath, ct);
            var message = QtBridgeProjectSummaryFormatter.Format(metadata)
                + Environment.NewLine + Environment.NewLine + FormatEvaluation(lookup);

            log.Info($"Show Status at {DateTime.Now:HH:mm:ss.fff}{Environment.NewLine}{message}");
            await Extensibility.Shell().ShowPromptAsync(message, PromptOptions.OK, ct);
        }

        // Also for statically detected projects, which the service does not evaluate.
        private static string FormatEvaluation(QtBridgeProjectCapabilityDetector.Lookup lookup)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Evaluated project ({(lookup.FromTracking ? "tracked" : "queried")}, "
                + $"tracking {(lookup.IsTracking ? "on" : "off")}, version {lookup.Version}):");
            if (lookup.Error != null) {
                builder.AppendLine($"  Query failed: {lookup.Error}");
                return builder.ToString().TrimEnd();
            }
            if (lookup.Project is not { } project) {
                builder.AppendLine("  Not loaded in the project system.");
                return builder.ToString().TrimEnd();
            }

            builder.AppendLine($"  Qt Bridge capability: {project.HasCapability}");
            builder.AppendLine($"  Qt Bridge package reference: {project.PackageId ?? "<none>"}");
            foreach (var configuration in project.ActiveConfigurations) {
                builder.AppendLine($"  Package references of '{configuration.Name}': "
                    + configuration.PackageReferences.Count);
                foreach (var reference in configuration.PackageReferences)
                    builder.AppendLine($"    {reference.Name} {reference.Version}");
            }
            return builder.ToString().TrimEnd();
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
