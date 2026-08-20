// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#include <metadata_loader.h>

#include <QDotNetDynamicObject>

#include <QByteArray>
#include <QDir>
#include <QFile>
#include <QHash>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QJsonValue>
#include <QMessageLogger>
#include <QMetaType>
#include <QtDebug>

namespace {

using BaseClass = QDotNetDynamicObject::BaseClass;
using ModelOverride = QDotNetDynamicObject::ModelOverride;
using ModelOverrides = QDotNetDynamicObject::ModelOverrides;

struct BuiltInType
{
    QMetaType::Type id = {};
    QDotNetParameter inbound = {};
    QDotNetParameter outbound = {};
    static BuiltInType Void;
};

template <typename T>
BuiltInType builtInType()
{
    return { static_cast<QMetaType::Type>(QMetaType::fromType<T>().id()),
             QDotNetInbound<T>::Parameter, QDotNetOutbound<T>::Parameter };
}

template <>
BuiltInType builtInType<QVariant>()
{
    return { QMetaType::Type::QVariant, QDotNetInbound<QDotNetObject>::Parameter,
             QDotNetOutbound<QDotNetObject>::Parameter };
}

BuiltInType BuiltInType::Void = builtInType<void>();

QHash<QString, BuiltInType> builtInTypes = {
    { "void", builtInType<void>() },           { "bool", builtInType<bool>() },
    { "qint8", builtInType<qint8>() },         { "quint8", builtInType<quint8>() },
    { "qint16", builtInType<qint16>() },       { "quint16", builtInType<quint16>() },
    { "qint32", builtInType<qint32>() },       { "quint32", builtInType<quint32>() },
    { "qint64", builtInType<qint64>() },       { "quint64", builtInType<quint64>() },
    { "float", builtInType<float>() },         { "double", builtInType<double>() },
    { "QChar", builtInType<QChar>() },         { "QString", builtInType<QString>() },
    { "QDateTime", builtInType<QDateTime>() }, { "QUrl", builtInType<QUrl>() },
    { "QVariant", builtInType<QVariant>() },
};

QHash<QString, BaseClass> modelBaseClasses = { { "object", BaseClass::Object },
                                               { "model", BaseClass::Model },
                                               { "listModel", BaseClass::ListModel },
                                               { "tableModel", BaseClass::TableModel } };

QHash<QString, ModelOverride> modelOverrides = {
    { "rowCount", ModelOverride::RowCount },
    { "columnCount", ModelOverride::ColumnCount },
    { "roleNames", ModelOverride::RoleNames },
    { "canFetchMore", ModelOverride::CanFetchMore },
    { "flags", ModelOverride::Flags },
    { "hasChildren", ModelOverride::HasChildren },
    { "index", ModelOverride::Index },
    { "parent", ModelOverride::Parent },
    { "sibling", ModelOverride::Sibling },
    { "buddy", ModelOverride::Buddy },
    { "data", ModelOverride::Data },
    { "headerData", ModelOverride::HeaderData },
    { "insertRows", ModelOverride::InsertRows },
    { "insertColumns", ModelOverride::InsertColumns },
    { "moveRows", ModelOverride::MoveRows },
    { "moveColumns", ModelOverride::MoveColumns },
    { "removeRows", ModelOverride::RemoveRows },
    { "removeColumns", ModelOverride::RemoveColumns },
    { "sort", ModelOverride::Sort },
    { "fetchMore", ModelOverride::FetchMore },
    { "setData", ModelOverride::SetData },
    { "setHeaderData", ModelOverride::SetHeaderData },
};

bool warn(const QString &msg, const QJsonValue &json = {})
{
    qWarning() << "Metadata Loader:" << msg;
    if (!json.isNull())
        qWarning() << json;
    return false;
}

QString camelStr(const QString &pascalStr)
{
    return QString(pascalStr).replace(0, 1, pascalStr[0].toLower());
}

bool loadMethod(QMetaObjectBuilder *typeDef, const QJsonObject &jsonMethod)
{
    auto methodName = jsonMethod["dotNet"]["name"].toString();

    int token = 0;
    if (const auto &jsonToken = jsonMethod["dotNet"]["metadataToken"]; jsonToken.isDouble())
        token = jsonToken.toInt();

    auto qtMethod = camelStr(methodName);
    if (const auto &jsonQtName = jsonMethod["qt"]["name"]; jsonQtName.isString())
        qtMethod = jsonQtName.toString();

    auto qtReturnType = jsonMethod["qt"]["returnType"].toString();

    QList<QDotNetParameter> paramTypes = { builtInTypes[qtReturnType].inbound };
    QStringList qtParamTypes;
    for (const auto &jsonParam : jsonMethod["qt"]["parameters"].toArray()) {
        QString qtParamType;
        if (jsonParam.isString())
            qtParamType = jsonParam.toString();
        else
            qtParamType = jsonParam.toObject()["type"].toString();
        paramTypes << builtInTypes[qtParamType].outbound;
        qtParamTypes << qtParamType;
    }
    qtMethod.append('(').append(qtParamTypes.join(',')).append(')');

    auto methodDef = typeDef->addMethod(qtMethod.toUtf8(), qtReturnType.toUtf8());

    if (!QDotNetDynamicObject::addMethod(typeDef, methodName, token, methodDef, paramTypes))
        return warn("Error calling 'addMethod'", jsonMethod);

    return true;
}

bool loadEvent(QMetaObjectBuilder *typeDef, const QJsonObject &jsonEvent)
{
    auto eventName = jsonEvent["dotNet"]["name"].toString();

    auto qtSignal = camelStr(eventName);
    if (const auto &jsonSignal = jsonEvent["qt"]["signal"]; jsonSignal.isString())
        qtSignal = jsonSignal.toString();
    qtSignal.append("(QObject *)");

    if (!QDotNetDynamicObject::addEvent(typeDef, eventName, typeDef->addSignal(qtSignal.toUtf8())))
        return warn("Error calling 'addEvent'", jsonEvent);

    return true;
}

bool loadProperty(QMetaObjectBuilder *typeDef, const QJsonObject &jsonProp)
{
    auto propName = jsonProp["dotNet"]["name"].toString();

    auto propQtName = camelStr(propName);
    if (const auto &jsonQtName = jsonProp["qt"]["name"]; jsonQtName.isString())
        propQtName = jsonQtName.toString();

    auto propQtType = jsonProp["qt"]["type"].toString();
    auto propType = builtInTypes[propQtType];

    auto propDef = typeDef->addProperty(propQtName.toUtf8(), propQtType.toUtf8());

    auto getType = propType.inbound;
    if (const auto &flag = jsonProp["dotNet"]["hasGet"]; flag.isBool() && !flag.toBool()) {
        propDef.setReadable(false);
        getType = BuiltInType::Void.inbound;
    }

    auto setType = propType.outbound;
    if (const auto &flag = jsonProp["dotNet"]["hasSet"]; flag.isBool() && !flag.toBool()) {
        propDef.setWritable(false);
        setType = BuiltInType::Void.outbound;
    }

    auto propNotifySignal = QString(propQtName).append("Changed()");
    if (const auto &flag = jsonProp["dotNet"]["isNotifiable"]; !flag.isBool() || flag.toBool())
        propDef.setNotifySignal(typeDef->addSignal(propNotifySignal.toUtf8()));

    if (!QDotNetDynamicObject::addProperty(typeDef, propName, propDef, getType, setType))
        return warn("Error calling 'addProperty'", jsonProp);

    return true;
}

bool loadType(const QJsonObject &jsonType, std::function<void()> qml_register_types)
{
    auto typeName = jsonType["dotNet"]["name"].toString();

    auto qualifiedTypeName = jsonType["dotNet"]["assemblyQualifiedName"].toString();

    auto assemblyFile = jsonType["dotNet"]["assemblyFile"].toString();

    auto isQmlElement = false;
    if (const auto &jsonQml = jsonType["qt"]["isQmlElement"]; jsonQml.isBool())
        isQmlElement = jsonQml.toBool();

    auto baseClass = jsonType["qt"]["model"].isNull() ? BaseClass::Object : BaseClass::Model;
    if (const auto &jsonBaseClass = jsonType["qt"]["model"]["baseClass"]; jsonBaseClass.isString())
        baseClass = modelBaseClasses[jsonBaseClass.toString()];

    ModelOverrides overrides = ModelOverride::None;
    if (const auto &jsonOverrides = jsonType["qt"]["model"]["overrides"]; jsonOverrides.isArray()) {
        for (const auto &jsonOverride : jsonOverrides.toArray())
            overrides |= modelOverrides[jsonOverride.toString()];
    }

    auto *typeDef =
            QDotNetDynamicObject::defineType(typeName, qualifiedTypeName, assemblyFile,
                                             isQmlElement, baseClass, overrides);

    if (const auto &jsonProps = jsonType["properties"]; jsonProps.isArray()) {
        for (const auto &jsonProp : jsonProps.toArray()) {
            if (!loadProperty(typeDef, jsonProp.toObject()))
                return false;
        }
    }

    if (const auto &jsonEvents = jsonType["events"]; jsonEvents.isArray()) {
        for (const auto &jsonEvent : jsonEvents.toArray()) {
            if (!loadEvent(typeDef, jsonEvent.toObject()))
                return false;
        }
    }

    if (const auto &jsonMethods = jsonType["methods"]; jsonMethods.isArray()) {
        for (const auto &jsonMethod : jsonMethods.toArray()) {
            if (!loadMethod(typeDef, jsonMethod.toObject()))
                return false;
        }
    }

    if (!jsonType["qt"]["qml"].isObject())
        return QDotNetDynamicObject::buildType(typeDef) != nullptr;

    auto qmlName = jsonType["qt"]["qml"]["name"].toString();
    auto qmlModule = jsonType["qt"]["qml"]["module"].toString();
    auto qmlRevMajor = jsonType["qt"]["qml"]["moduleRevisionMajor"].toInt();
    auto qmlRevMinor = jsonType["qt"]["qml"]["moduleRevisionMinor"].toInt();

    QDotNetDynamicObject::buildType(typeDef, qmlName, qmlModule, qmlRevMajor, qmlRevMinor,
                                    qml_register_types);

    return true;
}

bool validateMetadata(const QJsonDocument &metadata)
{
    // Placeholder for a future implementation of native-side validation of the metadata file.
    //   * This could be a full JSON schema validation or, in the case of generated metadata files,
    //     a faster check, for example: verifying a signature added to the file by the 'qbgen' tool.
    return true;
}

} // namespace

bool QtDotNet::loadTypeMetadata(const QString &appDirPath, std::function<void()> qml_register_types)
{
    QFile metadataFile(QDir(appDirPath).filePath("qt_dotnet_types.json"));
    if (!metadataFile.open(QIODevice::ReadOnly))
        return warn("Error loading metadata file");

    auto metadataBytes = metadataFile.readAll();
    const auto &jsonMetadata = QJsonDocument::fromJson(metadataBytes);
    if (!validateMetadata(jsonMetadata))
        return false;

    const auto &jsonTypes = jsonMetadata.object()["types"].toArray();
    for (const auto &jsonTypesItem : jsonTypes) {
        if (!loadType(jsonTypesItem.toObject(), qml_register_types))
            return false;
    }

    return true;
}
