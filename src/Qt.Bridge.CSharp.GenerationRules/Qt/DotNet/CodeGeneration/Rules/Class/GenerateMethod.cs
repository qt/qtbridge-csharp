// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateMethod : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src) => src is MethodInfo { IsStatic: false }
            && !src.ReflectedType.IsStaticClass();
        public override Result Execute(MemberInfo src)
        {
            if (src is not MethodInfo func)
                return Error();

            var type = src.ReflectedType;
            var returnType = func.ReturnType;
            if (func.GetParameters() is not { } args)
                return Error();

            var hasDelegateArgs = args.Any(IsDelegateParam);
            if (hasDelegateArgs) {
                if (type.GetPlaceholder(Includes) is not { } includes)
                    return Error();
                includes += "#include <QJSValue>";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            methods += $@"
Q_INVOKABLE {returnType.MFn(Ns | Name | Arg)} {func.MFn(Name)}({string.Join(", ", args
    .Select(QmlArgDecl))}) const;
{Blank}";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
mutable QDotNetFunction<{returnType.MFn(Ns | Name)}{args switch
            {
                { Length: > 0 } => ", " + string
                    .Join(", ", args.Select(arg => arg.ParameterType.MFn(Ns | Name))),
                _ => string.Empty
            }}> {func.MFn(Func)} = nullptr;";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
{returnType.MFn(Ns | Name | Arg)} {type.MFn(Ns | Name)}::{func.MFn(Name)}({string.Join(", ", args
    .Select(QmlArgDecl))}) const
{{
    {(returnType.Is(typeof(void)) ? string.Empty :
        "auto result = ")}method(""{func.MFn(Src)}"", d->{func.MFn(Func)}).invoke(*this{args switch
        {
            { Length: > 0 } => ", " + string.Join(", ", args
                .Select(InvokeArg)),
            _ => string.Empty
        }});
{(returnType.Is(typeof(void)) ? Wrap
    : !returnType.IsObject() ? $"{Tab}return result;"
    : returnType.Is<object>() ? $"{Tab}return Convert::toVariant(result);"
    : $"{Tab}return Convert::moveToHeap(result, this);")}
}}
{Blank}";

            return Ok;

            static bool IsDelegateParam(ParameterInfo arg)
                => arg.ParameterType.IsAssignableTo(TypeOf<Delegate>());

            static string QmlArgType(ParameterInfo arg)
                => IsDelegateParam(arg) ? "QJSValue" : arg.ParameterType.MFn(Ns | Name | Arg);

            static string QmlArgDecl(ParameterInfo arg)
                => $"{QmlArgType(arg)} {arg.MFn(Name | Src)}";

            static string InvokeArg(ParameterInfo arg)
            {
                return IsDelegateParam(arg)
                    ? $"{arg.ParameterType.MFn(Ns | Name)}::fromScriptValue({arg.MFn(Name | Src)}, this)"
                    : $"{arg.ParameterType.MFn(Star)}{arg.MFn(Name)}";
            }
        }
    }
}
