/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;

namespace Qt.DotNet.CodeGeneration.Rules.Class
{
    using MetaFunctions;
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateClass : GenerateClassFiles
    {
        public override int Priority => base.Priority + 1;
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.IsQmlElement()) {
                if (Root.GetPlaceholder(IncludeDirs) is not { } includeDirs)
                    return Error();
                includeDirs += $"include_directories({Hpp}/{type.MFn(Ns | Dir)})";

                if (type.GetPlaceholder(Includes) is not { } includes)
                    return Error();
                includes += "#include <QtQml/qqmlregistration.h>";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDecl) is not { } forwardDecl)
                return Error();
            forwardDecl += $"class {type.MFn(Name)};";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDeclBaseOf) is not { } forwardDeclBaseOf)
                return Error();
            forwardDeclBaseOf += $@"
template<>
constexpr bool is_base_of_v<QDotNetRef, {type.MFn(Ns | Name)}> = true;
template<>
constexpr bool is_base_of_v<QDotNetObject, {type.MFn(Ns | Name)}> = true;
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDeclTypeOf) is not { } forwardDeclTypeOf)
                return Error();
            forwardDeclTypeOf += $@"
template<>
struct QDotNetTypeOf<{type.MFn(Ns | Name)}>
{{
    static inline const QString TypeName = QStringLiteral(""{type.MFn(Src | Fqn)}"");
    static inline UnmanagedType MarshalAs = UnmanagedType::ObjectRef;
}};
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(ForwardDeclPrivate) is not { } forwardDeclPrivate)
                return Error();
            forwardDeclPrivate += $"struct {type.MFn(Name | Private)};";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PublicDeclarations) is not { } publicDecl)
                return Error();
            publicDecl += $@"
class {type.MFn(Ns | Name)} :
    {publicDecl[new(QObjectBaseClass) { Content = ["public QObject"] }]},
    {publicDecl[new(BaseClasses)]}
    public QDotNetObject
{{
    Q_OBJECT
    {(!type.IsQmlElement() ? Wrap : type.QmlElementName() is not { Length: > 0 } elementName
        ? "QML_ELEMENT"
        : $"QML_NAMED_ELEMENT({elementName})")}
    {(type.IsQmlSingleton() ? "QML_SINGLETON" : Wrap)}
    {publicDecl[new(TypeTraits)]}
public:
    Q_DOTNET_OBJECT({type.MFn(Name)},
        ""{type.MFn(Src | Fqn)}"");

    {publicDecl[new(CtorDeclarations)]}
    ~{type.MFn(Name)}() override;

    {publicDecl[new(PropertyDeclarations)]}
    {publicDecl[new(MethodDeclarations)]}
    {publicDecl[new(SignalDeclarations)]}
protected:
    void connectNotify(const QMetaMethod &signal) override;

private:
    {type.MFn(Name | Private)} *d = nullptr;
    friend {type.MFn(Name | Private)};
}};
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } privateIncludes)
                return Error();
            privateIncludes += "#include <QMetaMethod>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateDeclarations) is not { } privateDecl)
                return Error();
            privateDecl += $@"
struct {type.MFn(Ns | Name | Private)}
{{
    {type.MFn(Name)} *q;
    {type.MFn(Name | Private)}({type.MFn(Name)} *q);
    ~{type.MFn(Name | Private)}();
    {(!type.Implements<INotifyPropertyChanged>() ? ""
        : "void onPropertyChanged(const QString &propertyName);")}
    {privateDecl[new Placeholder(PrivateMemberDeclarations)]}
}};
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(QDotNetObjectImpl) is not { } qDotNetObjImpl)
                return Error();
            qDotNetObjImpl += $@"
namespace {src.MFn(Ns)}
{{
    Q_DOTNET_OBJECT_IMPL({type.MFn(Name)},
        Q_DOTNET_OBJECT_INIT(d(new {type.MFn(Name | Private)}(this))));
}}
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
void {type.MFn(Ns | Name)}::connectNotify(const QMetaMethod &signal)
{{
    QString signalTag = signal.tag();
    {implementation[new(EventSubscribers)]}
}}
{Blank}";
            implementation += $@"
{type.MFn(Ns | Name | Private)}::{type.MFn(Name | Private)}({type.MFn(Ns | Name)} *q) : q(q)
{{
}}

{type.MFn(Ns | Name | Private)}::~{type.MFn(Name | Private)}()
{{
    {implementation[new(EventUnsubscribers)]}
}}

{type.MFn(Ns | Name)}::~{type.MFn(Name)}()
{{
    delete d;
}}
{Blank}";
            if (type.Implements<INotifyPropertyChanged>()) {
                implementation += $@"
void {type.MFn(Ns | Name | Private)}::onPropertyChanged(const QString &propertyName)
{{
    {implementation[new(PropertyNotifiers)]}
}}
{Blank}";
            }
            return Ok;
        }
    }
}
