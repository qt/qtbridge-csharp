// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick

Window {
    id: window
    width: 760
    height: 520
    minimumWidth: 560
    minimumHeight: 400
    visible: true
    title: qsTr("Bookshelf")

    // BookLibrary is the C# side that exposes resource-backed values to QML.
    BookLibrary {
        id: bookLibrary
    }

    // Book metadata lives here in QML; dynamic text content (synopses, about)
    // is loaded on demand from Qt resources by the C# backend.
    ListModel {
        id: bookModel
        ListElement {
            bookId: "adventure"
            title:  "The Last Horizon"
            author: "Elena Cross"
            genre:  "Adventure"
        }
        ListElement {
            bookId: "history"
            title:  "Echoes of Antiquity"
            author: "Marcus Webb"
            genre:  "History"
        }
        ListElement {
            bookId: "science"
            title:  "The Quantum Paradox"
            author: "Dr. Sarah Kim"
            genre:  "Science"
        }
        ListElement {
            bookId: "fiction"
            title:  "Stellar Drift"
            author: "J. A. Novak"
            genre:  "Fiction"
        }
    }

    BookshelfView {
        anchors.fill: parent
        library: bookLibrary
        model: bookModel
    }
}
