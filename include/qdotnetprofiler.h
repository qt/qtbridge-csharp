// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

#pragma once

#ifdef __GNUC__
#  pragma GCC diagnostic push
#  pragma GCC diagnostic ignored "-Wconversion"
#endif
#include <QElapsedTimer>
#include <QFile>
#include <QFileInfo>
#include <QMap>
#include <QMutex>
#include <QTextStream>
#include <QThread>
#ifdef __GNUC__
#  pragma GCC diagnostic pop
#endif

class QDotNetProfiler
{
private:
    static qint64 timestamp()
    {
        static QElapsedTimer timer;
        if (!timer.isValid())
            timer.start();
        return timer.nsecsElapsed();
    }

    struct LogEntry
    {
        QString file;
        int line;
        QString tag;
        qint64 start, stop;
        Qt::HANDLE threadId;
        LogEntry() = default;
        LogEntry(const char *file, int line, const char *tag, qint64 start)
            : file(file), line(line), tag(tag), start(start), stop(0),
              threadId(QThread::currentThreadId())
        { }
    };

    struct LogFile
    {
        QMap<qint64, LogEntry> logEntries;
        QMutex syncLogEntries;

        void add(const LogEntry &logEntry)
        {
            QMutexLocker lock(&syncLogEntries);
            auto key = logEntry.start;
            if (logEntries.contains(key))
                key++;
            logEntries.insert(key, logEntry);
        }

        void write(const QString &path)
        {
            QMutexLocker lock(&syncLogEntries);
            if (logEntries.isEmpty())
                return;

            QFile logFile(path);
            if (!logFile.open(QIODevice::WriteOnly | QIODevice::Text)) {
                qWarning() << "QDotNetProfiler: Cannot open log file";
                return;
            }

            QTextStream logData(&logFile);
            for (const auto &logEntry : logEntries) {
                logData << logEntry.threadId << " | " << QFileInfo(logEntry.file).fileName()
                        << " | " << logEntry.line << " | " << logEntry.tag << " | "
                        << logEntry.start << " | " << logEntry.stop << "\n";
            }
        }
    };

    static inline LogFile logFile;
    LogEntry logEntry;

public:
    static void writeLog(const QString &logFilePath)
    {
        if (QFile::exists(logFilePath))
            QFile::remove(logFilePath);
        logFile.write(logFilePath);
    }

    QDotNetProfiler(const char *file, int line, const char *tag, bool scope = false)
        : logEntry(file, line, tag, timestamp())
    {
        if (scope) {
            logEntry.threadId = 0;
            logEntry.start = 0;
            logEntry.stop = -1;
            logFile.add(logEntry);
        }
    }

    void stop()
    {
        if (logEntry.stop)
            return;
        logEntry.stop = timestamp();
        logFile.add(logEntry);
    }

    ~QDotNetProfiler() { stop(); }
};

#ifdef ENABLE_QDOTNETPROFILER
#  define Q_DOTNET_PROFILER(var, tag) QDotNetProfiler var(__FILE__, __LINE__, tag)
#  define Q_DOTNET_PROFILER_STOP(var) var.stop()
#  define Q_DOTNET_PROFILE(tag) Q_DOTNET_PROFILER(__profiler, tag)
#  define Q_DOTNET_PROFILE_FUNC() Q_DOTNET_PROFILE(__func__)
#  define Q_DOTNET_PROFILE_SCOPE(type)                              \
      struct __profiler_##type                                      \
      {                                                             \
          static inline QDotNetProfiler __scope =                   \
                  QDotNetProfiler(__FILE__, __LINE__, #type, true); \
      }
#  define Q_DOTNET_PROFILER_WRITE_LOG(path) QDotNetProfiler::writeLog(path)
#else
#  define Q_DOTNET_PROFILER(var, tag)
#  define Q_DOTNET_PROFILER_STOP(var)
#  define Q_DOTNET_PROFILE(tag)
#  define Q_DOTNET_PROFILE_FUNC()
#  define Q_DOTNET_PROFILE_SCOPE(type)
#  define Q_DOTNET_PROFILER_WRITE_LOG(path)
#endif
