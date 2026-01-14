REM Copyright (C) 2026 The Qt Company Ltd.
REM SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only

@echo off
setlocal enabledelayedexpansion

:: Default configurations
set default_configs=Debug Release

:: Check if a configuration is passed as an argument
if "%~1"=="" (
    set configs=%default_configs%
) else (
    set configs=%1
)

for %%c in (%configs%) do (
    echo Building %%c configuration...
    dotnet build --configuration %%c
    if errorlevel 1 (
        echo Error building %%c configuration
        exit /b 1
    )

    echo Running tests for %%c configuration...
    dotnet test --configuration %%c --no-build
    if errorlevel 1 (
        echo Error running tests for %%c configuration
        exit /b 1
    )
)

echo All specified configurations built and tested successfully!

