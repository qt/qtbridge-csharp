// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using static Placeholders;

    public class GenerateMetadataFile : Rule
    {
        public override bool Matches(MemberInfo src) => src.IsRootNode();
        public override Result Execute(MemberInfo _)
        {
            var json = new FilePlaceholder(MetadataFile, Root, "qt_bridge_metadata.json")
            {
                IndentChars = "  "
            };
            json += $@"
{{
  ""$schema"": ""https://code.qt.io/cgit/qt/qtbridge-csharp.git/plain/qt_bridge_metadata_schema.json"",
  ""types"": [
{json[new(MetadataTypes) { Sorted = true, Separator = ",", Indent = 2 }]}
  ]
}}
";
            return Ok;
        }
    }
}
