// Copyright (C) 2026 The Qt Company Ltd.
// SPDX-License-Identifier: LicenseRef-Qt-Commercial OR BSD-3-Clause

import QtQuick
import QtQuick.Controls
import QtQuick.Dialogs
import QtQuick.Layouts

ApplicationWindow {
    id: mainWindow
    width: 1000
    height: 760
    visible: true
    title: "Dynamic Object Test"
    color: "#f3f4f6"

    Action {
        id: openFileAction;
        text: "Open File...";
        shortcut: "Ctrl+O";
        onTriggered: fixturesFileDialog.open()
    }
    Action {
        id: exitAction;
        text: "Exit";
        shortcut: "Ctrl+Q";
        onTriggered: Qt.quit()
    }
    Action {
        id: evalAction;
        text: "Open Evaluation...";
        shortcut: "Ctrl+E";
        onTriggered: openEvaluationWindow()
    }
    Action {
        id: modelAction;
        text: "Open model view...";
        shortcut: "Ctrl+M";
        onTriggered: openModelWindow()
    }
    Action {
        id: logAction;
        text: "Open Log...";
        shortcut: "Ctrl+L";
        onTriggered: openLogWindow()
    }
    Action {
        id: runAllAction;
        text: "Run all";
        shortcut: "F5";
        onTriggered: runAllFixtures()
    }
    Action {
        id: resetAction;
        text: "Reset all";
        shortcut: "Ctrl+R";
        onTriggered: resetFixtures()
    }

    menuBar: AppMenuBar {
        fileOpenAction: openFileAction
        applicationExitAction: exitAction
        viewModelAction: modelAction
        viewEvaluationAction: evalAction
        viewLogAction: logAction
        fixturesRunAllAction: runAllAction
        fixturesResetAction: resetAction
    }

    footer: AppStatusBar {
        buildObject: bt
        loadObject: lt
        fixturesFileName: mainWindow.fixturesFileName
        fixturesLoadError: mainWindow.fixturesLoadError
    }

    property string fixturesFileName: ""
    property string fixturesLoadError: ""

    property string logText: ""

    readonly property var modelFixtures: [
        {
            name: "Tree model",
            kind: "tree",
            expected: "Both panes show identical First Name and LastName headers, "
                + "the Beatles and Rolling Stones top-level groups, and the same "
                + "member rows when each group is expanded.",
            buildModel: bt,
            loadModel: lt
        }
    ]

    function appendLog(icon, message) {
        logText += icon + " " + message + "\n"
    }

    ListModel {
        id: fixtures
    }

    function joinLines(value) {
        // Support multiline display text in fixtures.json.
        return Array.isArray(value) ? value.join("\n") : value
    }

    function formatExpression(expression) {
        return expression.replace(/;\s+/g, ";\n")
    }

    function defaultFixturesUrl() {
        return Qt.resolvedUrl("../fixtures.json").toString()
    }

    function loadFixturesFrom(url) {
        fixturesFileName = decodeURIComponent(url.substring(url.lastIndexOf("/") + 1))

        const request = new XMLHttpRequest()
        request.open("GET", url, false)
        request.send()

        // Local file:// requests report success as status 0, not 200.
        if (request.status !== 200 && request.status !== 0) {
            fixturesLoadError = "Could not read the file."
            return false
        }

        let definitions
        try {
            definitions = JSON.parse(request.responseText)
        } catch (error) {
            fixturesLoadError = "Invalid JSON: " + error
            return false
        }

        if (!Array.isArray(definitions)) {
            fixturesLoadError = "Expected a JSON array of fixtures."
            return false
        }

        fixtures.clear()
        for (const definition of definitions) {
            const expression = joinLines(definition.expression)
            fixtures.append({
                name: definition.name,
                expression: expression,
                display: definition.display !== undefined
                    ? joinLines(definition.display)
                    : formatExpression(expression),
                buildStatus: "Not run",
                buildDetails: "",
                loadStatus: "Not run",
                loadDetails: ""
            })
        }

        fixturesLoadError = ""
        return true
    }

    function positionViewWindow() {
        viewWindow.x = mainWindow.x + mainWindow.width + 10
        viewWindow.y = mainWindow.y
    }

    function openModelWindow() {
        positionViewWindow()
        viewWindow.show()
        viewWindow.raise()
        viewWindow.requestActivate()
    }

    function openLogWindow() {
        logWindow.x = mainWindow.x - logWindow.width - 10
        logWindow.y = mainWindow.y
        logWindow.show()
        logWindow.raise()
        logWindow.requestActivate()
    }

    function openEvaluationWindow() {
        evaluationWindow.x = mainWindow.x + 40
        evaluationWindow.y = mainWindow.y + 40
        evaluationWindow.show()
        evaluationWindow.raise()
        evaluationWindow.requestActivate()
    }

    function setResult(index, path, passed, details) {
        fixtures.setProperty(index, path + "Status", passed ? "PASS" : "FAIL")
        fixtures.setProperty(index, path + "Details", details)
    }

    // Preserve bt and lt state.
    function resetFixtures() {
        for (let index = 0; index < fixtures.count; ++index) {
            fixtures.setProperty(index, "buildStatus", "Not run")
            fixtures.setProperty(index, "buildDetails", "")
            fixtures.setProperty(index, "loadStatus", "Not run")
            fixtures.setProperty(index, "loadDetails", "")
        }
    }

    function runFixture(index, target, path) {
        let passed = false
        let details = ""
        const name = fixtures.get(index).name

        try {
            passed = !!eval(fixtures.get(index).expression)
            details = passed ? "Passed" : "Fixture returned false"
            appendLog(passed ? "✔" : "✘", "[" + path + "] " + name + " ➜ " + details)
        } catch (error) {
            details = error.toString()
            appendLog("⚠", "[" + path + "] " + name + " ➜ " + details)
        }

        setResult(index, path, passed, details)
    }

    function runAllFixtures() {
        for (let index = 0; index < fixtures.count; ++index) {
            runFixture(index, bt, "build")
            runFixture(index, lt, "load")
        }
        apiFixturesPanel.scrollToFirstFailure()
    }

    function evaluate(expression) {
        try {
            const result = eval(expression)
            evaluationWindow.resultText = "Result: " + result
            appendLog(result ? "✔" : "✘", expression + " ➜ " + result)
        } catch (error) {
            evaluationWindow.resultText = "Error: " + error
            appendLog("⚠", expression + " ➜ " + error)
        }
    }

    ModelWindow {
        id: viewWindow
        height: mainWindow.height
        fixtures: mainWindow.modelFixtures
    }

    LogWindow {
        id: logWindow
        height: mainWindow.height
        logText: mainWindow.logText
        onClearRequested: mainWindow.logText = ""
    }

    EvaluationWindow {
        id: evaluationWindow
        onEvaluationRequested: expression => mainWindow.evaluate(expression)
    }

    onXChanged: positionViewWindow()
    onYChanged: positionViewWindow()
    onWidthChanged: positionViewWindow()

    Component.onCompleted: loadFixturesFrom(defaultFixturesUrl())

    FileDialog {
        id: fixturesFileDialog
        title: "Load fixtures file"
        nameFilters: ["JSON files (*.json)", "All files (*)"]
        onAccepted: loadFixturesFrom(selectedFile.toString())
    }

    LoadTimeType {
        id: lt

        BuildTimeType {
            id: bt
        }
    }

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 24

        ApiFixturesPanel {
            id: apiFixturesPanel
            Layout.fillWidth: true
            Layout.fillHeight: true
            fixturesModel: fixtures
            onRunRequested: (index, path) => runFixture(index, path === "build" ? bt : lt, path)
            onLogRequested: openLogWindow()
        }
    }
}
