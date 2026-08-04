// Copyright (C) 2025 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

using System.ComponentModel;
using System.Reflection;

namespace Qt.Bridge.CodeGeneration.Rules.SourceCode.Class
{
    using MetaFunctions;
    using Extensions;
    using Quick;
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
            var isQmlElement = type.IsQmlElement();
            if (isQmlElement) {
                if (Root.GetPlaceholder(IncludeDirs) is not { } includeDirs)
                    return Error();
                includeDirs += $"include_directories({Hpp}/{type.MFn(Ns | Dir)})";

                if (type.GetPlaceholder(Includes) is not { } includes)
                    return Error();
                includes += "#include <QtQml/qqmlregistration.h>";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            // Precompute QML macros and validate provided Name (fail fast on invalid)
            var elementName = isQmlElement ? type.QmlElementName() : null;
            var nameProvided = isQmlElement && type.QtAttributeData<QmlElementAttribute>()
                .Any(a => a.HasProperty(nameof(QmlElementAttribute.Name)));

            if (nameProvided && string.IsNullOrWhiteSpace(elementName)) {
                return Error($"QmlElement.Name on '{type.MFn(Src | Fqn)}' is invalid. It must "
                    + "start with an uppercase letter and contain only letters, digits, or '_'.");
            }

            var qmlElementMacro = Wrap.ToString();
            if (isQmlElement) {
                qmlElementMacro = !string.IsNullOrEmpty(elementName)
                    ? $"QML_NAMED_ELEMENT({elementName})"
                    : "QML_ELEMENT";
            }
            var qmlSingletonMacro = type.IsQmlSingleton() ? "QML_SINGLETON" : Wrap.ToString();

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
struct QDotNetIsRef<{type.MFn(Ns | Name)}> : std::true_type
{{}};
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
class {type.MFn(Ns | Name)} :
    {publicDecl[(Placeholder)new(QObjectBaseClass) { Content = ["public QObject"] }]},
    {publicDecl[(Placeholder)new(BaseClasses)]}
    public QDotNetObject
{{
    Q_OBJECT
    {qmlElementMacro}
    {qmlSingletonMacro}
    {publicDecl[(Placeholder)new(TypeTraits)]}
public:
    Q_DOTNET_OBJECT({type.MFn(Name)},
        ""{type.MFn(Src | Fqn)}"");

    {publicDecl[(Placeholder)new(CtorDeclarations)]}
    ~{type.MFn(Name)}() override;

    Q_INVOKABLE const QDotNetObject *asDotNetObject() const;

    {publicDecl[(Placeholder)new(PropertyDeclarations)]}
    {publicDecl[(Placeholder)new(MethodDeclarations)]}
    {publicDecl[(Placeholder)new(SignalDeclarations)]}
protected:
    void connectNotify(const QMetaMethod &signal) override;

private:
    {type.MFn(Name | Private)} *d = nullptr;
    friend {type.MFn(Name | Private)};
    {type.MFn(Name | Init)} *i = nullptr;
    friend {type.MFn(Name | Init)};
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

const QDotNetObject *{type.MFn(Ns | Name)}::asDotNetObject() const
{{
    return this;
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
