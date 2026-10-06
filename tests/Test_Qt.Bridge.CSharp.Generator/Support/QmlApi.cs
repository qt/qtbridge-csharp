// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Test_Qt.Bridge.CSharp.Generator.Support
{
    internal static class QmlApi
    {
        // Reads the QML API from a header generated for a source code export.
        public static string[] FromSourceCode(string header)
        {
            var api = new List<string>();
            foreach (var (name, _, writable, notify) in SourceCodeProperties(header)) {
                api.Add($"property {name} {(writable ? "writable" : "readonly")}"
                    + (notify == null ? "" : $" notify {notify}"));
            }
            foreach (Match method in Regex.Matches(header,
                @"Q_INVOKABLE [^(]*?\b(?<name>\w+)\((?<args>[^)]*)\)")) {
                var arity = method.Groups["args"].Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                api.Add($"method {method.Groups["name"].Value}/{arity}");
            }
            foreach (Match signal in Regex.Matches(header, @"Q_SIGNAL void (?<name>\w+)\("))
                api.Add($"signal {signal.Groups["name"].Value}");
            foreach (Match entry in Regex.Matches(
                Regex.Match(header, @"enum Values\s*\{(?<body>[^}]*)\}").Groups["body"].Value,
                @"(?<key>\w+)\s*=\s*(?<value>-?\d+)")) {
                api.Add($"enum {entry.Groups["key"].Value} = {entry.Groups["value"].Value}");
            }
            return [.. api.Distinct()];
        }

        // Reads the QML API from a type metadata entry, using the rules with which
        // metadata loader and QDotNetDynamicObject build the metaobject at run time.
        public static string[] FromMetadata(JsonNode type)
        {
            var api = new List<string>();
            if (type["qt"]!["isQmlElement"]?.GetValue<bool>() == true)
                api.Add("property nestedQmlElements readonly");
            foreach (var (name, _, writable, notify) in MetadataProperties(type)) {
                api.Add($"property {name} {(writable ? "writable" : "readonly")}"
                    + (notify == null ? "" : $" notify {notify}"));
                if (notify != null)
                    api.Add($"signal {notify}");
            }
            foreach (var ev in type["events"]?.AsArray() ?? [])
                api.Add($"signal {QtName(ev!["qt"]?["signal"], ev["dotNet"]!)}");
            foreach (var method in type["methods"]?.AsArray() ?? []) {
                var arity = method!["qt"]!["parameters"]?.AsArray().Count ?? 0;
                api.Add($"method {QtName(method["qt"]!["name"], method["dotNet"]!)}/{arity}");
            }
            api.AddRange(["method toString/0", "method equals/1", "method asDotNetObject/0"]);
            foreach (var entry in type["qt"]!["enum"]?.AsObject() ?? [])
                api.Add($"enum {entry.Key} = {entry.Value!.GetValue<int>()}");
            return [.. api.Distinct()];
        }

        public static IEnumerable<(string Name, string Type, bool Writable, string Notify)>
            SourceCodeProperties(string header)
        {
            foreach (Match property in Regex.Matches(header, @"Q_PROPERTY\((?<decl>[^)]*)\)")) {
                var tokens = property.Groups["decl"].Value
                    .Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                var accessor = Array.FindIndex(tokens,
                    token => token is "READ" or "WRITE" or "MEMBER");
                var notify = Array.IndexOf(tokens, "NOTIFY");
                yield return (
                    tokens[accessor - 1].TrimStart('*'),
                    string.Join(" ", tokens[..(accessor - 1)])
                        + (tokens[accessor - 1].StartsWith('*') ? " *" : ""),
                    tokens.Contains("WRITE"),
                    notify < 0 ? null : tokens[notify + 1]);
            }
        }

        public static IEnumerable<(string Name, string Type, bool Writable, string Notify)>
            MetadataProperties(JsonNode type)
        {
            foreach (var property in type["properties"]?.AsArray() ?? []) {
                var dotNet = property!["dotNet"]!;
                var qt = property["qt"]!;
                var name = QtName(qt["name"], dotNet);
                yield return (
                    name,
                    qt["type"]!.GetValue<string>(),
                    dotNet["hasSet"]?.GetValue<bool>() != false,
                    dotNet["isNotifiable"]?.GetValue<bool>() == false
                        ? null
                        : qt["notify"]?.GetValue<string>() ?? $"{name}Changed");
            }
        }

        private static string QtName(JsonNode qtName, JsonNode dotNet)
        {
            if (qtName?.GetValue<string>() is { } name)
                return name;
            name = dotNet["name"]!.GetValue<string>();
            return char.ToLowerInvariant(name[0]) + name[1..];
        }
    }
}
