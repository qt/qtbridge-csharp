// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

pragma ComponentBehavior: Bound

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

import DefaultStyle as AppStyle

Flickable {
    id: userDetailsView
    required property var viewModel

    property string fullName
    property string initials
    property color avatarColor
    property string email
    property string phone
    property string cell
    property int age
    property string street
    property string city
    property string region
    property string country
    property string postcode
    property string nationality

    onViewModelChanged: {
        if (viewModel) {
            fullName = viewModel.user.fullName
            initials = viewModel.user.initials
            avatarColor = AppStyle.Theme.colors.avatarColor(viewModel.user.avatarColorIndex)
            email = viewModel.user.email
            phone = viewModel.user.phone
            cell = viewModel.user.cell
            age = viewModel.user.age
            street = viewModel.user.street
            city = viewModel.user.city
            region = viewModel.user.state
            country = viewModel.user.country
            postcode = viewModel.user.postcode
            nationality = viewModel.user.nationality
        }
    }

    contentHeight: layout.height + 34
    clip: true
    ScrollBar.vertical: ScrollBar {
        policy: ScrollBar.AlwaysOn
        active: true
        implicitWidth: 20
    }

    ColumnLayout {
        id: layout
        spacing: 0
        anchors.top: parent.top
        anchors.horizontalCenter: parent.horizontalCenter
        anchors.margins: 28
        AppStyle.Button {
            text: "‹ Back to directory"
            Layout.preferredWidth: implicitWidth + 28
            Layout.preferredHeight: implicitHeight + 16
            enabled: userDetailsView.viewModel
            onClicked: userDetailsView.viewModel.back()
        }
        Rectangle {
            Layout.topMargin: 22
            Layout.preferredWidth: cardLayout.implicitWidth + 34
            Layout.preferredHeight: cardLayout.implicitHeight + 34
            color: "white"
            border.color: AppStyle.Theme.colors.border
            radius: 14
            RowLayout {
                id: cardLayout
                anchors.fill: parent
                Rectangle {
                    Layout.alignment: Qt.AlignTop
                    Layout.leftMargin: 60
                    Layout.rightMargin: 25
                    Layout.topMargin: 25
                    width: 170
                    height: 170
                    radius: 85
                    color: userDetailsView.avatarColor
                    Text {
                        anchors.centerIn: parent
                        font.pixelSize: 52
                        font.weight: Font.DemiBold
                        text: userDetailsView.initials
                        horizontalAlignment: Label.AlignHCenter
                        verticalAlignment: Label.AlignVCenter
                    }
                }
                ColumnLayout {
                    Layout.rightMargin: 34
                    spacing: 2

                    AppStyle.Label {
                        Layout.fillWidth: true
                        Layout.topMargin: 6
                        font.pixelSize: 34
                        font.weight: Font.DemiBold
                        text: userDetailsView.fullName
                    }

                    AppStyle.Label {
                        Layout.topMargin: 6
                        Layout.bottomMargin: 6
                        font.pixelSize: 16
                        color: AppStyle.Theme.colors.accent
                        text: userDetailsView.email
                    }

                    AppStyle.SectionTitle {
                        Layout.topMargin: 20
                        Layout.bottomMargin: 8
                        text: "Contact"
                    }
                    AppStyle.Label {
                        text: "Phone: " + userDetailsView.phone
                    }
                    AppStyle.Label {
                        text: "Mobile: " + userDetailsView.cell
                    }

                    AppStyle.SectionTitle {
                        Layout.topMargin: 20
                        Layout.bottomMargin: 8
                        text: "Personal"
                    }
                    AppStyle.Label {
                        text: "Age: " + userDetailsView.age
                    }
                    AppStyle.Label {
                        text: "Nationality: " + userDetailsView.nationality
                    }

                    AppStyle.SectionTitle {
                        Layout.topMargin: 20
                        Layout.bottomMargin: 8
                        text: "Address"
                    }
                    AppStyle.Label {
                        text: userDetailsView.street
                    }
                    AppStyle.Label {
                        text: userDetailsView.postcode + " " + userDetailsView.city
                    }
                    AppStyle.Label {
                        text: userDetailsView.region + ", " + userDetailsView.country
                    }
                }
            }
        }
    }
}
