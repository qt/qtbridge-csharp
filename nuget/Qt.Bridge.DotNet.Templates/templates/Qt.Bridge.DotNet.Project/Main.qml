import QtQuick 2.3
import QtQuick.Layouts 1.15
import QtQuick.Controls 2.15

ApplicationWindow {
    id: win
    visible: true
    width: 365
    height: 510
    title: "Qt.Bridge.DotNet"
    color: "#121212"

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 24
        spacing: 20

        Label {
            text: "Hello, World!"
            font.pixelSize: 40
            color: "white"
            horizontalAlignment: Text.AlignHCenter
            Layout.alignment: Qt.AlignHCenter
        }

        ColumnLayout {
            Layout.fillWidth: true
            spacing: 6
            Label {
                text: "Welcome to Qt.Bridge.DotNet"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: 24
                color: "#cfcfcf"
            }

            Label {
                text: "QML Multi-language App UI"
                Layout.fillWidth: true
                horizontalAlignment: Text.AlignHCenter
                font.pixelSize: 24
                color: "white"
                wrapMode: Text.WordWrap
            }
        }

        Button {
            id: counterBtn
            text: Counter.clicks === 0 ? "Click me" : "Clicked " + Counter.clicks + " times"

            Layout.fillWidth: true
            Layout.preferredHeight: 48
            font.pixelSize: 16

            hoverEnabled: false
            focusPolicy: Qt.NoFocus

            readonly property color brandGreen: "#41cd52"

            background: Rectangle {
                radius: 12
                border.width: 0
                color: counterBtn.down
                    ? Qt.darker(counterBtn.brandGreen, 1.2)
                    : counterBtn.brandGreen
            }

            contentItem: Text {
                text: counterBtn.text
                color: "white"
                font: counterBtn.font
                horizontalAlignment: Text.AlignHCenter
                verticalAlignment: Text.AlignVCenter
                elide: Text.ElideRight
            }

            onClicked: Counter.clicks += 1
        }
    }
}
