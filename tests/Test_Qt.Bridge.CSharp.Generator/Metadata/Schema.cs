// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Qt.Bridge
{
    using Parameter = TypeMetadata.MethodMetadata.QtMetadata.ParameterMetadata;

    public enum BasicType
    {
        Bool, QInt8, QUInt8, QInt16, QUInt16, QInt32, QUInt32, QInt64, QUInt64,
        Double, Float, QChar, QString, QDateTime, QUrl, QVariant, Void = int.MinValue
    };

    public class Metadata
    {
        public TypeMetadata[] Types { get; set; }
        public Dictionary<string, TypeMetadata> TypesByName
            => (Types ?? []).ToDictionary(t => t.DotNet.Name);

        public class ParameterConverter : JsonConverter<Parameter>
        {
            public override Parameter Read(
                ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Parameter param = null;
                try {
                    param = new()
                    {
                        Type = JsonSerializer.Deserialize<BasicType>(ref reader, options)
                    };
                } catch(JsonException) {
                    param = JsonSerializer.Deserialize<Parameter>(ref reader, options);
                }
                if (param is not { Type: >= 0 })
                    throw new JsonException("Invalid parameter type.");
                return param;
            }

            public override void Write(
                Utf8JsonWriter writer, Parameter value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public static Metadata FromJson(string json)
        {
            return JsonSerializer.Deserialize<Metadata>(json, new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                    new Metadata.ParameterConverter()
                }
            });
        }
    }

    public class TypeMetadata
    {
        public override string ToString() => DotNet.Name;

        public class DotNetMetadata
        {
            public string Name { get; set; }
            public string AssemblyQualifiedName { get; set; }
            public string AssemblyFile { get; set; }
            public string AssemblyFileHash { get; set; }
            public double ModuleMetadataToken { get; set; }
            public double MetadataToken { get; set; }
        }
        public DotNetMetadata DotNet { get; set; }

        public class QtMetadata
        {
            public string Name { get; set; }

            public class ModelMetadata
            {
                public enum ModelBaseClass { Model, ListModel, TableModel };
                public ModelBaseClass BaseClass { get; set; }

                public enum ModelOverride
                {
                    Buddy, CanFetchMore, ColumnCount, Data, FetchMore, Flags, HasChildren,
                    HeaderData, Index, InsertColumns, InsertRows, MoveColumns, MoveRows, Parent,
                    RemoveColumns, RemoveRows, RoleNames, RowCount, SetData, SetHeaderData, Sibling,
                    Sort
                };
                public ModelOverride[] Overrides { get; set; }

                public class CollectionMetadata
                {
                    public string CountMethod { get; set; }
                    public string ItemMethod { get; set; }
                    public bool IsObservable { get; set; }

                    public class RoleMetadata
                    {
                        public class DotNetMetadata
                        {
                            public string Name { get; set; }
                        }
                        public DotNetMetadata DotNet { get; set; }

                        public class QtMetadata
                        {
                            public string Name { get; set; }
                        }
                        public QtMetadata Qt { get; set; }
                    }
                    public RoleMetadata[] Roles { get; set; }
                }
                public CollectionMetadata Collection { get; set; }
            }
            public ModelMetadata Model { get; set; }

            public bool IsQmlElement { get; set; }

            public Dictionary<string, double> Enum { get; set; }

            public class QmlMetadata
            {
                public string Name { get; set; }
                public string Module { get; set; }
                public long ModuleRevisionMajor { get; set; }
                public long ModuleRevisionMinor { get; set; }
                public bool Singleton { get; set; }
            }
            public QmlMetadata Qml { get; set; }
        }
        public QtMetadata Qt { get; set; }

        public class PropertyMetadata
        {
            public override string ToString() => $"{DotNet.Name}: {Qt.Type}";

            public class DotNetMetadata
            {
                public string Name { get; set; }
                public bool HasGet { get; set; }
                public bool HasSet { get; set; }
                public bool IsNotifiable { get; set; }
            }
            public DotNetMetadata DotNet { get; set; }

            public class QtMetadata
            {
                public string Name { get; set; }
                public BasicType Type { get; set; }
            }
            public QtMetadata Qt { get; set; }
        }
        public PropertyMetadata[] Properties { get; set; }
        public Dictionary<string, PropertyMetadata> PropertiesByName
            => (Properties ?? []).ToDictionary(p => p.DotNet.Name);

        public class EventMetadata
        {
            public override string ToString() => DotNet.Name;

            public class DotNetMetadata
            {
                public string Name { get; set; }
            }
            public DotNetMetadata DotNet { get; set; }

            public class QtMetadata
            {
                public string Signal { get; set; }
            }
            public QtMetadata Qt { get; set; }
        }
        public EventMetadata[] Events { get; set; }
        public Dictionary<string, EventMetadata> EventsByName
            => (Events ?? []).ToDictionary(e => e.DotNet.Name);

        public class MethodMetadata
        {
            public override string ToString() => $"{Qt.ReturnType} {DotNet.Name}({string
                .Join(", ", (Qt.Parameters ?? []).Select(p => p.Type))})";

            public class DotNetMetadata
            {
                public double MetadataToken { get; set; }
                public string Name { get; set; }
                public string[] ParameterTypes { get; set; }
            }
            public DotNetMetadata DotNet { get; set; }

            public class QtMetadata
            {
                public string Name { get; set; }

                public BasicType ReturnType { get; set; }

                public class ParameterMetadata
                {
                    public string Name { get; set; }
                    public BasicType Type { get; set; }
                }
                public ParameterMetadata[] Parameters { get; set; }
            }
            public QtMetadata Qt { get; set; }
        }
        public MethodMetadata[] Methods { get; set; }
        public Dictionary<string, MethodMetadata[]> MethodsByName
            => (Methods ?? []).GroupBy(e => e.DotNet.Name)
                .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
