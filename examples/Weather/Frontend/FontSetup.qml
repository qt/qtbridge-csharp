// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

pragma Singleton
import QtQuick

// Bundles the design-system fonts. Typography.qml names the families but
// cannot load them; without these loaders Qt silently substitutes a system
// font. Main.qml touches this singleton at startup, because QML singletons
// are lazy and the loaders must exist before anything draws.
QtObject {
    property FontLoader body: FontLoader { source: Qt.resolvedUrl("assets/fonts/Inter.ttf") }
    property FontLoader display: FontLoader { source: Qt.resolvedUrl("assets/fonts/LeagueSpartan.ttf") }
}
