// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

pragma ComponentBehavior: Bound

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

import DefaultStyle as AppStyle

Item {
    id: usersView
    required property var viewModel
    ColumnLayout {
        spacing: 0
        anchors.fill: parent
        anchors.margins: 28

        RowLayout {
            spacing: 0
            AppStyle.Label {
                Layout.fillWidth: true
                text: "Directory"
                font.pixelSize: 30
                font.weight: Font.DemiBold
            }
            AppStyle.Button {
                text: "Refresh"
                Layout.preferredWidth: implicitWidth + 36
                Layout.preferredHeight: implicitHeight + 18
                enabled: usersView.viewModel && !usersView.viewModel.isLoading
                onClicked: usersView.viewModel.retry()
            }
        }

        RowLayout {
            spacing: 0
            Layout.topMargin: 20
            Layout.bottomMargin: 14
            Layout.fillWidth: true
            Rectangle {
                Layout.preferredWidth: searchBox.implicitWidth
                Layout.preferredHeight: searchBox.implicitHeight
                Layout.maximumWidth: 560
                color: AppStyle.Theme.colors.surface
                border.color: AppStyle.Theme.colors.border
                radius: 10
                RowLayout {
                    spacing: 0
                    id: searchBox
                    AppStyle.Label {
                        Layout.leftMargin: 14
                        Layout.rightMargin: 8
                        Layout.bottomMargin: 2
                        Layout.alignment: Qt.AlignVCenter
                        text: "⌕"
                        color: AppStyle.Theme.colors.mutedText
                        font.pixelSize: 22
                    }

                    TextInput {
                        id: search
                        Layout.minimumWidth: 340
                        Layout.preferredHeight: implicitHeight + 22
                        verticalAlignment: TextInput.AlignVCenter
                        font.pixelSize: 15
                        onTextChanged: timer.restart()
                        Timer {
                            id: timer
                            interval: 200
                            onTriggered: {
                                if (usersView.viewModel)
                                    usersView.viewModel.searchText = search.text
                            }
                        }
                    }

                    AppStyle.Button {
                        Layout.fillWidth: true
                        Layout.fillHeight: true
                        Layout.margins: 6
                        Layout.preferredWidth: implicitWidth + 24
                        Layout.preferredHeight: implicitHeight + 12
                        enabled: search.length > 0
                        text: "Clear"
                        onClicked: search.text = ""
                    }
                }
            }
            AppStyle.Label {
                Layout.fillWidth: true
                Layout.preferredHeight: searchBox.implicitHeight
                color: AppStyle.Theme.colors.mutedText
                text: usersView.viewModel
                    && !usersView.viewModel.isLoading ? usersView.viewModel.resultSummary : ""
                horizontalAlignment: Label.AlignRight
                verticalAlignment: Label.AlignVCenter
            }
        }

        StackLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true

            currentIndex: usersView.viewModel && !usersView.viewModel.errorMessage ? 0 : 1

            ListView {
                id: listView
                property alias viewModel: usersView.viewModel
                clip: true
                spacing: 14
                ScrollBar.vertical: ScrollBar {
                    id: scrollBar
                    policy: ScrollBar.AlwaysOn
                    active: true
                    implicitWidth: 20
                }
                model: !viewModel.isLoading ? viewModel.filteredUsers : []
                delegate: AppStyle.CardButton {
                    id: user
                    required property var item
                    required property string fullName
                    required property string email
                    required property string initials
                    required property int avatarColorIndex

                    onClicked: usersView.viewModel.openDetails(item)

                    width: listView.width - 1.5 * scrollBar.width
                    height: 84
                    RowLayout {
                        spacing: 0
                        anchors.fill: parent

                        Rectangle {
                            Layout.leftMargin: 28
                            Layout.rightMargin: 8
                            Layout.alignment: Qt.AlignVCenter
                            width: 48
                            height: 48
                            radius: 24
                            color: AppStyle.Theme.colors.avatarColor(user.avatarColorIndex)
                            Text {
                                anchors.centerIn: parent
                                font.pixelSize: 16
                                font.weight: Font.DemiBold
                                text: user.initials
                                horizontalAlignment: Label.AlignHCenter
                                verticalAlignment: Label.AlignVCenter
                            }
                        }

                        ColumnLayout {
                            spacing: 0
                            Layout.alignment: Qt.AlignLeft
                            AppStyle.Label {
                                text: user.fullName
                                font.pixelSize: 17
                                font.weight: Font.DemiBold
                            }
                            AppStyle.Label {
                                text: user.email
                                color: AppStyle.Theme.colors.mutedText
                            }
                        }

                        AppStyle.Label {
                            Layout.fillWidth: true
                            Layout.fillHeight: true
                            Layout.rightMargin: 28
                            text: "View details  ›"
                            horizontalAlignment: Label.AlignRight
                        }

                    }
                }
            }

            Item {
                ColumnLayout {
                    spacing: 0
                    anchors.centerIn: parent
                    AppStyle.Label {
                        Layout.alignment: Qt.AlignHCenter | Qt.AlignVCenter
                        text: "Could not load users"
                        font.pixelSize: 22
                        font.weight: Font.DemiBold
                    }
                    AppStyle.Label {
                        Layout.alignment: Qt.AlignHCenter | Qt.AlignVCenter
                        Layout.topMargin: 8
                        Layout.bottomMargin: 8
                        text: usersView.viewModel ? usersView.viewModel.errorMessage : ""
                        color: AppStyle.Theme.colors.mutedText
                    }
                    AppStyle.Button {
                        Layout.alignment: Qt.AlignHCenter | Qt.AlignVCenter
                        Layout.preferredWidth: implicitWidth + 36
                        Layout.preferredHeight: implicitHeight + 18
                        text: "Try again"
                        onClicked: usersView.viewModel.retry()
                    }
                }
            }
        }
    }
}
