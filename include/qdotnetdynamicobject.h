// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#include "qdotnetarray.h"
#include "qdotnetconvert.h"
#include "qdotnetobject.h"
#include "qdotnetparameter.h"
#include "qdotnetreflection.h"
#include "qdotnettype.h"

#include <functional>

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QCoreApplication>
#include <QDir>
#include <QFile>
#include <QJSEngine>
#include <QList>
#include <QMap>
#include <QMetaObject>
#include <QObject>
#include <QSet>
#include <QtCore/private/qmetaobjectbuilder_p.h>
#include <QtQml/qqmlprivate.h>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetDynamicObject : public QObject, public QDotNetObject
{
private:
    struct Parameter;
    struct DynamicType;
    struct DynamicMethod;
    struct DynamicProperty;

public:
    static QMetaObjectBuilder *defineType(const QString &typeName, const QString &qualifiedTypeName,
                                          const QString &assemblyFile, const QString &appDirPath)
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
        typeDef->setClassName(type->assemblyQualifiedName.toUtf8());
        typeDef->setStaticMetacallFunction(staticMetacall);
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

    const QMetaObject *metaObject() const override { return type->metaObject; }

    void *qt_metacast(const char *_clname) override
    {
        if (!_clname)
            return nullptr;
        if (_clname == QDotNetObject::ClassName || !strcmp(_clname, QDotNetObject::ClassName))
            return static_cast<QDotNetObject *>(this);
        return QObject::qt_metacast(_clname);
    }

    int qt_metacall(QMetaObject::Call _c, int _id, void **_a) override
    {
        _id = QObject::qt_metacall(_c, _id, _a);
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

    QDotNetDynamicObject() = delete;
    QDotNetDynamicObject(QObject *) = delete;

    ~QDotNetDynamicObject() noexcept override { }

    QDotNetDynamicObject(const QDotNetDynamicObject &obj) : type(obj.type) { copyFrom(obj); }

    QDotNetDynamicObject(QDotNetDynamicObject &&obj) : type(obj.type) { moveFrom(obj); }

private:
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

    void init()
    {
        init(type);
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

        auto *objProxy = qobject_cast<QDotNetDynamicObject *>(obj);
        if (!objProxy)
            return;

        if (call == QMetaObject::InvokeMetaMethod) {

            if (id == type->idxAsDotNetObject) {
                *reinterpret_cast<const QDotNetObject **>(args[0]) = objProxy;
                return;
            }

            const auto &itMethod = type->methods.find(id);
            if (itMethod == type->methods.end())
                return;
            return invokeMetaMethod(objProxy, *itMethod, args);
        }

        if (call == QMetaObject::ReadProperty) {
            const auto &itProp = type->properties.find(id);
            if (itProp == type->properties.end())
                return;
            return readProperty(objProxy, *itProp, args);
        }

        if (call == QMetaObject::WriteProperty) {
            const auto &itProp = type->properties.find(id);
            if (itProp == type->properties.end())
                return;
            return writeProperty(objProxy, *itProp, args);
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

        writeResult(method->params[0], args[0], method->methodInfo.invoke(*obj, argValues));
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
        writeResult(prop->params[0], args[0], prop->propertyInfo.getValue(*obj));
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
        }
        return nullptr;
    }

    static void writeResult(const Parameter &param, void *arg, const QDotNetRef &obj)
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
            break;
        }
    }

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

    struct DynamicType
    {
        QString name;
        QString assemblyPath;
        QString assemblyQualifiedName;
        QDotNetAssembly assembly;
        QDotNetModule module;
        QDotNetType typeInfo;
        QMap<int, DynamicMethod *> methods = {};
        QMap<int, DynamicProperty *> properties = {};
        QMap<QString, DynamicProperty *> propertiesByName = {};
        const QMetaObject *metaObject = nullptr;
        int idxAsDotNetObject = -1;
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
    };

    static inline QDotNetFunction<QDotNetRef, QDotNetType> createInstance = nullptr;

    static inline QMap<QString, DynamicType *> dynamicTypesByName{};
    static inline QMap<const QMetaObjectBuilder *, DynamicType *> dynamicTypesByDef{};
    static inline QMap<const QMetaObject *, const QMetaObjectBuilder *> typeDefs{};
    static inline QSet<void *> objectPlacementAddrs{};

    DynamicType *type = nullptr;
};
