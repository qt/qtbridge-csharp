// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#include <metadata_loader.h>

#include <native_host.h>

#include <QDotNetDynamicObject>
#include <QDotNetProfiler>

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

#include <cstdint>

namespace {

Q_DOTNET_PROFILE_SCOPE(MetaDataLoader);

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
    Q_DOTNET_PROFILE_FUNC();

    return { static_cast<QMetaType::Type>(QMetaType::fromType<T>().id()),
             QDotNetInbound<T>::Parameter, QDotNetOutbound<T>::Parameter };
}

template <>
BuiltInType builtInType<QVariant>()
{
    Q_DOTNET_PROFILE_FUNC();

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
    Q_DOTNET_PROFILE_FUNC();

    qWarning() << "Metadata Loader:" << msg;
    if (!json.isNull())
        qWarning() << json;
    return false;
}

QString camelStr(const QString &pascalStr)
{
    Q_DOTNET_PROFILE_FUNC();

    return QString(pascalStr).replace(0, 1, pascalStr[0].toLower());
}

bool loadMethod(QMetaObjectBuilder *typeDef, const QJsonObject &jsonMethod)
{
    Q_DOTNET_PROFILE_FUNC();

    const auto dotNet = jsonMethod["dotNet"].toObject();

    int token = 0;
    if (const QJsonValue jsonToken = dotNet["metadataToken"]; jsonToken.isDouble())
        token = jsonToken.toInt();

    const auto qt = jsonMethod["qt"].toObject();
    auto methodName = dotNet["name"].toString();

    auto qtMethod = camelStr(methodName);
    if (const QJsonValue jsonQtName = qt["name"]; jsonQtName.isString())
        qtMethod = jsonQtName.toString();

    auto qtReturnType = qt["returnType"].toString();

    QList<QDotNetParameter> paramTypes = { builtInTypes[qtReturnType].inbound };
    QStringList qtParamTypes;
    for (const QJsonValue jsonParam : qt["parameters"].toArray()) {
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
    Q_DOTNET_PROFILE_FUNC();

    const auto dotNet = jsonEvent["dotNet"].toObject();
    auto eventName = dotNet["name"].toString();

    auto qtSignal = camelStr(eventName);
    if (const QJsonValue jsonSignal = jsonEvent["qt"].toObject()["signal"];
        jsonSignal.isString())
        qtSignal = jsonSignal.toString();
    qtSignal.append("(QObject *)");

    if (!QDotNetDynamicObject::addEvent(typeDef, eventName, typeDef->addSignal(qtSignal.toUtf8())))
        return warn("Error calling 'addEvent'", jsonEvent);

    return true;
}

bool loadProperty(QMetaObjectBuilder *typeDef, const QJsonObject &jsonProp)
{
    Q_DOTNET_PROFILE_FUNC();

    const auto dotNet = jsonProp["dotNet"].toObject();
    const auto qt = jsonProp["qt"].toObject();

    auto propName = dotNet["name"].toString();

    auto propQtName = camelStr(propName);
    if (const QJsonValue jsonQtName = qt["name"]; jsonQtName.isString())
        propQtName = jsonQtName.toString();

    auto propQtType = qt["type"].toString();
    auto propType = builtInTypes[propQtType];

    auto propDef = typeDef->addProperty(propQtName.toUtf8(), propQtType.toUtf8());

    auto getType = propType.inbound;
    if (const QJsonValue flag = dotNet["hasGet"]; flag.isBool() && !flag.toBool()) {
        propDef.setReadable(false);
        getType = BuiltInType::Void.inbound;
    }

    auto setType = propType.outbound;
    if (const QJsonValue flag = dotNet["hasSet"]; flag.isBool() && !flag.toBool()) {
        propDef.setWritable(false);
        setType = BuiltInType::Void.outbound;
    }

    auto propNotifySignal = QString(propQtName).append("Changed()");
    if (const QJsonValue flag = dotNet["isNotifiable"]; !flag.isBool() || flag.toBool())
        propDef.setNotifySignal(typeDef->addSignal(propNotifySignal.toUtf8()));

    if (!QDotNetDynamicObject::addProperty(typeDef, propName, propDef, getType, setType))
        return warn("Error calling 'addProperty'", jsonProp);

    return true;
}

bool loadCollectionModel(QMetaObjectBuilder *typeDef, const QJsonObject &jsonCollection)
{
    Q_DOTNET_PROFILE_FUNC();

    QDotNetDynamicObject::CollectionModel model;
    model.countMethod = jsonCollection["countMethod"].toString();
    model.itemMethod = jsonCollection["itemMethod"].toString();
    model.isObservable = jsonCollection["isObservable"].toBool();
    for (const QJsonValue jsonRole : jsonCollection["roles"].toArray()) {
        const auto role = jsonRole.toObject();
        model.roles.append({
            role["dotNet"].toObject()["name"].toString(),
            role["qt"].toObject()["name"].toString().toUtf8()
        });
    }
    return QDotNetDynamicObject::addCollectionModel(typeDef, model)
            || warn("Error calling 'addCollectionModel'", jsonCollection);
}

bool loadEnum(QMetaObjectBuilder *typeDef, const QJsonObject &jsonEnum)
{
    Q_DOTNET_PROFILE_FUNC();

    constexpr double minValue = INT32_MIN;
    constexpr double maxValue = INT32_MAX;

    auto values = typeDef->addEnumerator("Values");
    for (auto it = jsonEnum.constKeyValueBegin(); it != jsonEnum.constKeyValueEnd(); ++it) {
        if (it->second.isDouble()) {
            if (double value = it->second.toDouble(); minValue <= value && value <= maxValue)
                values.addKey(it->first.toString().toUtf8(), static_cast<qint32>(value));
            else
                warn("Enum value overflow", it->second);
        } else {
            warn("Enum value format", it->second);
        }
    }

    if (!QDotNetDynamicObject::setEnum(typeDef, values))
        return warn("Error setting enumerator", jsonEnum);

    return true;
}

bool loadType(const QJsonObject &jsonType, const std::function<void()> &qmlRegisterTypes)
{
    Q_DOTNET_PROFILE_FUNC();

    const auto dotNet = jsonType["dotNet"].toObject();
    const auto qt = jsonType["qt"].toObject();

    const QJsonValue jsonModel = qt["model"];
    const auto model = jsonModel.toObject();

    auto typeName = dotNet["name"].toString();

    auto qualifiedTypeName = dotNet["assemblyQualifiedName"].toString();

    auto assemblyFile = dotNet["assemblyFile"].toString();

    auto isQmlElement = false;
    if (const QJsonValue jsonQml = qt["isQmlElement"]; jsonQml.isBool())
        isQmlElement = jsonQml.toBool();

    auto baseClass = !jsonModel.isObject() ? BaseClass::Object : BaseClass::Model;
    if (const QJsonValue jsonBaseClass = model["baseClass"]; jsonBaseClass.isString())
        baseClass = modelBaseClasses[jsonBaseClass.toString()];

    ModelOverrides overrides = ModelOverride::None;
    if (const QJsonValue jsonOverrides = model["overrides"]; jsonOverrides.isArray()) {
        for (const QJsonValue jsonOverride : jsonOverrides.toArray())
            overrides |= modelOverrides[jsonOverride.toString()];
    }

    auto *typeDef =
            QDotNetDynamicObject::defineType(typeName, qualifiedTypeName, assemblyFile,
                                             isQmlElement, baseClass, overrides);

    if (const QJsonValue collection = model["collection"]; collection.isObject()) {
        if (!loadCollectionModel(typeDef, collection.toObject()))
            return false;
    }

    if (const QJsonValue jsonProps = jsonType["properties"]; jsonProps.isArray()) {
        for (const QJsonValue jsonProp : jsonProps.toArray()) {
            if (!loadProperty(typeDef, jsonProp.toObject()))
                return false;
        }
    }

    if (const QJsonValue jsonEvents = jsonType["events"]; jsonEvents.isArray()) {
        for (const QJsonValue jsonEvent : jsonEvents.toArray()) {
            if (!loadEvent(typeDef, jsonEvent.toObject()))
                return false;
        }
    }

    if (const QJsonValue jsonMethods = jsonType["methods"]; jsonMethods.isArray()) {
        for (const QJsonValue jsonMethod : jsonMethods.toArray()) {
            if (!loadMethod(typeDef, jsonMethod.toObject()))
                return false;
        }
    }

    if (const QJsonValue jsonEnum = qt["enum"]; jsonEnum.isObject()) {
        if (!loadEnum(typeDef, jsonEnum.toObject()))
            return false;
    }

    const QJsonValue jsonQmlInfo = qt["qml"];
    if (!jsonQmlInfo.isObject())
        return QDotNetDynamicObject::buildType(typeDef) != nullptr;

    const auto qml = jsonQmlInfo.toObject();
    auto qmlName = qml["name"].toString();
    auto qmlModule = qml["module"].toString();
    auto qmlRevMajor = qml["moduleRevisionMajor"].toInt();
    auto qmlRevMinor = qml["moduleRevisionMinor"].toInt();
    auto qmlSingleton = qml["singleton"].toBool(false);

    QDotNetDynamicObject::buildType(typeDef, qmlName, qmlModule, qmlRevMajor, qmlRevMinor,
                                    qmlRegisterTypes, qmlSingleton);

    return true;
}

bool validateMetadata(const QJsonDocument &metadata)
{
    Q_DOTNET_PROFILE_FUNC();

    // Placeholder for a future implementation of native-side validation of the metadata file.
    //   * This could be a full JSON schema validation or, in the case of generated metadata files,
    //     a faster check, for example: verifying a signature added to the file by the 'qbgen' tool.
    return true;
}

} // namespace

bool QtDotNet::loadTypeMetadata(const QString &appDirPath, const std::function<void()> &qmlRegisterTypes)
{
    Q_DOTNET_PROFILE_FUNC();

    if (!QtDotNet::nativeHostManifestIsValid())
        return warn("Application manifest is not valid");

    // An empty name means the application ships no metadata, explicitly supported.
    const auto *metadataName = QtDotNet::nativeHostMetadataName();
    if (!metadataName)
        return true;

    QFile metadataFile(QDir(appDirPath).filePath(QString::fromUtf8(metadataName)));
    if (!metadataFile.open(QIODevice::ReadOnly))
        return warn("Error loading metadata file");

    auto metadataBytes = metadataFile.readAll();
    if (!QtDotNet::nativeHostVerifyMetadata(metadataBytes))
        return warn("Type metadata does not match the checksum in the application manifest");

    const auto &jsonMetadata = QJsonDocument::fromJson(metadataBytes);
    if (!validateMetadata(jsonMetadata))
        return false;

    const auto &jsonTypes = jsonMetadata.object()["types"].toArray();
    for (const QJsonValue jsonTypesItem : jsonTypes) {
        if (!loadType(jsonTypesItem.toObject(), qmlRegisterTypes))
            return false;
    }

    return true;
}
