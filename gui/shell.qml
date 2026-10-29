import QtQuick
import QtQuick.Layouts
import Quickshell
import Quickshell.Io
import Quickshell.Wayland

PanelWindow {
    id: win
    color: "transparent"
    exclusionMode: ExclusionMode.Ignore
    WlrLayershell.namespace: "hyprstyle"
    WlrLayershell.layer: WlrLayer.Overlay
    WlrLayershell.keyboardFocus: WlrKeyboardFocus.Exclusive

    anchors {
        top: true
        bottom: true
        left: true
        right: true
    }

    readonly property string bin: Quickshell.env("HYPRSTYLE_BIN") || "hyprstyle"

    property var info: ({ styles: [], folders: [], current: null })
    property var shownStyle: null
    property string selected: ""
    property string pending: ""
    property string folder: ""
    property string query: ""
    property string status: ""
    property string confirming: ""
    property string lastCommand: ""
    property string lastJson: ""
    property int tab: 0
    property bool changelogOpen: false
    property bool hidden: false
    property bool revealed: false
    property bool started: false
    property bool intro: true
    property int stamp: 0

    readonly property var selectedStyle: {
        const list = info.styles || []
        for (let i = 0; i < list.length; i++) {
            if (list[i].name === selected)
                return list[i]
        }
        return null
    }

    readonly property var changelogs: info.changelogs || []

    readonly property var latestChangelog: changelogs.length > 0 ? changelogs[changelogs.length - 1] : null

    readonly property string latestSummary: latestChangelog
        ? latestChangelog.log.replace(/^\s*[-*]\s*/gm, "").replace(/\s*\n+\s*/g, "  ·  ")
        : ""

    readonly property var shown: {
        const q = query.trim().toLowerCase()
        return (info.styles || []).filter(s => {
            if (folder !== "" && s.folder !== folder)
                return false
            if (q === "")
                return true
            return s.name.toLowerCase().includes(q)
                || s.aliases.some(a => a.toLowerCase().includes(q))
        })
    }

    onSelectedChanged: confirming = ""
    onStatusChanged: statusFade.restart()

    onConfirmingChanged: {
        if (confirming !== "")
            confirmReset.restart()
    }

    onSelectedStyleChanged: {
        if (selectedStyle !== null)
            shownStyle = selectedStyle
    }

    Process {
        id: loader
        command: [win.bin, "json"]
        stdout: StdioCollector {
            onStreamFinished: {
                if (text === win.lastJson)
                    return

                win.lastJson = text

                try {
                    const offset = stylesTab.grid.contentY
                    win.info = JSON.parse(text)

                    if (win.folder !== "" && !(win.info.folders || []).includes(win.folder))
                        win.folder = ""

                    if (win.pending !== "") {
                        win.selected = win.pending
                        win.pending = ""
                    } else if (!win.started) {
                        win.selected = win.info.current || ""
                    }

                    if (win.selectedStyle === null)
                        win.selected = ""

                    if (!win.started)
                        introEnd.start()

                    win.started = true

                    componentsTab.sync(win.info.componentConfigs || [])

                    Qt.callLater(() => {
                        stylesTab.grid.contentY = offset
                        stylesTab.grid.returnToBounds()
                    })
                } catch (e) {
                    win.status = "Couldn't read the style list"
                }
            }
        }
    }

    Process {
        id: runner
        stdout: StdioCollector {
            onStreamFinished: {
                const parts = text.split(/[\r\n]+/).filter(s => s.trim() !== "")
                win.status = parts.length ? parts[parts.length - 1].replace("✓", "").trim() : ""
            }
        }
        onExited: (exitCode, exitStatus) => {
            if (win.lastCommand === "--save-components")
                componentsTab.saveFinished(exitCode === 0)

            win.hidden = false
            win.visible = true
            win.revealed = true

            if (["save", "undo", "setpreview", "deletepreview"].includes(win.lastCommand))
                win.stamp++

            win.lastJson = ""
            loader.running = true
        }
    }

    Timer {
        id: delay
        interval: 450
        property var args: []
        onTriggered: win.run(args)
    }

    Timer {
        id: confirmReset
        interval: 3000
        onTriggered: win.confirming = ""
    }

    Timer {
        id: introEnd
        interval: 900
        onTriggered: win.intro = false
    }

    Timer {
        id: closer
        interval: 220
        onTriggered: Qt.quit()
    }

    Timer {
        id: poll
        interval: 3000
        repeat: true
        running: true
        property int left: 10

        onTriggered: {
            if (--left <= 0)
                running = false

            if (!loader.running)
                loader.running = true
        }
    }

    Connections {
        target: Quickshell
        function onLastWindowClosed() {
            if (!win.hidden)
                Qt.quit()
        }
    }

    Shortcut {
        sequence: "Escape"
        onActivated: win.dismiss()
    }

    Component.onCompleted: {
        loader.running = true
        revealed = true
    }

    function dismiss() {
        if (changelogOpen) {
            changelogOpen = false
            return
        }

        if (componentsTab.dirty && confirming !== "close") {
            confirming = "close"
            tab = 1
            status = "You have unsaved component changes. Press Escape again to discard them."
            return
        }

        revealed = false
        closer.restart()
    }

    function run(args) {
        lastCommand = args[0]
        runner.command = [bin].concat(args)
        runner.running = true
    }

    function runHidden(args) {
        hidden = true
        revealed = false
        visible = false
        delay.args = args
        delay.start()
    }

    function setPreview(source) {
        const path = source.trim()
        if (selected === "" || path === "")
            return
        run(["setpreview", selected, path])
    }

    Rectangle {
        anchors.fill: parent
        color: "#88000000"
        opacity: win.revealed ? 1 : 0

        Behavior on opacity { NumberAnimation { duration: 220 } }

        MouseArea {
            anchors.fill: parent
            onClicked: win.dismiss()
        }
    }

    Rectangle {
        id: card
        anchors.centerIn: parent
        width: Math.min(1060, parent.width - 80)
        height: Math.min(680, parent.height - 80)
        radius: 6
        color: "#16161a"
        border.width: 1
        border.color: "#2a2a30"
        opacity: win.revealed ? 1 : 0
        scale: win.revealed ? 1 : 0.96

        Behavior on opacity { NumberAnimation { duration: 220; easing.type: Easing.OutCubic } }
        Behavior on scale { NumberAnimation { duration: 280; easing.type: Easing.OutCubic } }

        MouseArea {
            anchors.fill: parent
        }

        Item {
            anchors.fill: parent

            ColumnLayout {
                anchors.fill: parent
                anchors.margins: 16
                spacing: 0

                Item {
                    Layout.fillWidth: true
                    Layout.preferredHeight: 34

                    Rectangle {
                        anchors.left: parent.left
                        anchors.right: parent.right
                        anchors.bottom: parent.bottom
                        height: 1
                        color: "#2a2a30"
                    }

                    Row {
                        TabLabel {
                            label: "Styles"
                            on: win.tab === 0
                            onClicked: win.tab = 0
                        }

                        TabLabel {
                            label: "Components"
                            on: win.tab === 1
                            dot: componentsTab.dirty
                            onClicked: win.tab = 1
                        }
                    }
                }

                Item {
                    Layout.fillWidth: true
                    Layout.preferredHeight: win.info.updateAvailable === true ? 42 : 0
                    opacity: win.info.updateAvailable === true ? 1 : 0
                    clip: true

                    Behavior on Layout.preferredHeight { NumberAnimation { duration: 260; easing.type: Easing.OutCubic } }
                    Behavior on opacity { NumberAnimation { duration: 260 } }

                    RowLayout {
                        anchors.left: parent.left
                        anchors.right: parent.right
                        anchors.bottom: parent.bottom
                        height: 30

                        Muted {
                            Layout.fillWidth: true
                            text: "Version " + win.info.latestVersion + " is out (you have " + win.info.version + ")"
                            elide: Text.ElideRight
                        }

                        Btn {
                            label: "Update"
                            onClicked: win.run(["update"])
                        }
                    }
                }

                Item {
                    Layout.fillWidth: true
                    Layout.fillHeight: true
                    Layout.topMargin: 14

                    StylesTab {
                        id: stylesTab
                        anchors.fill: parent
                        shell: win
                        opacity: win.tab === 0 ? 1 : 0
                        visible: opacity > 0

                        Behavior on opacity { NumberAnimation { duration: 160 } }
                    }

                    ComponentsTab {
                        id: componentsTab
                        anchors.fill: parent
                        shell: win
                        opacity: win.tab === 1 ? 1 : 0
                        visible: opacity > 0

                        Behavior on opacity { NumberAnimation { duration: 160 } }
                    }
                }

                Muted {
                    id: statusText
                    Layout.fillWidth: true
                    Layout.topMargin: 12
                    text: win.status !== "" ? win.status
                        : win.tab === 0 ? "Double-click a style to apply it"
                        : "Changes are written to your config when you save."
                    elide: Text.ElideRight
                }

                NumberAnimation {
                    id: statusFade
                    target: statusText
                    property: "opacity"
                    from: 0
                    to: 1
                    duration: 260
                    easing.type: Easing.OutCubic
                }

                Rectangle {
                    Layout.fillWidth: true
                    Layout.topMargin: 8
                    Layout.preferredHeight: win.latestChangelog !== null ? 32 : 0
                    visible: win.latestChangelog !== null
                    radius: 4
                    color: stripHit.containsMouse ? "#1f1f25" : "#1a1a1f"
                    border.width: 1
                    border.color: stripHit.containsMouse ? "#3c3c46" : "#2a2a30"

                    Behavior on color { ColorAnimation { duration: 140 } }
                    Behavior on border.color { ColorAnimation { duration: 140 } }

                    RowLayout {
                        anchors.fill: parent
                        anchors.leftMargin: 10
                        anchors.rightMargin: 10
                        spacing: 10

                        Text {
                            text: win.latestChangelog ? win.latestChangelog.version : ""
                            color: "#7aa2f7"
                            font.pixelSize: 12
                        }

                        Text {
                            Layout.fillWidth: true
                            text: win.latestSummary
                            color: "#a8a8b2"
                            font.pixelSize: 12
                            elide: Text.ElideRight
                        }

                        Text {
                            text: "See all changes  ▴"
                            color: stripHit.containsMouse ? "#d4d4dc" : "#62626c"
                            font.pixelSize: 11

                            Behavior on color { ColorAnimation { duration: 140 } }
                        }
                    }

                    MouseArea {
                        id: stripHit
                        anchors.fill: parent
                        hoverEnabled: true
                        cursorShape: Qt.PointingHandCursor
                        onClicked: win.changelogOpen = true
                    }
                }
            }

            ChangelogPanel {
                anchors.fill: parent
                open: win.changelogOpen
                entries: win.changelogs
                onClosed: win.changelogOpen = false
            }
        }
    }
}
