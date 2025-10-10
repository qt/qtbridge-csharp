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
            forwardDeclPrivate += $"struct {type.MFn(Name | Init)};";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PublicDeclarations) is not { } publicDecl)
                return Error();
            publicDecl += $@"
class {type.MFn((System.Enum)(Ns | Name))} :
    {publicDecl[(Placeholder)new((System.Enum)QObjectBaseClass) { Content = ["public QObject"] }]},
    {publicDecl[(Placeholder)new((System.Enum)BaseClasses)]}
    public QDotNetObject
{{
    Q_OBJECT
    {(!type.IsQmlElement() ? Wrap : type.QmlElementName() is not { Length: > 0 } elementName
        ? "QML_ELEMENT"
        : $"QML_NAMED_ELEMENT({elementName})")}
    {(type.IsQmlSingleton() ? "QML_SINGLETON" : Wrap)}
    {publicDecl[(Placeholder)new((System.Enum)TypeTraits)]}
public:
    Q_DOTNET_OBJECT({type.MFn((System.Enum)Name)},
        ""{type.MFn((System.Enum)(Src | Fqn))}"");

    {publicDecl[(Placeholder)new((System.Enum)CtorDeclarations)]}
    ~{type.MFn((System.Enum)Name)}() override;

    {publicDecl[(Placeholder)new((System.Enum)PropertyDeclarations)]}
    {publicDecl[(Placeholder)new((System.Enum)MethodDeclarations)]}
    {publicDecl[(Placeholder)new((System.Enum)SignalDeclarations)]}
protected:
    void connectNotify(const QMetaMethod &signal) override;

private:
    {type.MFn((System.Enum)(Name | Private))} *d = nullptr;
    friend {type.MFn((System.Enum)(Name | Private))};
    {type.MFn((System.Enum)(Name | Init))} *i = nullptr;
    friend {type.MFn((System.Enum)(Name | Init))};
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
{Blank}
    template<typename T>
    T *asQObject(T &obj);
{Blank}
    {(!type.Implements<INotifyPropertyChanged>() ? ""
        : "void onPropertyChanged(const QString &propertyName);")}
    {privateDecl[new Placeholder(PrivateMemberDeclarations)]}
}};

struct {type.MFn(Ns | Name | Init)}
{{
    {type.MFn(Name | Init)}({type.MFn(Name)} *q, {type.MFn(Name | Private)} *d);
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
        Q_DOTNET_OBJECT_INIT(
            d(new {type.MFn(Name | Private)}(this)),
            i(new {type.MFn(Name | Init)}(this, d))));
}}
{Blank}";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Implementation) is not { } implementation)
                return Error();
            implementation += $@"
{implementation[new(PublicCtors)]}
{type.MFn(Ns | Name | Private)}::{type.MFn(Name | Private)}({type.MFn(Ns | Name)} *q) : q(q)
{{
    {implementation[new(PrivateCtor)]}
}}

{type.MFn(Ns | Name | Init)}::{type.MFn(Name | Init)}(
    {type.MFn(Ns | Name)} *q,
    {type.MFn(Ns | Name | Private)} *d)
{{
    {implementation[new(Initializer)]}
}}

{type.MFn(Ns | Name | Private)}::~{type.MFn(Name | Private)}()
{{
    {implementation[new(EventUnsubscribers)]}
    {implementation[new(PrivateDtor)]}
}}

{type.MFn(Ns | Name)}::~{type.MFn(Name)}()
{{
    delete i;
    delete d;
    {implementation[new(PublicDtor)]}
}}

template<typename T>
T *{type.MFn(Ns | Name | Private)}::asQObject(T &obj)
{{
    auto *qobj = new T(std::move(obj));
    if (QJSEngine::objectOwnership(q) == QJSEngine::JavaScriptOwnership)
        QJSEngine::setObjectOwnership(qobj, QJSEngine::JavaScriptOwnership);
    return qobj;
}}

void {type.MFn(Ns | Name)}::connectNotify(const QMetaMethod &signal)
{{
    QString signalTag = signal.tag();
    {implementation[new(EventSubscribers)]}
}}

{(!type.Implements<INotifyPropertyChanged>() ? Wrap : $@"{Wrap}
void {type.MFn(Ns | Name | Private)}::onPropertyChanged(const QString &propertyName)
{{
    Q_ASSERT_X(q->thread() == QThread::currentThread(),
        ""Property Notifier"", ""Emit must run on the object's thread"");
    {implementation[new(PropertyNotifiers)]}
}}
")}
{implementation[new(EventHandlers)]}
{implementation[new(MethodsImplementation)]}";
            return Ok;
        }
    }
}
