// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Qt.Bridge.CodeGeneration.Static { internal static class StaticFiles { } }

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode
{
    using static Traits;

    public class GenerateStaticFiles : Rule
    {
        public override bool Matches(MemberInfo src) => src.IsRootNode();
        public override int Priority => int.MaxValue;

        private ResourceManager ResourceManager { get; }
            = new(typeof(Static.StaticFiles).FullName, typeof(Static.StaticFiles).Assembly);

        public override Result Execute(MemberInfo _)
        {
            var resObjs = ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            foreach (var resObj in resObjs) {
                if (resObj is not DictionaryEntry res)
                    continue;
                if (res.Value is not byte[] { Length: > 0 } data)
                    continue;
                var charData = new char[data.Length];
                if (!Encoding.UTF8.TryGetChars(data, charData, out var charDataLen))
                    continue;
                var text = new string(charData, 0, charDataLen);
                if (res.Key is not string str || str.Split(',') is not { Length: > 0 } metaData)
                    continue;

                if (metaData.ElementAtOrDefault(0) is not { Length: > 0 } fileIdStr)
                    continue;
                if (!System.Enum.TryParse<Placeholders>(fileIdStr, out var fileId))
                    continue;

                if (metaData.ElementAtOrDefault(1) is not { Length: > 0 } path)
                    continue;

                var file = new FilePlaceholder(fileId, Root, $@"{Root.MFn(Dir)}{path}");
                file.AddText(text);

                if (metaData.ElementAtOrDefault(2) is { Length: > 0 } pathsIdStr) {
                    if (!System.Enum.TryParse<Placeholders>(pathsIdStr, out var pathsId))
                        return Error();
                    if (Root.GetPlaceholder(pathsId) is not { } paths)
                        return Error();
                    paths += path;
                }
            }

            return Ok;
        }
    }
}
