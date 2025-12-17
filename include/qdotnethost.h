/***************************************************************************************************
 Copyright (C) 2023 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

#pragma once

#include "qdotnetfunction.h"

#ifdef __GNUC__
#   pragma GCC diagnostic push
#   pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QCoreApplication>
#include <QDebug>
#include <QDir>
#include <QLibrary>
#include <QFile>
#include <QProcess>
#include <QRegularExpression>
#include <QScopedPointer>
#include <QString>
#include <QTemporaryFile>
#include <QVersionNumber>
#ifdef __GNUC__
#   pragma GCC diagnostic pop
#endif

class QDotNetHost
{
public:
    QDotNetHost() = default;

    ~QDotNetHost()
    {
        unload();
    }

    bool load(const QString &runtimeConfig = defaultRuntimeConfig, const QString &runtimePath = {})
    {
        if (isLoaded())
            return true;
        if (!loadRuntime(runtimePath))
            return false;
        if (!init(runtimeConfig)) {
            unloadRuntime();
            return false;
        }
        return true;
    }

    bool appMain(const QString &appHostPath, const QString &appLibPath)
    {
        if (isLoaded())
            return false;

        if (!loadRuntime({}))
            return false;
        const char_t *host_path = STR(appHostPath);
        const char_t *app_path = STR(appLibPath);
        const char_t *dotnet_root = (char_t *)L"C:\\Program Files\\dotnet\\";

        int argc = 1;
        const char_t *argv[] = { host_path, nullptr };

        return fnMainStartup(argc, argv, host_path, dotnet_root, app_path) == 0;
    }

    bool loadApp(const QString &appPath, const QStringList &args = {}, const QString &runtimePath = {})
    {
        if (isLoaded())
            return false;

        if (!loadRuntime(runtimePath))
            return false;

        auto argc = args.size();
        if (argc == 0)
            argc = 1;
        QScopedPointer<const char_t *,
            QScopedPointerArrayDeleter<const char_t *>> argvPtr(new const char_t * [argc + 1]);

        auto argv = argvPtr.data();
        argv[0] = STR(appPath);
        for (int i = 1; i < argc ; ++i)
            argv[i] = STR(args[i]);
        argv[argc] = nullptr;

        auto result = fnInitApp(argc, argv, nullptr, &hostContext);
        if (HOSTFN_FAILED(result) || hostContext == nullptr) {
            qCritical() << "Error calling function: hostfxr_initialize_for_dotnet_command_line";
            unloadRuntime();
            return false;
        }

        setRuntimeProperty("STARTUP_HOOKS",
            QDir(QCoreApplication::applicationDirPath()).filePath("Qt.DotNet.Adapter.dll"));

        setRuntimeProperty("QT_DOTNET_RESOLVE_FN", QString("%1")
            .arg((qulonglong)(&fnLoadAssemblyAndGetFunctionPointer), 16, 16, QChar('0')));

        return true;
    }

    int runApp()
    {
        if (!isLoaded() || fnLoadAssemblyAndGetFunctionPointer != nullptr)
            return false;
        return fnRunApp(hostContext);
    }

    void unload()
    {
        if (!isLoaded())
            return;
        close();
        unloadRuntime();
    }

    bool isLoaded() const
    {
        return (hostContext != nullptr);
    }

    bool isReady() const
    {
        return (fnLoadAssemblyAndGetFunctionPointer != nullptr);
    }

    bool resolveFunction(QDotNetFunction<quint32, void *, qint32> &outFunc,
        const QString &assemblyPath, const QString &typeName, const QString &methodName)
    {
        return resolveFunction(outFunc, assemblyPath, typeName, methodName, {});
    }

    template<typename TResult, typename... TArgs>
    bool resolveFunction(QDotNetFunction<TResult, TArgs...> &outFunc, const QString &assemblyPath,
        const QString &typeName, const QString &methodName, const QString &delegateType)
    {
        if (!isLoaded() && !load())
            return false;
        outFunc = resolveFunction(assemblyPath, typeName, methodName, delegateType);
        return outFunc.isValid();
    }

    QMap<QString, QString> runtimeProperties() const
    {
        if (!isLoaded())
            return {};
        size_t queryCount = 0;
        const auto result = fnAllRuntimeProperties(hostContext, &queryCount, nullptr, nullptr);
        if (result != Success && result != HostApiBufferTooSmall) {
            qCritical() << "Error calling function: hostfxr_get_runtime_properties";
            return {};
        }
        const size_t count = queryCount;
        const char_t **keys = new const char_t * [count] { nullptr };
        const char_t **values = new const char_t * [count] { nullptr };
        if (HOSTFN_FAILED(fnAllRuntimeProperties(hostContext, &queryCount, keys, values))) {
            qCritical() << "Error calling function: hostfxr_get_runtime_properties";
            delete[] keys;
            delete[] values;
            return {};
        }
        QMap<QString, QString> properties;
        for (size_t i = 0; i < count; ++i) {
            const char_t *key = keys[i];
            const char_t *value = values[i];
            if (key && value)
                properties.insert(QSTR(key), QSTR(value));
        }
        delete[] keys;
        delete[] values;
        return properties;
    }

    QString runtimeProperty(const QString &name) const
    {
        if (!isLoaded())
            return {};
        const char_t *value = nullptr;
        if (HOSTFN_FAILED(fnRuntimeProperty(hostContext, STR(name), &value))) {
            qCritical() << "Error calling function: hostfxr_get_runtime_property_value";
            return {};
        }
        if (!value)
            return {};
        return QSTR(value);
    }

    bool setRuntimeProperty(const QString &name, const QString &value) const
    {
        if (!isLoaded())
            return false;
        if (HOSTFN_FAILED(fnSetRuntimeProperty(hostContext, STR(name), STR(value)))) {
            qCritical() << "Error calling function: hostfxr_set_runtime_property_value";
            return false;
        }
        return true;
    }

    void setErrorWriter(hostfxr_error_writer_fn errorWriter)
    {
        if (fnSetErrorWriter == nullptr || hostContext == nullptr)
            return;
        fnSetErrorWriter(errorWriter);
    }

private:
    void *resolveFunction(const QString &assemblyPath, const QString &typeName,
        const QString &methodName, const QString &delegateType) const
    {
        if (hostContext == nullptr)
            return nullptr;
        void *funcPtr = nullptr;
        auto result = fnLoadAssemblyAndGetFunctionPointer(
            STR(assemblyPath),
            STR(typeName),
            STR(methodName),
            delegateType.isEmpty() ? nullptr : STR(delegateType),
            nullptr,
            &funcPtr);
        if (HOSTFN_FAILED(result)) {
            qCritical() << "Error getting function pointer:"
                << methodName << QString::number(static_cast<unsigned>(result), 16);
            return nullptr;
        }
        return funcPtr;
    }

    QString findRuntimePath() const
    {
        QProcess procDotNetInfo;
        procDotNetInfo.start("dotnet", { "--list-runtimes" });
        if (!procDotNetInfo.waitForFinished() || procDotNetInfo.exitCode() != 0) {
            qCritical() << "Error calling dotnet";
            return {};
        }
        const QString dotNetInfo(procDotNetInfo.readAllStandardOutput());

        QVersionNumber selectedVersion = {};
        QString selectedRuntimePath = {};
        const QRegularExpression dotNetInfoParser(regexParseDotNetInfo,
            QRegularExpression::MultilineOption | QRegularExpression::DotMatchesEverythingOption);
        for (const auto &match : dotNetInfoParser.globalMatch(dotNetInfo)) {

            const auto hostVersion = match.captured("version");
            const auto version = QVersionNumber::fromString(hostVersion);
            if (version <= selectedVersion)
                continue;

            const auto runtimeDirPath = match.captured("path");
            if (runtimeDirPath.isEmpty())
                continue;
            QDir runtimeDir(runtimeDirPath);
            if (!runtimeDir.exists())
                continue;
            runtimeDir.cd(QString("../../host/fxr/%1").arg(hostVersion));
            if (!runtimeDir.exists())
                continue;
#ifdef Q_OS_WINDOWS
            const auto runtimePath = runtimeDir.absoluteFilePath("hostfxr.dll");
#else
            const auto runtimePath = runtimeDir.absoluteFilePath("libhostfxr.so");
#endif
            if (!QFile::exists(runtimePath))
                continue;

            selectedVersion = version;
            selectedRuntimePath = runtimePath;
        }

        if (selectedVersion.isNull()) {
            qCritical() << "Error locating runtime host library.";
            return {};
        }

        return selectedRuntimePath;
    }

    bool loadRuntime(const QString & runtimePath)
    {
        if (fnCloseHost != nullptr)
            return true;

        if (!runtimePath.isEmpty()) {
            if (!QFile::exists(runtimePath))
                return false;
            runtime.setFileName(runtimePath);
        } else {
            const QString defaultRuntimePath = findRuntimePath();
            if (defaultRuntimePath.isEmpty())
                return false;
            runtime.setFileName(defaultRuntimePath);
        }

        if (!runtime.load()) {
            qCritical() << "Error loading library: hostfxr";
            return false;
        }

        if (!(fnMainStartup = GET_FN(runtime, hostfxr_main_startupinfo_fn))) {
            qCritical() << "Error loading function: hostfxr_main_startupinfo";
            return false;
        }

        if (!(fnSetErrorWriter = GET_FN(runtime, hostfxr_set_error_writer_fn))) {
            qCritical() << "Error loading function: hostfxr_set_error_writer";
            return false;
        }

        if (!(fnInitApp = GET_FN(runtime, hostfxr_initialize_for_dotnet_command_line_fn))) {
            qCritical() << "Error loading function: hostfxr_initialize_for_dotnet_command_line";
            return false;
        }

        if (!(fnInitHost = GET_FN(runtime, hostfxr_initialize_for_runtime_config_fn))) {
            qCritical() << "Error loading function: hostfxr_initialize_for_runtime_config";
            return false;
        }

        if (!(fnRuntimeProperty = GET_FN(runtime, hostfxr_get_runtime_property_value_fn))) {
            qCritical() << "Error loading function: hostfxr_get_runtime_property_value_fn";
            return false;
        }

        if (!(fnSetRuntimeProperty = GET_FN(runtime, hostfxr_set_runtime_property_value_fn))) {
            qCritical() << "Error loading function: hostfxr_set_runtime_property_value_fn";
            return false;
        }

        if (!(fnAllRuntimeProperties = GET_FN(runtime, hostfxr_get_runtime_properties_fn))) {
            qCritical() << "Error loading function: hostfxr_get_runtime_properties_fn";
            return false;
        }

        if (!(fnRunApp = GET_FN(runtime, hostfxr_run_app_fn))) {
            qCritical() << "Error loading function: hostfxr_run_app";
            return false;
        }

        if (!(fnGetRuntimeDelegate = GET_FN(runtime, hostfxr_get_runtime_delegate_fn))) {
            qCritical() << "Error loading function: hostfxr_get_runtime_delegate";
            return false;
        }

        if (!(fnCloseHost = GET_FN(runtime, hostfxr_close_fn))) {
            qCritical() << "Error loading function: hostfxr_close";
            return false;
        }

        fnSetErrorWriter(defaultErrorWriter);
        return true;
    }

    void unloadRuntime()
    {
        fnSetErrorWriter = nullptr;
        fnInitApp = nullptr;
        fnInitHost = nullptr;
        fnRuntimeProperty = nullptr;
        fnSetRuntimeProperty = nullptr;
        fnAllRuntimeProperties = nullptr;
        fnRunApp = nullptr;
        fnGetRuntimeDelegate = nullptr;
        fnCloseHost = nullptr;
        fnLoadAssemblyAndGetFunctionPointer = nullptr;
        fnLoadAssembly = nullptr;
        fnGetFunctionPointer = nullptr;
        runtime.unload();
    }

    bool init(const QString &runtimeConfig)
    {
        if (fnLoadAssemblyAndGetFunctionPointer)
            return true;

        if (fnInitHost == nullptr)
            return false;

        const QString tempFileName = writeTempFile(runtimeConfig, runtimeConfigFileName);
        if (tempFileName.isEmpty()) {
            qCritical() << "Error writing runtime configuration file.";
            return false;
        }

        auto result = fnInitHost(STR(tempFileName), nullptr, &hostContext);
        if (HOSTFN_FAILED(result) || hostContext == nullptr) {
            qCritical() << "Error calling function: hostfxr_initialize_for_runtime_config";
            QFile::remove(tempFileName);
            return false;
        }

        if (!QFile::remove(tempFileName))
            qWarning() << "Error removing file:" << tempFileName;

        result = fnGetRuntimeDelegate(hostContext,
            hdt_load_assembly_and_get_function_pointer,
            reinterpret_cast<void **>(&fnLoadAssemblyAndGetFunctionPointer));
        if (HOSTFN_FAILED(result)) {
            qCritical() << "Error calling function:"
                << "hostfxr_get_runtime_delegate(hdt_load_assembly_and_get_function_pointer)";
            return false;
        }

        return true;
    }

    bool close()
    {
        if (fnCloseHost == nullptr || hostContext == nullptr)
            return false;
        if (HOSTFN_FAILED(fnCloseHost(hostContext)))
            qWarning() << "Error calling function: hostfxr_close";
        hostContext = nullptr;
        fnLoadAssemblyAndGetFunctionPointer = nullptr;
        return true;
    }

    static void defaultErrorWriter(const char_t *message)
    {
        qWarning() << "Qt/.NET: Host error:" << QSTR(message);
    }

    static QString writeTempFile(const QString &text, const QString &fileNameTemplate)
    {
        QTemporaryFile tempFile(fileNameTemplate);
        tempFile.setAutoRemove(false);
        if (!tempFile.open()) {
            qCritical() << "Error creating temp file:" << tempFile.errorString();
            return {};
        }

        QString tempFileName = tempFile.fileName();
        const QByteArray fileData = text.toUtf8();
        if (!tempFile.write(fileData)) {
            qCritical() << "Error writing file:" << tempFileName;
            return {};
        }
        tempFile.close();

        return tempFileName;
    }

    static inline const QString runtimeConfigFileName = QStringLiteral("runtimeconfig.XXXXXX.json");
    static inline const QString defaultRuntimeConfig = QStringLiteral(R"[json](
{
  "runtimeOptions": {
    "tfm": "net8.0",
    "rollForward": "LatestMinor",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "8.0.0"
    }
  }
}
)[json]");

    static inline const QString regexParseDotNetInfo = QStringLiteral(R"[regex](
\bMicrosoft\.NETCore\.App[^0-9]*(?<version>[0-9\.]+)[^\[]*\[(?<path>[^\]]+)\]
)[regex]").remove('\r').remove('\n');

    static inline const QString regexParseDotNetRoot = QStringLiteral(R"[regex](
^(?:(?![\\\/]host[\\\/]).)*[\\\/]
)[regex]").remove('\r').remove('\n');

    QLibrary runtime;
    hostfxr_main_startupinfo_fn fnMainStartup = nullptr;
    hostfxr_set_error_writer_fn fnSetErrorWriter = nullptr;
    hostfxr_initialize_for_dotnet_command_line_fn fnInitApp = nullptr;
    hostfxr_initialize_for_runtime_config_fn fnInitHost = nullptr;
    hostfxr_get_runtime_property_value_fn fnRuntimeProperty = nullptr;
    hostfxr_set_runtime_property_value_fn fnSetRuntimeProperty = nullptr;
    hostfxr_get_runtime_properties_fn fnAllRuntimeProperties = nullptr;
    hostfxr_run_app_fn fnRunApp = nullptr;
    hostfxr_get_runtime_delegate_fn fnGetRuntimeDelegate = nullptr;
    hostfxr_close_fn fnCloseHost = nullptr;
    hostfxr_handle hostContext = nullptr;

    load_assembly_and_get_function_pointer_fn fnLoadAssemblyAndGetFunctionPointer = nullptr;
    load_assembly_fn fnLoadAssembly = nullptr;
    get_function_pointer_fn fnGetFunctionPointer = nullptr;
};

#define EMBED_HASH_HI_PART_UTF8 "c3ab8ff13720e8ad9047dd39466b3c89"
#define EMBED_HASH_LO_PART_UTF8 "74e592c2fa383d4a3960714caef0c4f2"
#define EMBED_HASH_FULL_UTF8 (EMBED_HASH_HI_PART_UTF8 EMBED_HASH_LO_PART_UTF8)
#define QT_DOTNET_HOST(appName) \
constexpr int EMBED_SZ = sizeof(EMBED_HASH_FULL_UTF8) / sizeof(EMBED_HASH_FULL_UTF8[0]); \
constexpr int EMBED_MAX = (EMBED_SZ > 1025 ? EMBED_SZ : 1025); \
static char appName[EMBED_MAX] = EMBED_HASH_FULL_UTF8; \
static const char hi_part[] = EMBED_HASH_HI_PART_UTF8; \
static const char lo_part[] = EMBED_HASH_LO_PART_UTF8
