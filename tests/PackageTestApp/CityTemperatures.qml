/***************************************************************************************************
 Copyright (C) 2025 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/
import QtQuick
import QtQuick.Controls
import QtQuick.Controls.Basic
import QtQml.Models

Window {
    id: window
    visible: true
    width: 640
    height: 480
    title: "City Temperatures"

    ListModel {
        id: cities
        ListElement { city: "Berlin" }
        ListElement { city: "Oslo"   }
        ListElement { city: "Oulu"   }
    }

    GridView {
        focus: true
        id: citiesGrid;
        model: cities;
        delegate: cityDelegate
        anchors.fill: parent; cellWidth: window.width / 4; cellHeight: (window.height - 32) / 3
        header: Rectangle {
            id: addCity
            width: citiesGrid.width; height: 32
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)
            TextField {
                focus: true
                placeholderText: "Add city"
                anchors.top: parent.top; anchors.left: parent.left; anchors.right: parent.right; anchors.margins: 2
                color: "black"; placeholderTextColor: "gray"
                background: Rectangle {
                    color: "transparent"
                    border.color: "transparent"
                }
                onAccepted: {
                    cities.insert(0, { "city": text })
                    clear()
                    citiesGrid.positionViewAtBeginning()
                }
                onFocusChanged: {
                    focus = true
                }
            }
        }
    }

    Component {
        id: cityDelegate
        Rectangle {
            id: wrapper
            width: window.width / 4; height: (window.height - 32) / 3;
            color: "#53d769"; border.color: Qt.lighter(color, 1.1)

            Weather {
                id: weather
                location: city
            }

            Text {
                text: weather.isValid ? (weather.temperature.toFixed(2) + " " + weather.temperatureUnits) : "???"
                anchors.centerIn: parent; font.pixelSize: wrapper.width / 6
            }
            Text {
                text: city
                font.pixelSize: wrapper.width / 10
                anchors.top: parent.top; anchors.left: parent.left; anchors.margins: 5
            }
            Button {
                id: removeCity
                text: "\u2715"
                anchors.top: parent.top; anchors.right: parent.right; width: 20; height: 20; anchors.margins: 5
                background: Rectangle {
                    color: removeCity.down ? Qt.lighter(wrapper.color, 1.1) : wrapper.color
                    border.color: Qt.lighter(wrapper.color, 1.1)
                    border.width: 1
                }
                onClicked: cities.remove(index, 1)
            }
        }
    }    
}
