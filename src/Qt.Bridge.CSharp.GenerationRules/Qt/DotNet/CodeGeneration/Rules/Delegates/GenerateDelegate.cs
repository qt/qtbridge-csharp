// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Delegates
{
    using MetaFunctions;
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateDelegate : GenerateDelegateHeader
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            var sigTypes = type.DelegateSignature();
            var signatureParameters = new[]
            {
                $"QDotNetCallbackReturn<{sigTypes.First().MFn(Ns | Name)}>::Parameter"
            }.Concat(sigTypes.Skip(1)
                .Select(x => $"QDotNetCallbackArg<{x.MFn(Ns | Name)}>::Parameter"));
            var baseClass = $"QDotNetDelegate<{string
                .Join($", ", sigTypes.Select(x => $"{x.MFn(Ns | Name)}"))}>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDecl) is not { } forwardDecl)
                return Error();
            forwardDecl += $"struct {type.MFn(Name)};";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += "#include <QDotNetDelegate>";
            foreach (var sigType in sigTypes.Where(x => !x.IsBuiltIn()))
                includes += $"#include <{sigType.MFn(Ns | Dir)}{sigType.MFn(File)}.h>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PublicDeclarations) is not { } publicDecl)
                return Error();
            publicDecl += $@"

struct {type.MFn(Ns | Name)} : public {baseClass}
{{
    static inline const QString AssemblyQualifiedName =
        QStringLiteral(""{type.MFn(Src | Fqn)}"");
    static QList<QDotNetParameter> SignatureParameters()
    {{
        return {{ {string.Join($",{Wrap}\n            ", signatureParameters)} }};
    }};

    {type.MFn(Name)}(nullptr_t) : {baseClass}(nullptr) {{ }}
    {type.MFn(Name)}(const void *objectRef): {baseClass}(objectRef) {{ }}
    {type.MFn(Name)}(const {type.MFn(Name)} &cpySrc) : {baseClass}(cpySrc) {{ }}
    {type.MFn(Name)} &operator=(const {type.MFn(Name)} &cpySrc)
    {{
        {baseClass}::operator=(cpySrc);
        return *this;
    }}
    {type.MFn(Name)}({type.MFn(Name)} &&movSrc) noexcept : {baseClass}(std::move(movSrc)) {{ }}
    {type.MFn(Name)} &operator=({type.MFn(Name)} &&movSrc) noexcept
    {{
        {baseClass}::operator=(std::move(movSrc));
        return *this;
    }}
#ifdef QT_QUICK_LIB
    static {type.MFn(Name)} fromScriptValue(const QJSValue &value, const QObject *context = nullptr)
    {{
        return QDotNetConvert::fromScriptDelegate<{type.MFn(Name)}, {string.Join(", ",
            sigTypes.Select(x => x.MFn(Ns | Name)))}>(AssemblyQualifiedName, value, context);
    }}
#endif
}};
";
            return Ok;
        }
    }
}
