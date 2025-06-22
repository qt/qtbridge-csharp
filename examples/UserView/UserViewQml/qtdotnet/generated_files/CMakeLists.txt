cmake_minimum_required(VERSION 3.16)

project(qmlapp VERSION 0.1 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD_REQUIRED ON)

find_package(Qt6 6.6 REQUIRED COMPONENTS Quick)

qt_standard_project_setup(REQUIRES 6.6 SUPPORTS_UP_TO 6.8)

qt_add_executable(qmlapp
    main.cpp
)

qt_add_qml_module(qmlapp
    URI qmlapp
    VERSION 1.0
    QML_FILES
        Main.qml
    DEPENDENCIES
        QtQml.Models
    SOURCES
        qmlapp.h
        qmlapp.cpp
)

set_target_properties(qmlapp PROPERTIES WIN32_EXECUTABLE TRUE)


target_link_libraries(qmlapp
    PRIVATE Qt6::Quick
)
