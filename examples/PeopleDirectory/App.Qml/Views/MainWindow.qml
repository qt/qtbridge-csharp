// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

import Application
import DefaultStyle as AppStyle

ApplicationWindow {
    id: mainWindow

    required property var viewModel

    title: viewModel.title
    visible: true
    width: 1100
    height: 720
    minimumWidth: 760
    minimumHeight: 520
    color: AppStyle.Theme.colors.appBackground

    Component.onCompleted: {
        x = (Screen.width - width) / 2
        y = (Screen.height - height) / 2
    }

    GridLayout {
        anchors.fill: parent
        columns: 1
        columnSpacing: 0
        rowSpacing: 0

        AppStyle.Label {
            Layout.fillWidth: true
            Layout.preferredHeight: 72
            background: Rectangle {
                color: AppStyle.Theme.colors.surface
                border.color: AppStyle.Theme.colors.border
            }
            text: "People"
            horizontalAlignment: Text.AlignLeft
            verticalAlignment: Text.AlignVCenter
            font.pixelSize: 26
            font.weight: Font.DemiBold
            padding: 28
        }

        StackLayout {
            id: views
            Layout.fillWidth: true
            Layout.fillHeight: true

            LoadingView {
                id: loadingView
                animateIndicator: StackLayout.isCurrentItem
            }

            UsersView {
                id: usersView
                viewModel: mainWindow.viewModel.current
            }

            UserDetailsView {
                id: userDetailsView
                viewModel: mainWindow.viewModel.current
            }

            currentIndex: switch (viewModel.current.getType()) {
                case ViewModelTypes.loadingViewModel: return loadingView.StackLayout.index
                case ViewModelTypes.usersViewModel: return usersView.StackLayout.index
                case ViewModelTypes.userDetailsViewModel: return userDetailsView.StackLayout.index
                default: return -1
            }
        }
    }
}
