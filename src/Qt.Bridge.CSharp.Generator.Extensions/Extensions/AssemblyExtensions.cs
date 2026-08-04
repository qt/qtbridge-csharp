// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;
using Qt.Quick;

namespace Qt.Bridge.CodeGeneration.Extensions
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<QmlFileAttribute> QmlFiles(this Assembly self)
        {
            return self.QtAttributeData()
                .Where(x => x.AttributeType.Is<QmlFileAttribute>())
                .Select(x => new QmlFileAttribute()
                {
                    Uri = x.Property<string>(nameof(QmlFileAttribute.Uri)),
                    TypeName = x.Property<string>(nameof(QmlFileAttribute.TypeName)),
                    IsRoot = x.Property<bool>(nameof(QmlFileAttribute.IsRoot), true),
                    Path = x.Property<string>(nameof(QmlFileAttribute.Path))
                });
        }

        public static IEnumerable<QtResourceAttribute> QtResources(this Assembly self)
        {
            // All properties use the default-value overload: Property<T>(name, null) rather than
            // Property<T>(name). The no-default overload throws ArgumentException when a named
            // argument is absent from the attribute usage, and every QtResourceAttribute property
            // except Prefix and AccessMode is optional at the call site.
            return self.QtAttributeData()
                .Where(x => x.AttributeType.Is<QtResourceAttribute>())
                .Select(x => new QtResourceAttribute()
                {
                    SourcePath = x.Property<string>(nameof(QtResourceAttribute.SourcePath), null),
                    Alias = x.Property<string>(nameof(QtResourceAttribute.Alias), null),
                    Prefix = x.Property(nameof(QtResourceAttribute.Prefix), "/"),
                    AssemblyId = x.Property<string>(nameof(QtResourceAttribute.AssemblyId), null),
                    Key = x.Property<string>(nameof(QtResourceAttribute.Key), null),
                    AccessMode = x.Property(nameof(QtResourceAttribute.AccessMode), "Default"),
                    Reason = x.Property<string>(nameof(QtResourceAttribute.Reason), null)
                });
        }

        public static IEnumerable<QtResourceAttribute> QtResourcesWithReferences(
            this Assembly self,
            DependencyGraph graph)
        {
            var assemblies = new[] { self }.Concat(self.GetReferencedAssemblies().Select(name =>
            {
                try {
                    return graph.LoadAssembly(name);
                } catch {
                    return null;
                }
            }).Where(assembly => assembly != null));

            return assemblies.SelectMany(assembly => assembly.QtResources());
        }

        public static string QmlRootModule(this Assembly self)
        {
            return self.QtAttributeData()
                .Where(x => x.AttributeType.Is<QmlModuleAttribute>()
                    && x.Property<bool>(nameof(QmlModuleAttribute.IsRoot), true))
                .Select(x => x.Property<string>(nameof(QmlModuleAttribute.Uri)))
                .FirstOrDefault();
        }
    }
}
