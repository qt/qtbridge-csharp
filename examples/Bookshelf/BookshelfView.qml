// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

pragma ComponentBehavior: Bound

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

Item {
    id: root

    required property BookLibrary library
    required property ListModel model

    property int selectedIndex: 0

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    readonly property string coverBase: "qrc:/assemblies/Bookshelf/covers/"

    function selectedField(field) {
        var entry = root.model.get(root.selectedIndex)
        return entry ? entry[field] : ""
    }

    // -------------------------------------------------------------------------
    // Layout: sidebar (fixed width) | detail panel (fill)
    // -------------------------------------------------------------------------

    RowLayout {
        anchors.fill: parent
        spacing: 0

        // -----------------------------------------------------------------
        // Sidebar
        // -----------------------------------------------------------------
        Rectangle {
            Layout.fillHeight: true
            Layout.preferredWidth: 210
            color: "#1a1a2e"

            ListView {
                id: sidebar
                anchors.fill: parent
                anchors.margins: 10
                model: root.model
                currentIndex: root.selectedIndex
                spacing: 6
                clip: true

                delegate: ItemDelegate {
                    id: bookItem

                    width: sidebar.width
                    height: 68

                    required property string bookId
                    required property string title
                    required property string author
                    required property int    index

                    highlighted: sidebar.currentIndex === bookItem.index

                    background: Rectangle {
                        color: bookItem.highlighted ? "#2a2a46" : "transparent"
                        radius: 8
                        border.color: bookItem.highlighted ? "#5555aa" : "transparent"
                        border.width: 1
                    }

                    contentItem: RowLayout {
                        spacing: 10

                        // Cover thumbnail - loaded via qrc:/
                        Image {
                            source: root.coverBase + bookItem.bookId + ".svg"
                            Layout.preferredWidth:  44
                            Layout.preferredHeight: 44
                            fillMode: Image.PreserveAspectFit
                            smooth: true
                        }

                        ColumnLayout {
                            spacing: 2
                            Layout.fillWidth: true

                            Text {
                                text: bookItem.title
                                color: bookItem.highlighted ? "#e0e0ff" : "#ccccdd"
                                font.pixelSize: 12
                                font.weight: bookItem.highlighted ? Font.Medium : Font.Normal
                                elide: Text.ElideRight
                                Layout.fillWidth: true
                            }
                            Text {
                                text: bookItem.author
                                color: "#7777aa"
                                font.pixelSize: 10
                                elide: Text.ElideRight
                                Layout.fillWidth: true
                            }
                        }
                    }

                    onClicked: root.selectedIndex = bookItem.index
                }
            }
        }

        // -----------------------------------------------------------------
        // Detail panel
        // -----------------------------------------------------------------
        Rectangle {
            Layout.fillHeight: true
            Layout.fillWidth: true
            color: "#12121e"

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 28
                spacing: 0

                // Header: large cover + title / author / genre / resource path
                RowLayout {
                    Layout.fillWidth: true
                    spacing: 24

                    Image {
                        source: root.coverBase + root.selectedField("bookId") + ".svg"
                        Layout.preferredWidth:  130
                        Layout.preferredHeight: 130
                        fillMode: Image.PreserveAspectFit
                        smooth: true
                    }

                    ColumnLayout {
                        Layout.fillWidth: true
                        Layout.alignment: Qt.AlignVCenter
                        spacing: 8

                        Text {
                            text: root.selectedField("title")
                            color: "#e8e8ff"
                            font.pixelSize: 20
                            font.weight: Font.Bold
                            wrapMode: Text.WordWrap
                            Layout.fillWidth: true
                        }

                        Text {
                            text: root.selectedField("author")
                            color: "#9999cc"
                            font.pixelSize: 13
                        }

                        // Genre badge
                        Rectangle {
                            height: 22
                            width: genreText.implicitWidth + 18
                            radius: 5
                            color: "#252540"
                            border.color: "#44447a"
                            border.width: 1

                            Text {
                                id: genreText
                                anchors.centerIn: parent
                                text: root.selectedField("genre")
                                color: "#8888cc"
                                font.pixelSize: 11
                            }
                        }

                        // qrc:/ path label - makes the resource URL explicit
                        Text {
                            text: root.coverBase + root.selectedField("bookId") + ".svg"
                            color: "#3a3a5a"
                            font.pixelSize: 10
                            font.family: "Courier New, monospace"
                            elide: Text.ElideRight
                            Layout.fillWidth: true
                        }
                    }
                }

                // Divider
                Rectangle {
                    Layout.fillWidth: true
                    height: 1
                    color: "#252538"
                    Layout.topMargin: 18
                    Layout.bottomMargin: 16
                }

                // Synopsis - loaded by C# either from qrc:/ or, when available,
                // from the managed ResourceManager copy of the same content.
                ScrollView {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    clip: true

                    ScrollBar.vertical.policy: ScrollBar.AsNeeded
                    ScrollBar.vertical.width: 10
                    ScrollBar.vertical.background: Rectangle {
                        implicitWidth: 10
                        radius: 5
                        color: "#18182a"
                        border.color: "#252538"
                        border.width: 1
                    }
                    ScrollBar.vertical.contentItem: Rectangle {
                        implicitWidth: 6
                        radius: 3
                        color: "#6666aa"
                    }

                    ScrollBar.horizontal.policy: ScrollBar.AsNeeded
                    ScrollBar.horizontal.height: 10
                    ScrollBar.horizontal.background: Rectangle {
                        implicitHeight: 10
                        radius: 5
                        color: "#18182a"
                        border.color: "#252538"
                        border.width: 1
                    }
                    ScrollBar.horizontal.contentItem: Rectangle {
                        implicitHeight: 6
                        radius: 3
                        color: "#6666aa"
                    }

                    Text {
                        width: parent.width
                        // getDisplaySynopsis() prefers the managed ResourceManager path for
                        // ManagedAndNative entries and otherwise falls back to qrc:/.
                        text: root.library.getDisplaySynopsis(root.selectedField("bookId"))
                        color: "#b8b8d0"
                        font.pixelSize: 13
                        lineHeight: 1.6
                        wrapMode: Text.WordWrap
                    }
                }

                // ManagedAndNative badge - visible only when the displayed synopsis comes
                // from ResourceManager and still matches the qrc:/ copy.
                Rectangle {
                    visible: root.library.hasManagedSynopsis(root.selectedField("bookId"))
                        && root.library.verifyManagedSynopsis(root.selectedField("bookId"))
                    Layout.topMargin: 8
                    height: 24
                    width: badgeText.implicitWidth + 20
                    radius: 5
                    color: "#1a2e1a"
                    border.color: "#2a6a2a"
                    border.width: 1

                    Text {
                        id: badgeText
                        anchors.centerIn: parent
                        text: "Displayed via ResourceManager - AccessMode=ManagedAndNative"
                        color: "#4a9a4a"
                        font.pixelSize: 10
                    }
                }

                // Divider
                Rectangle {
                    Layout.fillWidth: true
                    height: 1
                    color: "#252538"
                    Layout.topMargin: 8
                    Layout.bottomMargin: 8
                }

                // About footer - loaded by C# via Qt.Resources.ReadAllText()
                Text {
                    Layout.fillWidth: true
                    // library.about reads about.txt via Qt.Resources.ReadAllText().
                    // about.txt is a direct <QtResource> item (AccessMode=Default).
                    text: root.library.about
                    color: "#5a5a80"
                    font.pixelSize: 10
                    wrapMode: Text.WordWrap
                    maximumLineCount: 4
                    elide: Text.ElideRight
                }
            }
        }
    }
}
