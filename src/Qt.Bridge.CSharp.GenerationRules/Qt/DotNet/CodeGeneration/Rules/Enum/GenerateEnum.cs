// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Enum
{
    using static Placeholders;
    using static Traits;

    public class GenerateEnum : GenerateEnumHeader
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type { IsEnum: true } type)
                return Error();

            if (type.GetEnumNames() is not { Length: > 0 } names)
                return Error();
            if (type.GetEnumValuesAsUnderlyingType() is not { Length: > 0 } values)
                return Error();
            if (names.Length != values.Length)
                return Error();
            var enumValues = names
                .Select((x, i) => new { Name = x, Value = values.GetValue(i) })
                .Where(x => x?.Value switch
                {
                    sbyte or byte or short or ushort or int => true,
                    uint u32 => u32 <= int.MaxValue,
                    ulong u64 => u64 <= int.MaxValue,
                    long s64 => int.MinValue <= s64 && s64 <= int.MaxValue,
                    _ => false
                })
                .Select(x => $"{x.Name.MFn(Enum)} = {x.Value}")
                .ToList();
            if (!enumValues.Any())
                return Error();
            if (type.GetEnumUnderlyingType() is not { } valuesType)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PublicDeclarations) is not { } publicDecl)
                return Error();
            publicDecl += $@"

namespace {type.MFn(Ns)}
{{
    class {type.MFn(Name | Enum)};
}}

class {type.MFn(Ns | Name | Enum)} : public QObject
{{
    Q_OBJECT
    QML_NAMED_ELEMENT({type.MFn(Name)})
    QML_UNCREATABLE(""Type '{type.MFn(Name)}' is an enum."")
public:
    enum Values
    {{
        {string.Join(@",
        ", enumValues)}
    }};
    Q_ENUM(Values)
}};

namespace {type.MFn(Ns)}
{{
    using {type.MFn(Name)} = {type.MFn(Name | Enum)}::Values;
}}

template<>
struct QDotNetTypeOf<{type.MFn(Ns | Name)}>
{{
    static inline const QString TypeName = QString(
        ""{type.MFn(Src | Fqn)}"");
    static inline UnmanagedType MarshalAs = QDotNetTypeOf<{valuesType.MFn(Ns | Name)}>::MarshalAs;
}};

template<>
struct QDotNetOutbound<{type.MFn(Ns | Name)}>
{{
    using SourceType = {type.MFn(Ns | Name)};
    using OutboundType = {valuesType.MFn(Ns | Name)};
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<{type.MFn(Ns | Name)}>::TypeName,
        QDotNetTypeOf<{type.MFn(Ns | Name)}>::MarshalAs);
    static OutboundType convert(SourceType srvValue)
    {{
        return static_cast<OutboundType>(srvValue);
    }}
}};

template<>
struct QDotNetInbound<{type.MFn(Ns | Name)}>
{{
    using InboundType = {valuesType.MFn(Ns | Name)};
    using TargetType = {type.MFn(Ns | Name)};
    static inline const QDotNetParameter Parameter = QDotNetParameter(
        QDotNetTypeOf<{type.MFn(Ns | Name)}>::TypeName,
        QDotNetTypeOf<{type.MFn(Ns | Name)}>::MarshalAs);
    static TargetType convert(InboundType inboundValue)
    {{
        return static_cast<TargetType>(inboundValue);
    }}
}};
";
            return Ok;
        }
    }
}
