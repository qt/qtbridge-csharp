/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System.ComponentModel;
using System.Reflection;
using System.Text;
using Qt.Quick;

namespace Qt.Bridge.CodeGeneration.Rules.Class
{
    using Extensions;
    using static Placeholders;
    using static Traits;

    public class GenerateQmlElement : GenerateClass
    {
        public override int Priority => base.Priority + 1;
        public override bool Matches(MemberInfo src)
            => src is Type type && base.Matches(type) && type.IsQmlElement();
        public override Result Execute(MemberInfo src)
        {
            if (src is not Type type)
                return Error();

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(Includes) is not { } includes)
                return Error();
            includes += "#include <QQmlListProperty>";
            includes += "#include <qqml.h>";
            if (type.Implements<IQmlElement>())
                includes += "#include <QQmlParserStatus>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.Implements<IQmlElement>()) {
                if (type.GetPlaceholder(BaseClasses) is not { } baseClasses)
                    return Error();
                baseClasses += "public QQmlParserStatus,";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(TypeTraits) is not { } traits)
                return Error();
            traits += @"Q_CLASSINFO(""DefaultProperty"", ""nestedQmlElements"")";
            if (type.Implements<IQmlElement>())
                traits += @"Q_INTERFACES(QQmlParserStatus)";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PropertyDeclarations) is not { } properties)
                return Error();
            properties += $@"
Q_PROPERTY(QQmlListProperty<QObject> nestedQmlElements READ nestedQmlElements CONSTANT)
QQmlListProperty<QObject> nestedQmlElements();
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodDeclarations) is not { } methods)
                return Error();
            if (type.Implements<IQmlElement>()) {
                methods += $@"
void classBegin() override;
void componentComplete() override;
{Blank}";
            }

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateIncludes) is not { } privateIncludes)
                return Error();
            privateIncludes += "#include <QList>";

            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(PrivateMemberDeclarations) is not { } privateMembers)
                return Error();
            privateMembers += $@"
QList<QObject *> nestedQmlElements;
";
            ////////////////////////////////////////////////////////////////////////////////////////
            //
            if (type.GetPlaceholder(MethodsImplementation) is not { } implementation)
                return Error();
            implementation += $@"
QQmlListProperty<QObject> {type.MFn(Ns | Name)}::nestedQmlElements()
{{
    return QQmlListProperty<QObject>(this, &d->nestedQmlElements);
}}
{Blank}";

            if (type.Implements<IQmlElement>()) {
                implementation += $@"
void {type.MFn(Ns | Name)}::classBegin()
{{
    qmlClassBegin();
}}

void {type.MFn(Ns | Name)}::componentComplete()
{{
    QList<const QDotNetObject *> dotNetObjs;
    for (QObject *qObj : d->nestedQmlElements) {{
        if (!qObj)
            continue;
        const QDotNetObject *dnObj = Convert::asDotNetObject(qObj);
        if (!dnObj || !dnObj->isValid())
            continue;
        dotNetObjs << dnObj;
    }}
    QDotNetArray<QDotNetRef> nestedObjs(dotNetObjs.size());
    for (int i = 0; i < dotNetObjs.size(); ++i)
        nestedObjs[i] = *dotNetObjs[i];
    auto objArray = nestedObjs.cast<{TypeOf<object[]>().MFn(Ns | Name)}>();
    qmlComponentComplete(&objArray);
}}
{Blank}";
            }

            return Ok;
        }
    }
}
