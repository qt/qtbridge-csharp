// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Metadata
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateEvent : Rule
    {
        public override bool Matches(MemberInfo src) => src is EventInfo ev
            && ev.AddMethod?.IsStatic == false
            && ev.ReflectedType.ExportAsMetadata();

        public override Result Execute(MemberInfo src)
        {
            if (src is not EventInfo ev)
                return Error();
            var type = src.ReflectedType;
            if (ev.EventHandlerType.DelegateSignature()?.ToArray() is not { Length: 3 } evTypes
                || !evTypes[2].IsAssignableTo(TypeOf<EventArgs>())) {
                return Error();
            }
            var argsType = evTypes[2];

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MetadataEvents) is not { } jsonEvents)
                return Error();

            Placeholder jsonEvent = null;
            jsonEvents += $@"
{{
    {jsonEvents[jsonEvent = new(MetadataEvent, ev) { Sorted = false, Separator = "," }]}
}}
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            jsonEvent += $@"
""dotNet"": {{
    {jsonEvent[new(DotNetInfo, ev)
            {
                Sorted = false,
                Separator = ",",
                Content = [
                    $@"""name"": ""{ev.MFn(Src)}"""
                ]
            }]}
}}";

            return Ok;
        }
    }
}
