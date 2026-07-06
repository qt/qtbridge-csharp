// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetarray.h"
#include "qdotnetconvert.h"
#include "qdotnetevent.h"
#include "qdotnetobject.h"
#include "qdotnetparameter.h"
#include "qdotnetreflection.h"
#include "qdotnettype.h"

#include <functional>

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QAbstractItemModel>
#include <QCoreApplication>
#include <QDir>
#include <QFile>
#include <QJSEngine>
#include <QList>
#include <QMap>
#include <QMetaObject>
#include <QObject>
#include <QQmlListProperty>
#include <QQmlParserStatus>
#include <QSet>
#include <QtCore/private/qmetaobjectbuilder_p.h>
#include <QtQml/qqmlprivate.h>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetDynamicObject : public QAbstractItemModel,
                             public QQmlParserStatus,
                             public QDotNetObject
{
    struct DynamicType;
    struct DynamicMethod;
    struct DynamicProperty;
    struct DynamicEvent;
    struct DynamicModel;
    struct DynamicQmlElement;
    struct Parameter;

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Public definitions
public:

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Dynamic meta-type authoring

    enum BaseClass { Object, Model, ListModel, TableModel };

    enum ModelOverride {
        None = 0,
        RowCount = 1 << 0,
        ColumnCount = 1 << 1,
        RoleNames = 1 << 2,
        CanFetchMore = 1 << 3,
        Flags = 1 << 4,
        HasChildren = 1 << 5,
        Index = 1 << 6,
        Parent = 1 << 7,
        Sibling = 1 << 8,
        Buddy = 1 << 9,
        Data = 1 << 10,
        HeaderData = 1 << 11,
        InsertRows = 1 << 12,
        InsertColumns = 1 << 13,
        MoveRows = 1 << 14,
        MoveColumns = 1 << 15,
        RemoveRows = 1 << 16,
        RemoveColumns = 1 << 17,
        Sort = 1 << 18,
        FetchMore = 1 << 19,
        SetData = 1 << 20,
        SetHeaderData = 1 << 21
    };

    Q_DECLARE_FLAGS(ModelOverrides, ModelOverride)

    static QMetaObjectBuilder *defineType(const QString &typeName, const QString &qualifiedTypeName,
                                          const QString &assemblyFile, const QString &appDirPath,
                                          bool isQmlElement,
                                          BaseClass baseClass = BaseClass::Object,
                                          ModelOverrides modelOverrides = ModelOverride::None)
    {
        if (!QCoreApplication::startingUp())
            return nullptr;

        auto assemblyPath = QDir(appDirPath).filePath(assemblyFile);
        if (!QFile::exists(assemblyPath)) {
            qWarning() << "QDotNetDynamicObject: File not found:" << assemblyPath;
            return nullptr;
        }

        auto *typeDef = new QMetaObjectBuilder();
        auto *type = dynamicTypesByDef[typeDef] = new DynamicType();
        type->name = typeName;
        type->assemblyPath = assemblyPath;
        type->assemblyQualifiedName = qualifiedTypeName;
        dynamicTypesByName[type->assemblyQualifiedName] = type;
        type->isQmlElement = isQmlElement;
        type->baseClass = baseClass;
        type->modelOverrides = modelOverrides;
        switch (baseClass) {
        case BaseClass::Model:
        case BaseClass::ListModel:
        case BaseClass::TableModel:
            typeDef->setSuperClass(&QAbstractItemModel::staticMetaObject);
            break;
        default:
            typeDef->setSuperClass(&QObject::staticMetaObject);
            break;
        }
        typeDef->setClassName(type->assemblyQualifiedName.toUtf8());
        typeDef->setStaticMetacallFunction(staticMetacall);

        if (isQmlElement) {
            typeDef->addClassInfo("DefaultProperty", "nestedQmlElements");
            auto propDef = typeDef->addProperty("nestedQmlElements", "QQmlListProperty<QObject>",
                                                QMetaType::fromType<QQmlListProperty<QObject>>());
            propDef.setWritable(false);
            propDef.setConstant(true);
            type->idxNestedQmlElements = propDef.index();
        }

        return typeDef;
    }

    static bool addMethod(QMetaObjectBuilder *typeDef, const QString &methodName, int token,
                          const QMetaMethodBuilder &methodDef, QList<QDotNetParameter> params)
    {
        if (!QCoreApplication::startingUp())
            return false;

        if (params.empty()) {
            qWarning() << "QDotNetDynamicObject: Empty method parameter list";
            return false;
        }

        const auto &itDynamicType = dynamicTypesByDef.find(typeDef);
        if (itDynamicType == dynamicTypesByDef.end()) {
            qWarning() << "QDotNetDynamicObject: Unrecognized type definition:" << typeDef;
            return false;
        }
        auto *type = *itDynamicType;
        auto *method = new DynamicMethod();
        type->methods[methodDef.index()] = method;
        method->declaringType = type;
        method->name = methodName;
        method->token = token;
        method->params = { params.begin(), params.end() };

        return true;
    }

    static bool addProperty(QMetaObjectBuilder *typeDef, const QString &propertyName,
                            const QMetaPropertyBuilder &propertyDef, QDotNetParameter getReturnType,
                            QDotNetParameter setValueType)
    {
        if (!QCoreApplication::startingUp())
            return false;

        const auto &itDynamicType = dynamicTypesByDef.find(typeDef);
        if (itDynamicType == dynamicTypesByDef.end()) {
            qWarning() << "QDotNetDynamicObject: Unrecognized type definition:" << typeDef;
            return false;
        }

        auto *type = *itDynamicType;
        auto *property = new DynamicProperty();
        type->properties[propertyDef.index()] = property;
        property->declaringType = type;
        property->name = propertyName;
        type->propertiesByName[property->name] = property;
        property->params = { getReturnType, setValueType };
        property->isReadable = propertyDef.isReadable();
        property->isWriteable = propertyDef.isWritable();
        if (propertyDef.hasNotifySignal())
            property->idxNotifySignal = propertyDef.notifySignal().index();

        return true;
    }

    static bool addEvent(QMetaObjectBuilder *typeDef, const QString &eventName,
                         const QMetaMethodBuilder &signalDef)
    {
        if (!QCoreApplication::startingUp())
            return false;

        const auto &itDynamicType = dynamicTypesByDef.find(typeDef);
        if (itDynamicType == dynamicTypesByDef.end()) {
            qWarning() << "QDotNetDynamicObject: Unrecognized type definition:" << typeDef;
            return false;
        }

        if (signalDef.parameterTypes().count() != 1) {
            qWarning() << "QDotNetDynamicObject: Incompatible signal:" << signalDef.signature();
            return false;
        }

        auto *type = *itDynamicType;
        auto *event = new DynamicEvent();
        type->events.append(event);
        event->declaringType = type;
        event->name = eventName;
        event->idxEventSignal = signalDef.index();

        return true;
    }

    static const QMetaObject *buildType(QMetaObjectBuilder *typeDef)
    {
        return buildType(typeDef, "", "", 0, 0, nullptr);
    }

    static const QMetaObject *buildType(QMetaObjectBuilder *typeDef, const QString &qmlName,
                                        const QString &qmlUri, int major, int minor,
                                        std::function<void()> qml_register_types = nullptr)
    {
        using namespace QQmlPrivate;

        if (!QCoreApplication::startingUp())
            return nullptr;

        const auto &itDynamicType = dynamicTypesByDef.find(typeDef);
        if (itDynamicType == dynamicTypesByDef.end()) {
            qWarning() << "QDotNetDynamicObject: Unrecognized type definition:" << typeDef;
            return nullptr;
        }
        auto *type = *itDynamicType;

        addMethod(typeDef, "ToString", 0, typeDef->addMethod("toString()", "QString"),
                  { { QDotNetInbound<QString>::Parameter } });
        addMethod(typeDef, "Equals", 0, typeDef->addMethod("equals(QVariant)", "bool"),
                  { { QDotNetInbound<bool>::Parameter, QDotNetOutbound<QDotNetRef>::Parameter } });
        auto asDotNetObject = typeDef->addMethod("asDotNetObject()", "const QDotNetObject *");
        type->idxAsDotNetObject = asDotNetObject.index();

        const auto *metaObject = typeDef->toMetaObject();
        type->metaObject = metaObject;
        typeDefs[metaObject] = typeDef;

        if (qmlName.isEmpty())
            return metaObject;

        auto utf8 = qmlName.toUtf8();
        QByteArray qmlNameUtf8(utf8.length(), Qt::Uninitialized);
        qmlNameUtf8.assign(utf8).nullTerminate();

        utf8 = qmlUri.toUtf8();
        QByteArray qmlUriUtf8(utf8.length(), Qt::Uninitialized);
        qmlUriUtf8.assign(utf8).nullTerminate();

        RegisterType t{};
        t.structVersion = RegisterType::StructVersion::CurrentVersion;
        t.metaObject = metaObject;
        t.elementName = qmlNameUtf8;
        t.uri = qmlUriUtf8;
        t.version = QTypeRevision::fromVersion(major, minor);
        t.revision = QTypeRevision::zero();
        t.objectSize = sizeof(QDotNetDynamicObject);
        t.create = QDotNetDynamicObject::createObject;
        t.userdata = const_cast<DynamicType *>(type);
        t.typeId = QmlMetaType<QDotNetDynamicObject>::self();
        t.listId = QmlMetaType<QDotNetDynamicObject>::list();
        t.parserStatusCast = StaticCastSelector<QQmlParserStatus>::cast();
        t.valueSourceCast = StaticCastSelector<QQmlPropertyValueSource>::cast();
        t.valueInterceptorCast = StaticCastSelector<QQmlPropertyValueInterceptor>::cast();
        t.finalizerCast = StaticCastSelector<QQmlFinalizerHook>::cast();
        t.attachedPropertiesFunction = qmlAttachedPropertiesFunction(nullptr, metaObject);
        t.attachedPropertiesMetaObject = metaObject;

        qmlregister(RegistrationType::TypeRegistration, &t);
        if (qml_register_types)
            qml_register_types();
        else
            qmlRegisterModule(qmlUriUtf8, major, minor);

        return metaObject;
    }

    //// END Dynamic meta-type authoring
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Dynamic object life-cycle

#define RESOLVE_FUNC(Context, Name) method(#Name, Context->fn##Name)

    QDotNetDynamicObject() = delete;

    QDotNetDynamicObject(QObject *) = delete;

    ~QDotNetDynamicObject() noexcept override { cleanUp(); }

    QDotNetDynamicObject(const QDotNetDynamicObject &obj) = delete;

    QDotNetDynamicObject(QDotNetDynamicObject &&obj) : type(obj.type)
    {
        moveFrom(obj);

        eventHandlers = obj.eventHandlers;
        for (auto *eventHandler : eventHandlers)
            static_cast<DynamicEventHandler *>(eventHandler)->receiver = this;
        obj.eventHandlers.clear();

        model = obj.model;
        obj.model = nullptr;

        qmlElement = obj.qmlElement;
        obj.qmlElement = nullptr;
    }

    static void *operator new(std::size_t count) { return ::operator new(count); }

    static void *operator new(std::size_t, void *ptr)
    {
        assert(ptr);
        objectPlacementAddrs.insert(ptr);
        return ptr;
    }

    static void operator delete(void *ptr)
    {
        if (objectPlacementAddrs.contains(ptr))
            objectPlacementAddrs.remove(ptr);
        else
            ::operator delete(ptr);
    }

#ifdef Q_CC_MSVC
    static void operator delete(void *, void *)
    {
        // Deliberately empty placement delete operator.
        // Silences MSVC warning C4291: no matching operator delete found
    }
#endif

    static QObject *dispatch(QDotNetRef &dotNetObj, const QString &qualifiedTypeName,
                             const QObject *context = nullptr)
    {
        const auto &itDynamicType = dynamicTypesByName.find(qualifiedTypeName);
        if (itDynamicType == dynamicTypesByName.end()) {
            qWarning() << "QDotNetDynamicObject: Unrecognized type:" << qualifiedTypeName;
            return nullptr;
        }
        auto *type = *itDynamicType;

        QDotNetDynamicObject obj(type, dotNetObj);
        return QDotNetConvert::moveToHeap(obj, context);
    }

    //// END Public definitions
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Private definitions
private:

    QDotNetDynamicObject(DynamicType *type, QDotNetRef &&obj) : type(type)
    {
        moveFrom(obj);
        init();
    }

    QDotNetDynamicObject(DynamicType *type, const QDotNetRef &obj) : type(type)
    {
        copyFrom(obj);
        init();
    }

    void init()
    {
        init(type);
        initEvents();
        initModel();
        initQmlElement();
    }

    static void init(DynamicType *type)
    {
        if (!type->assembly.isValid()) {
            QDotNetAssembly assembly = QtDotNet::call<QDotNetRef, QString>(
                    "System.Reflection.Assembly", "LoadFrom", type->assemblyPath);
            if (!assembly.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR loading assembly:" << type->assemblyPath;
                return;
            }

            QDotNetType typeInfo = assembly.getType(type->name);
            if (!typeInfo.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR getting type:" << type->name;
                return;
            }

            QDotNetModule module = typeInfo.module();
            if (!module.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR accessing type module:" << type->name;
                return;
            }

            type->assembly = assembly;
            type->module = module;
            type->typeInfo = typeInfo;
        }
    }

    void cleanUp()
    {
        cleanUpEvents();
        cleanUpModel();
        cleanUpQmlElement();
    }

    void initEvents()
    {
        for (auto *event : type->events)
            eventHandlers.append(new DynamicEventHandler(this, event));
    }

    void cleanUpEvents()
    {
        for (auto *eventHandler : eventHandlers)
            delete eventHandler;
        eventHandlers.clear();
    }

    void initModel()
    {
        if (model)
            return;

        switch (type->baseClass) {
        case Model:
        case ListModel:
        case TableModel:
            break;
        default:
            return;
        }

        model = new DynamicModel;
        if (type->modelOverrides.testFlag(RowCount))
            RESOLVE_FUNC(model, RowCount);
        if (type->modelOverrides.testFlag(ColumnCount))
            RESOLVE_FUNC(model, ColumnCount);
        if (type->modelOverrides.testFlag(RoleNames))
            RESOLVE_FUNC(model, RoleNames);
        if (type->modelOverrides.testFlag(CanFetchMore))
            RESOLVE_FUNC(model, CanFetchMore);
        if (type->modelOverrides.testFlag(Flags))
            RESOLVE_FUNC(model, Flags);
        if (type->modelOverrides.testFlag(HasChildren))
            RESOLVE_FUNC(model, HasChildren);
        if (type->modelOverrides.testFlag(Index))
            RESOLVE_FUNC(model, Index);
        if (type->modelOverrides.testFlag(Parent))
            RESOLVE_FUNC(model, Parent);
        if (type->modelOverrides.testFlag(Sibling))
            RESOLVE_FUNC(model, Sibling);
        if (type->modelOverrides.testFlag(Buddy))
            RESOLVE_FUNC(model, Buddy);
        if (type->modelOverrides.testFlag(Data))
            RESOLVE_FUNC(model, Data);
        if (type->modelOverrides.testFlag(HeaderData))
            RESOLVE_FUNC(model, HeaderData);
        if (type->modelOverrides.testFlag(InsertRows))
            RESOLVE_FUNC(model, InsertRows);
        if (type->modelOverrides.testFlag(InsertColumns))
            RESOLVE_FUNC(model, InsertColumns);
        if (type->modelOverrides.testFlag(MoveRows))
            RESOLVE_FUNC(model, MoveRows);
        if (type->modelOverrides.testFlag(MoveColumns))
            RESOLVE_FUNC(model, MoveColumns);
        if (type->modelOverrides.testFlag(RemoveRows))
            RESOLVE_FUNC(model, RemoveRows);
        if (type->modelOverrides.testFlag(RemoveColumns))
            RESOLVE_FUNC(model, RemoveColumns);
        if (type->modelOverrides.testFlag(Sort))
            RESOLVE_FUNC(model, Sort);
        if (type->modelOverrides.testFlag(FetchMore))
            RESOLVE_FUNC(model, FetchMore);
        if (type->modelOverrides.testFlag(SetData))
            RESOLVE_FUNC(model, SetData);
        if (type->modelOverrides.testFlag(SetHeaderData))
            RESOLVE_FUNC(model, SetHeaderData);
    }

    void cleanUpModel()
    {
        if (!model)
            return;
        delete model;
        model = nullptr;
    }

    void initQmlElement()
    {
        if (!QDotNetObject::type().isAssignableTo<IQmlElement>()) {
            qWarning() << "QDotNetDynamicObject: missing IQmlElement implementation:" << type->name;
            return;
        }
        if (qmlElement || !type->isQmlElement)
            return;
        qmlElement = new DynamicQmlElement;
        RESOLVE_FUNC(qmlElement, QmlClassBegin);
        RESOLVE_FUNC(qmlElement, QmlComponentComplete);
    }

    void cleanUpQmlElement()
    {
        if (!qmlElement)
            return;
        delete qmlElement;
        qmlElement = nullptr;
    }

    static void createObject(void *memory, void *args)
    {
        if (!createInstance.isValid()) {
            createInstance =
                    adapter().resolveStaticMethod("System.Activator", "CreateInstance",
                                                  { { QDotNetInbound<QDotNetRef>::Parameter,
                                                      QDotNetOutbound<QDotNetType>::Parameter } });
            if (!createInstance.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR resolving Activator.CreateInstance method";
                return;
            }
        }

        auto *type = static_cast<DynamicType *>(args);
        init(type);

        QDotNetRef obj = createInstance(type->typeInfo);
        if (!obj.isValid()) {
            qFatal() << "QDotNetDynamicObject: ERROR creating instance:" << type->name;
            return;
        }

        new (memory) QDotNetDynamicObject(type, std::move(obj));
    }

    void classBegin() override
    {
        if (!qmlElement || !qmlElement->fnQmlClassBegin.isValid())
            return;
        qmlElement->fnQmlClassBegin();
    }

    void componentComplete() override
    {
        if (!qmlElement || !qmlElement->fnQmlComponentComplete.isValid())
            return;

        QList<const QDotNetObject *> dotNetObjs;
        for (QObject *qObj : qmlElement->nestedQmlElements) {
            if (!qObj)
                continue;
            const QDotNetObject *dnObj = QDotNetConvert::asDotNetObject(qObj);
            if (!dnObj || !dnObj->isValid())
                continue;
            dotNetObjs << dnObj;
        }
        QDotNetArray<QDotNetRef> nestedObjs(dotNetObjs.size());
        for (int i = 0; i < dotNetObjs.size(); ++i)
            nestedObjs[i] = *dotNetObjs[i];

        qmlElement->fnQmlComponentComplete(nestedObjs);
    }

#undef RESOLVE_FUNC

    //// END Dynamic object life-cycle
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Dynamic object interface

    const QMetaObject *metaObject() const override { return type->metaObject; }

    void *qt_metacast(const char *_clname) override
    {
        if (!_clname)
            return nullptr;
        if (_clname == QDotNetObject::ClassName || !strcmp(_clname, QDotNetObject::ClassName))
            return static_cast<QDotNetObject *>(this);
        if (!strcmp(_clname, "QQmlParserStatus"))
            return static_cast<QQmlParserStatus *>(this);
        if (!strcmp(_clname, "org.qt-project.Qt.QQmlParserStatus"))
            return static_cast<QQmlParserStatus *>(this);
        return QAbstractItemModel::qt_metacast(_clname);
    }

    int qt_metacall(QMetaObject::Call _c, int _id, void **_a) override
    {
        _id = QAbstractItemModel::qt_metacall(_c, _id, _a);
        if (_id < 0)
            return _id;

        auto *m = metaObject();
        int ownMethodCount = m->methodCount() - m->methodOffset();
        int ownPropertyCount = m->propertyCount() - m->propertyOffset();

        switch (_c) {
        case QMetaObject::InvokeMetaMethod:
            if (_id < ownMethodCount)
                staticMetacall(this, _c, _id, _a);
            _id -= ownMethodCount;
            break;
        case QMetaObject::RegisterMethodArgumentMetaType:
            if (_id < ownMethodCount)
                *reinterpret_cast<QMetaType *>(_a[0]) = QMetaType();
            _id -= ownMethodCount;
            break;
        case QMetaObject::ReadProperty:
        case QMetaObject::WriteProperty:
        case QMetaObject::ResetProperty:
        case QMetaObject::BindableProperty:
        case QMetaObject::RegisterPropertyMetaType:
            staticMetacall(this, _c, _id, _a);
            _id -= ownPropertyCount;
            break;
        }
        return _id;
    }

    static void staticMetacall(QObject *obj, QMetaObject::Call call, int id, void **args)
    {
        const auto &itTypeDef = typeDefs.find(obj->metaObject());
        if (itTypeDef == typeDefs.end())
            return;
        const auto *typeDef = *itTypeDef;

        const auto &itDynamicType = dynamicTypesByDef.find(typeDef);
        if (itDynamicType == dynamicTypesByDef.end())
            return;
        const auto *type = *itDynamicType;

        auto *dynObj = static_cast<QDotNetDynamicObject *>(obj);
        if (!dynObj)
            return;

        if (call == QMetaObject::InvokeMetaMethod) {

            if (id == type->idxAsDotNetObject) {
                *reinterpret_cast<const QDotNetObject **>(args[0]) = dynObj;
                return;
            }

            const auto &itMethod = type->methods.find(id);
            if (itMethod != type->methods.end())
                return invokeMetaMethod(dynObj, *itMethod, args);

            return QMetaObject::activate(dynObj, type->metaObject->methodOffset() + id, args);
        }

        if (call == QMetaObject::ReadProperty) {

            if (id == type->idxNestedQmlElements) {
                if (!dynObj->qmlElement) {
                    qFatal() << "QDotNetDynamicObject: ERROR reading nested elements for "
                                "non-element type: " << type->name;
                    return;
                }
                auto *nestedQmlElements = &dynObj->qmlElement->nestedQmlElements;
                *reinterpret_cast<QQmlListProperty<QObject> *>(args[0]) =
                        QQmlListProperty<QObject>(dynObj, nestedQmlElements);
                return;
            }

            const auto &itProp = type->properties.find(id);
            if (itProp == type->properties.end())
                return;
            return readProperty(dynObj, *itProp, args);
        }

        if (call == QMetaObject::WriteProperty) {
            const auto &itProp = type->properties.find(id);
            if (itProp == type->properties.end())
                return;
            return writeProperty(dynObj, *itProp, args);
        }
    }

    static void invokeMetaMethod(QDotNetDynamicObject *obj, DynamicMethod *method, void **args)
    {
        if (!method->methodInfo.isValid()) {
            if (method->token)
                method->methodInfo = method->declaringType->module.resolveMethod(method->token);
            else
                method->methodInfo = method->declaringType->typeInfo.method(method->name);
            if (!method->methodInfo.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR resolving method:" << method->name;
                return;
            }
        }

        QDotNetArray<QDotNetRef> argValues(method->params.count() - 1);
        for (int i = 1; i < method->params.count(); ++i)
            argValues.set(i - 1, readArg(method->params[i], args[i]));

        auto result = method->methodInfo.invoke(*obj, argValues);
        obj->writeResult(method->params[0], args[0], result);
    }

    static void readProperty(QDotNetDynamicObject *obj, DynamicProperty *prop, void **args)
    {
        if (!prop->propertyInfo.isValid()) {
            prop->propertyInfo = prop->declaringType->typeInfo.property(prop->name);
            if (!prop->propertyInfo.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR resolving property:" << prop->name;
                return;
            }
        }
        if (!prop->isReadable) {
            auto js = qjsEngine(obj);
            if (js)
                js->throwError(QString("Property is write-only: %1").arg(prop->name));
            qWarning() << "QDotNetDynamicObject: property is write-only:" << prop->name;
            return;
        }
        auto result = prop->propertyInfo.getValue(*obj);
        obj->writeResult(prop->params[0], args[0], result);
    }

    static void writeProperty(QDotNetDynamicObject *obj, DynamicProperty *prop, void **args)
    {
        if (!prop->propertyInfo.isValid()) {
            prop->propertyInfo = prop->declaringType->typeInfo.property(prop->name);
            if (!prop->propertyInfo.isValid()) {
                qFatal() << "QDotNetDynamicObject: ERROR resolving property:" << prop->name;
                return;
            }
        }
        if (!prop->isWriteable) {
            auto js = qjsEngine(obj);
            if (js)
                js->throwError(QString("Property is read-only: %1").arg(prop->name));
            qWarning() << "QDotNetDynamicObject: property is read-only:" << prop->name;
            return;
        }
        prop->propertyInfo.setValue(*obj, readArg(prop->params[1], args[0]));
    }

    static QDotNetRef readArg(const Parameter &param, void *arg)
    {
        switch (param.unmanagedType) {
        case UnmanagedType::Bool:
            return QDotNetConvert::fromBoolean(*reinterpret_cast<bool *>(arg));
        case UnmanagedType::I1:
            return QDotNetConvert::fromSByte(*reinterpret_cast<qint8 *>(arg));
        case UnmanagedType::U1:
            return QDotNetConvert::fromByte(*reinterpret_cast<quint8 *>(arg));
        case UnmanagedType::I2:
            return QDotNetConvert::fromInt16(*reinterpret_cast<qint16 *>(arg));
        case UnmanagedType::U2:
            if (param.typeName == QDotNetTypeOf<QChar>::TypeName)
                return QDotNetConvert::fromChar(*reinterpret_cast<QChar *>(arg));
            else
                return QDotNetConvert::fromUInt16(*reinterpret_cast<quint16 *>(arg));
        case UnmanagedType::I4:
            return QDotNetConvert::fromInt32(*reinterpret_cast<qint32 *>(arg));
        case UnmanagedType::U4:
            return QDotNetConvert::fromUInt32(*reinterpret_cast<quint32 *>(arg));
        case UnmanagedType::I8:
            return QDotNetConvert::fromInt64(*reinterpret_cast<qint64 *>(arg));
        case UnmanagedType::U8:
            return QDotNetConvert::fromUInt64(*reinterpret_cast<quint64 *>(arg));
        case UnmanagedType::R4:
            return QDotNetConvert::fromSingle(*reinterpret_cast<float *>(arg));
        case UnmanagedType::R8:
            return QDotNetConvert::fromDouble(*reinterpret_cast<double *>(arg));
        case UnmanagedType::LPWStr:
            return QDotNetConvert::fromString(*reinterpret_cast<QString *>(arg));
        default:
            if (param.typeName == QDotNetTypeOf<QDateTime>::TypeName)
                return QDotNetConvert::fromDateTime(*reinterpret_cast<QDateTime *>(arg));
            else if (param.typeName == QDotNetTypeOf<QUrl>::TypeName)
                return QDotNetConvert::fromUri(*reinterpret_cast<QUrl *>(arg));
            else
                return QDotNetConvert::fromVariant(*reinterpret_cast<QVariant *>(arg));
        }
        return nullptr;
    }

    void writeResult(const Parameter &param, void *arg, QDotNetRef &obj)
    {
        switch (param.unmanagedType) {
        case UnmanagedType::Bool:
            *reinterpret_cast<bool *>(arg) = QDotNetConvert::toBoolean(obj);
            break;
        case UnmanagedType::I1:
            *reinterpret_cast<qint8 *>(arg) = QDotNetConvert::toSByte(obj);
            break;
        case UnmanagedType::U1:
            *reinterpret_cast<quint8 *>(arg) = QDotNetConvert::toByte(obj);
            break;
        case UnmanagedType::I2:
            *reinterpret_cast<qint16 *>(arg) = QDotNetConvert::toInt16(obj);
            break;
        case UnmanagedType::U2:
            if (param.typeName == QDotNetTypeOf<QChar>::TypeName)
                *reinterpret_cast<QChar *>(arg) = QDotNetConvert::toChar(obj);
            else
                *reinterpret_cast<quint16 *>(arg) = QDotNetConvert::toUInt16(obj);
            break;
        case UnmanagedType::I4:
            *reinterpret_cast<qint32 *>(arg) = QDotNetConvert::toInt32(obj);
            break;
        case UnmanagedType::U4:
            *reinterpret_cast<quint32 *>(arg) = QDotNetConvert::toUInt32(obj);
            break;
        case UnmanagedType::I8:
            *reinterpret_cast<qint64 *>(arg) = QDotNetConvert::toInt64(obj);
            break;
        case UnmanagedType::U8:
            *reinterpret_cast<quint64 *>(arg) = QDotNetConvert::toUInt64(obj);
            break;
        case UnmanagedType::R4:
            *reinterpret_cast<float *>(arg) = QDotNetConvert::toSingle(obj);
            break;
        case UnmanagedType::R8:
            *reinterpret_cast<double *>(arg) = QDotNetConvert::toDouble(obj);
            break;
        case UnmanagedType::LPWStr:
            *reinterpret_cast<QString *>(arg) = QDotNetConvert::toString(obj);
            break;
        default:
            if (param.typeName == QDotNetTypeOf<QDateTime>::TypeName)
                *reinterpret_cast<QDateTime *>(arg) = QDotNetConvert::toDateTime(obj);
            else if (param.typeName == QDotNetTypeOf<QUrl>::TypeName)
                *reinterpret_cast<QUrl *>(arg) = QDotNetConvert::toUri(obj);
            else
                *reinterpret_cast<QVariant *>(arg) = QDotNetConvert::toVariant(obj, this);
            break;
        }
    }

    //// END Dynamic object interface
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Event handling

    struct DynamicEventHandler : public QDotNetEventHandlerHelper<DynamicEvent>
    {
        DynamicEventHandler(QDotNetDynamicObject *receiver, DynamicEvent *d)
            : QDotNetEventHandlerHelper<DynamicEvent>(d->name, receiver, d)
        {
        }

        void handleEvent(const QString &name, QDotNetObject &sender, QDotNetObject &args) override
        {
            if (!args.isValid())
                return;

            auto *qDynObj = static_cast<QDotNetDynamicObject *>(receiver);

            // NOTE: Do not change invokeMethod using Qt::AutoConnection
            //   * Event-to-signal mapping and notifications need to run on qDynObj's thread.
            QMetaObject::invokeMethod(qDynObj, [=]() mutable {

                if (name == "PropertyChanged" && args.type().is<QDotNetPropertyEvent>()) {
                    auto propChanged = args.cast<QDotNetPropertyEvent>();
                    qDynObj->onPropertyChanged(propChanged.propertyName());
                    args = propChanged.cast<QDotNetObject>();
                } else if (name == "ModelChanged" && args.type().is<QDotNetModelEvent>()) {
                    auto modelChanged = args.cast<QDotNetModelEvent>();
                    qDynObj->onModelDataChanged(modelChanged);
                    args = modelChanged.cast<QDotNetObject>();
                }

                QObject *qEvArgs = QDotNetConvert::objectDispatch(args);
                if (qEvArgs)
                    qDynObj->onEvent(d, qEvArgs);
                delete qEvArgs;
            });
        }
    };

    void onEvent(DynamicEvent *event, QObject *args)
    {
        if (event->idxEventSignal < 0)
            return;
        auto idxSignal = type->metaObject->methodOffset() + event->idxEventSignal;
        auto eventSignal = type->metaObject->method(idxSignal);
        eventSignal.invoke(this, args);
    }

    void onPropertyChanged(const QString &propertyName)
    {
        const auto &itProperty = type->propertiesByName.find(propertyName);
        if (itProperty == type->propertiesByName.end())
            return;
        auto *property = *itProperty;
        if (property->idxNotifySignal < 0)
            return;

        auto idxSignal = type->metaObject->methodOffset() + property->idxNotifySignal;
        auto notifySignal = type->metaObject->method(idxSignal);
        notifySignal.invoke(this);
    }

    //// END Event handling
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN QAbstractItemModel overrides

    QModelIndex setOwnIndex(const QModelIndex &idx) const
    {
        if (idx.model() != nullptr && idx.model() != reinterpret_cast<void *>(-1))
            return QModelIndex(idx);
        return createIndex(idx.row(), idx.column(), idx.internalId());
    }

    int rowCount(const QModelIndex &parent = QModelIndex()) const override
    {
        if (!model || !model->fnRowCount.isValid())
            return {};
        return model->fnRowCount(parent);
    }

    int columnCount(const QModelIndex &parent) const override
    {
        if (!model)
            return {};
        if (!model->fnColumnCount.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
                return parent.isValid() ? 0 : 1;
            default:
                return {};
            }
        }
        return model->fnColumnCount(parent);
    }

    QHash<int, QByteArray> roleNames() const override
    {
        if (!model || !model->fnRoleNames.isValid())
            return QAbstractItemModel::roleNames();

        if (!model->roleNamesByIndex.isEmpty())
            return model->roleNamesByIndex;

        auto a = QDotNetConvert::toArray(method("RoleNames", model->fnRoleNames).invoke(*this));
        for (int i = 0; i + 1 < a.length(); i += 2) {
            auto key = a[i];
            if (!QDotNetConvert::isInt32(key))
                continue;
            auto value = a[i + 1];
            model->roleNamesByIndex.insert(QDotNetConvert::toInt32(key),
                                           QDotNetConvert::toString(value).toUtf8());
        }

        return model->roleNamesByIndex;
    }

    bool canFetchMore(const QModelIndex &parent) const override
    {
        if (!model || !model->fnCanFetchMore.isValid())
            return QAbstractItemModel::canFetchMore(parent);
        return model->fnCanFetchMore(parent);
    }

    Qt::ItemFlags flags(const QModelIndex &idx) const override
    {
        if (!model)
            return QAbstractItemModel::flags(idx);
        if (!model->fnFlags.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
            case BaseClass::TableModel: {
                Qt::ItemFlags f = QAbstractItemModel::flags(idx);
                if (idx.isValid())
                    f |= Qt::ItemNeverHasChildren;
                return f;
            }
            default:
                return QAbstractItemModel::flags(idx);
            }
        }
        return static_cast<Qt::ItemFlags>(model->fnFlags(idx));
    }

    bool hasChildren(const QModelIndex &parent) const override
    {
        if (!model)
            return QAbstractItemModel::hasChildren(parent);
        if (!model->fnHasChildren.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
                return parent.isValid() ? false : (rowCount() > 0);
            case BaseClass::TableModel:
                if (!parent.isValid())
                    return rowCount(parent) > 0 && columnCount(parent) > 0;
                return false;
            default:
                return QAbstractItemModel::hasChildren(parent);
            }
        }
        return model->fnHasChildren(parent);
    }

    QModelIndex index(int row, int column, const QModelIndex &parent = QModelIndex()) const override
    {
        if (!model)
            return {};
        if (!model->fnIndex.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
            case BaseClass::TableModel:
                return hasIndex(row, column, parent) ? createIndex(row, column) : QModelIndex();
            default:
                return {};
            }
        }
        return setOwnIndex(model->fnIndex(row, column, parent));
    }

    QModelIndex parent(const QModelIndex &idx) const override
    {
        if (!model)
            return {};
        if (!model->fnParent.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
            case BaseClass::TableModel:
                return QModelIndex();
            default:
                return {};
            }
        }
        return setOwnIndex(model->fnParent(idx));
    }

    QModelIndex sibling(int row, int column, const QModelIndex &idx) const override
    {
        if (!model)
            return QAbstractItemModel::sibling(row, column, idx);
        if (!model->fnSibling.isValid()) {
            switch (type->baseClass) {
            case BaseClass::ListModel:
            case BaseClass::TableModel:
                return index(row, column);
            default:
                return QAbstractItemModel::sibling(row, column, idx);
            }
        }

        return setOwnIndex(model->fnSibling(row, column, idx));
    }

    QModelIndex buddy(const QModelIndex &idx) const override
    {
        if (!model || !model->fnBuddy.isValid())
            return QAbstractItemModel::buddy(idx);
        return setOwnIndex(model->fnBuddy(idx));
    }

    QVariant data(const QModelIndex &idx, int role) const override
    {
        if (!model || !model->fnData.isValid())
            return {};
        auto result = model->fnData(idx, role);
        return QDotNetConvert::toVariant(result, this);
    }

    QVariant headerData(int section, Qt::Orientation ori, int role) const override
    {
        if (!model || !model->fnHeaderData.isValid())
            return QAbstractItemModel::headerData(section, ori, role);
        auto result = model->fnHeaderData(section, ori, role);
        return QDotNetConvert::toVariant(result, this);
    }

    bool insertRows(int row, int count, const QModelIndex &parent) override
    {
        if (!model || !model->fnInsertRows.isValid())
            return QAbstractItemModel::insertRows(row, count, parent);
        return model->fnInsertRows(row, count, parent);
    }

    bool insertColumns(int column, int count, const QModelIndex &parent) override
    {
        if (!model || !model->fnInsertColumns.isValid())
            return QAbstractItemModel::insertColumns(column, count, parent);
        return model->fnInsertColumns(column, count, parent);
    }

    bool moveRows(const QModelIndex &srcParent, int srcRow, int count,
                  const QModelIndex &destParent, int destChild) override
    {
        if (!model || !model->fnMoveRows.isValid())
            return QAbstractItemModel::moveRows(srcParent, srcRow, count, destParent, destChild);
        return model->fnMoveRows(srcParent, srcRow, count, destParent, destChild);
    }

    bool moveColumns(const QModelIndex &srcParent, int srcCol, int count,
                     const QModelIndex &destParent, int destChild) override
    {
        if (!model || !model->fnMoveColumns.isValid())
            return QAbstractItemModel::moveColumns(srcParent, srcCol, count, destParent, destChild);
        return model->fnMoveColumns(srcParent, srcCol, count, destParent, destChild);
    }

    bool removeRows(int row, int count, const QModelIndex &parent) override
    {
        if (!model || !model->fnRemoveRows.isValid())
            return QAbstractItemModel::removeRows(row, count, parent);
        return model->fnRemoveRows(row, count, parent);
    }

    bool removeColumns(int column, int count, const QModelIndex &parent) override
    {
        if (!model || !model->fnRemoveColumns.isValid())
            return QAbstractItemModel::removeColumns(column, count, parent);
        return model->fnRemoveColumns(column, count, parent);
    }

    void sort(int column, Qt::SortOrder order) override
    {
        if (!model || !model->fnSort.isValid())
            return QAbstractItemModel::sort(column, order);
        model->fnSort(column, order);
    }

    void fetchMore(const QModelIndex &parent) override
    {
        if (!model || !model->fnFetchMore.isValid())
            return QAbstractItemModel::fetchMore(parent);
        model->fnFetchMore(parent);
    }

    bool setData(const QModelIndex &idx, const QVariant &value, int role) override
    {
        if (!model || !model->fnSetData.isValid())
            return QAbstractItemModel::setData(idx, value, role);
        return model->fnSetData(idx, QDotNetConvert::fromVariant(value), role);
    }

    bool setHeaderData(int section, Qt::Orientation ori, const QVariant &value, int role) override
    {
        if (!model || !model->fnSetHeaderData.isValid())
            return QAbstractItemModel::setHeaderData(section, ori, value, role);
        return model->fnSetHeaderData(section, ori, QDotNetConvert::fromVariant(value), role);
    }

    void onModelDataChanged(QDotNetModelEvent &args)
    {
        // NOTE: Do not change invokeMethod using Qt::AutoConnection
        //   * Emit must run on the object's thread.
        QMetaObject::invokeMethod(this, [=]() mutable {
            Q_ASSERT_X(thread() == QThread::currentThread(), "Model Changed",
                       "Emit must run on the object's thread");
            auto parent = setOwnIndex(args.parent());
            auto dest = setOwnIndex(args.destinationParent());
            auto topLeft = setOwnIndex(args.topLeft());
            auto bottomRight = setOwnIndex(args.bottomRight());
            switch (args.action()) {
            case QDotNetModelAction::BeginResetModel:
                beginResetModel();
                break;
            case QDotNetModelAction::EndResetModel:
                endResetModel();
                break;
            case QDotNetModelAction::BeginInsertRows:
                beginInsertRows(parent, args.first(), args.last());
                break;
            case QDotNetModelAction::EndInsertRows:
                endInsertRows();
                break;
            case QDotNetModelAction::BeginMoveRows:
                beginMoveRows(parent, args.first(), args.last(), dest, args.destinationChild());
                break;
            case QDotNetModelAction::EndMoveRows:
                endMoveRows();
                break;
            case QDotNetModelAction::BeginRemoveRows:
                beginRemoveRows(parent, args.first(), args.last());
                break;
            case QDotNetModelAction::EndRemoveRows:
                endRemoveRows();
                break;
            case QDotNetModelAction::BeginInsertColumns:
                beginInsertColumns(parent, args.first(), args.last());
                break;
            case QDotNetModelAction::EndInsertColumns:
                endInsertColumns();
                break;
            case QDotNetModelAction::BeginMoveColumns:
                beginMoveColumns(parent, args.first(), args.last(), dest, args.destinationChild());
                break;
            case QDotNetModelAction::EndMoveColumns:
                endMoveColumns();
                break;
            case QDotNetModelAction::BeginRemoveColumns:
                beginRemoveColumns(parent, args.first(), args.last());
                break;
            case QDotNetModelAction::EndRemoveColumns:
                endRemoveColumns();
                break;
            case QDotNetModelAction::DataChanged:
                emit dataChanged(topLeft, bottomRight, args.roles());
                break;
            case QDotNetModelAction::HeaderDataChanged:
                emit headerDataChanged(args.orientation(), args.first(), args.last());
                break;
            case QDotNetModelAction::NoAction:
                break;
            }
            args.setSynchronized(true);
        });
    }

    //// END QAbstractItemModel overrides
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Dynamic meta-type definitions

    struct DynamicType
    {
        QString name;
        QString assemblyPath;
        QString assemblyQualifiedName;
        QDotNetAssembly assembly;
        QDotNetModule module;
        QDotNetType typeInfo;
        BaseClass baseClass = BaseClass::Object;
        ModelOverrides modelOverrides = ModelOverride::None;
        bool isQmlElement = false;
        QMap<int, DynamicMethod *> methods = {};
        QMap<int, DynamicProperty *> properties = {};
        QMap<QString, DynamicProperty *> propertiesByName = {};
        QList<DynamicEvent *> events = {};
        const QMetaObject *metaObject = nullptr;
        int idxAsDotNetObject = -1;
        int idxNestedQmlElements = -1;
    };

    struct DynamicMember
    {
        DynamicType *declaringType = nullptr;
        QString name;
        QList<Parameter> params;
        int token = 0;
        virtual ~DynamicMember() = default;
    };

    struct DynamicMethod : public DynamicMember
    {
        QDotNetMethodInfo methodInfo;
    };

    struct DynamicProperty : public DynamicMember
    {
        QDotNetPropertyInfo propertyInfo;
        bool isReadable = true;
        bool isWriteable = true;
        int idxNotifySignal = -1;
    };

    struct DynamicEvent : public DynamicMember
    {
        int idxEventSignal = -1;
    };

    struct DynamicModel
    {
        QHash<int, QByteArray> roleNamesByIndex;
        QDotNetFunction<int, QModelIndex> fnRowCount = nullptr;
        QDotNetFunction<int, QModelIndex> fnColumnCount = nullptr;
        QDotNetFunction<QDotNetObject> fnRoleNames = nullptr;
        QDotNetFunction<bool, QModelIndex> fnCanFetchMore = nullptr;
        QDotNetFunction<int, QModelIndex> fnFlags = nullptr;
        QDotNetFunction<bool, QModelIndex> fnHasChildren = nullptr;
        QDotNetFunction<QModelIndex, int, int, QModelIndex> fnIndex = nullptr;
        QDotNetFunction<QModelIndex, QModelIndex> fnParent = nullptr;
        QDotNetFunction<QModelIndex, int, int, QModelIndex> fnSibling = nullptr;
        QDotNetFunction<QModelIndex, QModelIndex> fnBuddy = nullptr;
        QDotNetFunction<QDotNetObject, QModelIndex, int> fnData = nullptr;
        QDotNetFunction<QDotNetObject, int, int, int> fnHeaderData = nullptr;
        QDotNetFunction<bool, int, int, QModelIndex> fnInsertRows = nullptr;
        QDotNetFunction<bool, int, int, QModelIndex> fnInsertColumns = nullptr;
        QDotNetFunction<bool, QModelIndex, int, int, QModelIndex, int> fnMoveRows = nullptr;
        QDotNetFunction<bool, QModelIndex, int, int, QModelIndex, int> fnMoveColumns = nullptr;
        QDotNetFunction<bool, int, int, QModelIndex> fnRemoveRows = nullptr;
        QDotNetFunction<bool, int, int, QModelIndex> fnRemoveColumns = nullptr;
        QDotNetFunction<void, int, int> fnSort = nullptr;
        QDotNetFunction<void, QModelIndex> fnFetchMore = nullptr;
        QDotNetFunction<bool, QModelIndex, QDotNetObject, int> fnSetData = nullptr;
        QDotNetFunction<bool, int, int, QDotNetObject, int> fnSetHeaderData = nullptr;
    };

    struct DynamicQmlElement
    {
        QDotNetFunction<void> fnQmlClassBegin = nullptr;
        QDotNetFunction<void, QDotNetArray<QDotNetRef>> fnQmlComponentComplete = nullptr;
        QList<QObject *> nestedQmlElements;
    };

    //// END Dynamic meta-type definitions
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Aux. definitions

    template <typename T>
    using StaticCastSelector = QQmlPrivate::StaticCastSelector<QDotNetDynamicObject, T>;

    struct Parameter
    {
        QString typeName;
        UnmanagedType unmanagedType;

        Parameter(const QDotNetParameter &p)
            : typeName(p.typeName), unmanagedType(p.unmanagedType())
        {
        }
    };

    struct IQmlElement
    {
        Q_DOTNET_OBJECT_TYPE(IQmlElement, "Qt.Quick.IQmlElement, Qt.Bridge.CSharp.Api");
    };

    //// END Aux. definitions
    ////////////////////////////////////////////////////////////////////////////////////////////////

    ////////////////////////////////////////////////////////////////////////////////////////////////
    //// BEGIN Data

    static inline QDotNetFunction<QDotNetRef, QDotNetType> createInstance = nullptr;

    static inline QMap<QString, DynamicType *> dynamicTypesByName{};
    static inline QMap<const QMetaObjectBuilder *, DynamicType *> dynamicTypesByDef{};
    static inline QMap<const QMetaObject *, const QMetaObjectBuilder *> typeDefs{};
    static inline QSet<void *> objectPlacementAddrs{};

    DynamicType *type = nullptr;
    QList<QDotNetEventHandler *> eventHandlers{};
    DynamicModel *model = nullptr;
    DynamicQmlElement *qmlElement = nullptr;

    //// END Data
    ////////////////////////////////////////////////////////////////////////////////////////////////
};

Q_DECLARE_OPERATORS_FOR_FLAGS(QDotNetDynamicObject::ModelOverrides)
