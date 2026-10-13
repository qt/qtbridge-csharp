// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause
// Toggle.qml — IntelliQHome Design System — on/off switch
// Maps to Figma: Style Library for IntelliQHome → Switch (node 117:1137)
//
// Figma variants (re-inspected via MCP, 2026-07-09):
//   Selected: True | False    State: Idle | Hover | Pressed
// Figma-verified: track 68×32 pill, radius_full.
//   ON  → track highlight_strong (hover: highlight_medium, pressed: highlight_light),
//         no border, base_full_white handle 24px (right)
//   OFF → transparent track with 2px control_inactive border, control_inactive
//         handle 16px (left); hover/pressed tint border+handle with
//         highlight_medium / highlight_light
//
// Usage — uncontrolled (the caller owns `checked`):
//   import IntelliQHome
//   Toggle { text: "Dark mode"; checked: Theme.isDark; onToggled: Theme.isDark = checked }
//
// Usage — controlled (something else owns the state):
//   Toggle { boundValue: device.on; onUserSet: (v) => device.on = v }

import QtQuick
import QtQuick.Controls.Basic
import Weather.Controls

Switch {
    id: root

    // Controlled mode. Bind `boundValue` and the switch always shows that value,
    // never its own: a click reports intent through userSet() and leaves `checked`
    // following the binding. So the switch only moves once the owner agrees —
    // which is what a device whose state is a physical report (a lock's bolt)
    // needs. Leave `boundValue` unset and the switch behaves as a plain Switch,
    // owning `checked` itself.
    property var boundValue: undefined
    signal userSet(bool value)

    readonly property bool _controlled: root.boundValue !== undefined

    // Bound here rather than installed in Component.onCompleted, which is what a
    // switch created already on needs: Behaviors are armed before onCompleted, so
    // a binding installed there reads as a change and animates off-to-on. A value
    // established during creation animates nothing. An instantiation-site
    // `checked:` still overrides this, which is what uncontrolled callers do.
    checked: root.boundValue === true

    // Clicking has already flipped `checked` and destroyed the binding by the
    // time this runs, so re-establish it before reporting the intent.
    onClicked: {
        if (!root._controlled)
            return
        const intent = !(root.boundValue === true)
        root.checked = Qt.binding(() => root.boundValue === true)
        root.userSet(intent)
    }

    property string label: text
    onLabelChanged: root.text = label

    readonly property int _trackW: 68
    readonly property int _trackH: 32
    readonly property int _thumbOn:  24
    readonly property int _thumbOff: 16
    readonly property int _gap:      Spacing.xs   // 8 px label gap

    // OFF-state accent: border + handle share one color per Figma state
    readonly property color _offAccent:
        root.pressed        ? Theme.highlight_light
      : _hover.hovered      ? Theme.highlight_medium
      :                       Theme.control_inactive

    spacing: _gap

    indicator: Rectangle {
        id: _track
        implicitWidth:  root._trackW
        implicitHeight: root._trackH
        radius: height / 2
        anchors.verticalCenter: parent.verticalCenter

        color: !root.enabled ? "transparent"
             : root.checked  ? (root.pressed   ? Theme.highlight_light
                              : _hover.hovered ? Theme.highlight_medium
                              :                  Theme.highlight_strong)
             :                  "transparent"

        border.width: root.checked && root.enabled ? 0 : 2
        border.color: root.enabled ? root._offAccent : Theme.control_inactive

        HoverHandler { id: _hover; enabled: root.enabled }
        Behavior on color { ColorAnimation { duration: 150 } }
        Behavior on border.color { ColorAnimation { duration: 150 } }

        Rectangle {
            id: _thumb
            width:  root.checked ? root._thumbOn : root._thumbOff
            height: width
            radius: height / 2
            color:  !root.enabled ? Theme.text_subtle
                  : root.checked  ? Theme.base_full_white
                  :                 root._offAccent
            anchors.verticalCenter: parent.verticalCenter
            x: root.checked ? _track.width - width - 4
                            : _track.width / 2 - 18 - width / 2

            Behavior on x     { NumberAnimation { duration: 150; easing.type: Easing.InOutQuad } }
            Behavior on width { NumberAnimation { duration: 150; easing.type: Easing.InOutQuad } }
            Behavior on color { ColorAnimation { duration: 150 } }
        }

        Rectangle {
            anchors { fill: parent; margins: -3 }
            radius: parent.radius + 3
            color: "transparent"
            border.color: Theme.highlight_strong     // TODO: add Theme.stroke_focus token
            border.width: 2
            visible: root.visualFocus
        }
    }

    contentItem: Text {
        leftPadding: root.indicator.width + root.spacing
        text: root.text
        font.family:    Typography.font_body
        font.pixelSize: Typography.body_2_size
        font.weight:    Typography.weight_light
        color: root.enabled ? Theme.text_strong : Theme.text_subtle
        verticalAlignment: Text.AlignVCenter
        Behavior on color { ColorAnimation { duration: Theme.duration_fast } }
    }

    HoverHandler { cursorShape: root.enabled ? Qt.PointingHandCursor : Qt.ArrowCursor }
}
