/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

[assembly: Qt.Generate(

    MainIncludes = $@"
#include <generatortestapp/prime.h>
",

    MainBeforeAppExec = $@"
GeneratorTestApp::Prime prime;
QObject::connect(&prime, &GeneratorTestApp::Prime::valueChanged,
    [&app, dotnetThread, &prime]()
    {{
        auto index = prime.index();
        qInfo() << ""Prime ["" << index + 1 << ""] ="" << prime.value();
        if (index < 99) {{
            prime.setIndex(index + 1);
        }}
    }});

prime.setIndex(0);

")]
