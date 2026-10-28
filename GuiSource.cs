static partial class Gui
{
    private const string Source = """"
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

            component Btn: Rectangle {
                id: btn
                property string label: ""
                property bool accent: false
                property bool danger: false
                signal clicked()

                implicitWidth: caption.implicitWidth + 24
                implicitHeight: 30
                radius: 4
                opacity: enabled ? 1 : 0.4
                scale: hit.pressed ? 0.97 : 1
                border.width: accent ? 0 : 1
                border.color: danger ? "#e06c75" : "#34343c"
                color: accent ? (hit.containsMouse ? "#8db0f9" : "#7aa2f7")
                    : danger ? (hit.containsMouse ? "#3a2328" : "#2a1b1f")
                    : (hit.containsMouse ? "#2a2a31" : "#002a2a31")

                Behavior on color { ColorAnimation { duration: 140 } }
                Behavior on border.color { ColorAnimation { duration: 140 } }
                Behavior on opacity { NumberAnimation { duration: 140 } }
                Behavior on scale { NumberAnimation { duration: 90; easing.type: Easing.OutQuad } }

                Text {
                    id: caption
                    anchors.centerIn: parent
                    text: btn.label
                    color: btn.accent ? "#14141a" : btn.danger ? "#eba0a6" : "#d4d4dc"
                    font.pixelSize: 13

                    Behavior on color { ColorAnimation { duration: 140 } }
                }

                MouseArea {
                    id: hit
                    anchors.fill: parent
                    hoverEnabled: true
                    cursorShape: Qt.PointingHandCursor
                    onClicked: btn.clicked()
                }
            }

            component Input: Rectangle {
                id: box
                property alias text: edit.text
                property string hint: ""
                signal accepted()

                implicitHeight: 30
                radius: 4
                color: "#1b1b20"
                border.width: 1
                border.color: edit.activeFocus ? "#7aa2f7" : "#2e2e36"

                Behavior on border.color { ColorAnimation { duration: 160 } }

                TextInput {
                    id: edit
                    anchors.fill: parent
                    anchors.leftMargin: 8
                    anchors.rightMargin: 8
                    verticalAlignment: TextInput.AlignVCenter
                    color: "#d4d4dc"
                    selectionColor: "#7aa2f7"
                    selectedTextColor: "#14141a"
                    font.pixelSize: 13
                    clip: true
                    onAccepted: box.accepted()
                }

                Text {
                    anchors.fill: edit
                    verticalAlignment: Text.AlignVCenter
                    text: box.hint
                    color: "#62626c"
                    font.pixelSize: 13
                    elide: Text.ElideRight
                    opacity: edit.text === "" ? 1 : 0

                    Behavior on opacity { NumberAnimation { duration: 120 } }
                }
            }

            component Muted: Text {
                color: "#8a8a94"
                font.pixelSize: 12
            }

            component SideRow: Rectangle {
                id: row
                property string label: ""
                property bool on: false
                signal clicked()

                implicitHeight: 28
                color: on ? "#23232a" : rowHit.containsMouse ? "#1c1c22" : "#001c1c22"

                Behavior on color { ColorAnimation { duration: 140 } }

                Rectangle {
                    width: 2
                    height: parent.height
                    color: "#7aa2f7"
                    opacity: row.on ? 1 : 0

                    Behavior on opacity { NumberAnimation { duration: 180 } }
                }

                Text {
                    anchors.fill: parent
                    anchors.leftMargin: 12
                    verticalAlignment: Text.AlignVCenter
                    text: row.label
                    color: row.on ? "#e6e6ee" : "#a8a8b2"
                    font.pixelSize: 13
                    elide: Text.ElideRight

                    Behavior on color { ColorAnimation { duration: 140 } }
                }

                MouseArea {
                    id: rowHit
                    anchors.fill: parent
                    hoverEnabled: true
                    onClicked: row.clicked()
                }
            }

            Process {
                id: loader
                command: ["@BIN@", "json"]
                stdout: StdioCollector {
                    onStreamFinished: {
                        if (text === win.lastJson)
                            return

                        win.lastJson = text

                        try {
                            const offset = grid.contentY
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

                            Qt.callLater(() => {
                                grid.contentY = offset
                                grid.returnToBounds()
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
                onExited: {
                    win.hidden = false
                    win.visible = true
                    win.revealed = true

                    if (["save", "setpreview", "deletepreview"].includes(win.lastCommand))
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
                revealed = false
                closer.restart()
            }

            function run(args) {
                lastCommand = args[0]
                runner.command = ["@BIN@"].concat(args)
                runner.running = true
            }

            function runHidden(args) {
                hidden = true
                revealed = false
                visible = false
                delay.args = args
                delay.start()
            }

            function saveCurrent() {
                const name = saveName.text.trim()
                if (name === "")
                    return
                pending = name
                saveName.text = ""
                runHidden(["save", name])
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

                ColumnLayout {
                    anchors.fill: parent
                    anchors.margins: 16
                    spacing: 0

                    RowLayout {
                        Layout.fillWidth: true
                        spacing: 8

                        Input {
                            Layout.preferredWidth: 220
                            hint: "Search"
                            onTextChanged: win.query = text
                        }

                        Item { Layout.fillWidth: true }

                        Input {
                            id: saveName
                            Layout.preferredWidth: 260
                            hint: "Save current setup as..."
                            onAccepted: win.saveCurrent()
                        }

                        Btn {
                            label: "Save"
                            accent: true
                            enabled: saveName.text.trim() !== ""
                            onClicked: win.saveCurrent()
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
                            }

                            Btn {
                                label: "Update"
                                onClicked: win.run(["update"])
                            }
                        }
                    }

                    RowLayout {
                        Layout.fillWidth: true
                        Layout.fillHeight: true
                        Layout.topMargin: 12
                        spacing: 14

                        ColumnLayout {
                            visible: (win.info.folders || []).length > 0
                            Layout.minimumWidth: 150
                            Layout.preferredWidth: 150
                            Layout.maximumWidth: 150
                            Layout.fillHeight: true
                            spacing: 0

                            SideRow {
                                Layout.fillWidth: true
                                label: "All styles"
                                on: win.folder === ""
                                onClicked: win.folder = ""
                            }

                            Repeater {
                                model: win.info.folders || []

                                delegate: SideRow {
                                    required property string modelData
                                    Layout.fillWidth: true
                                    label: modelData
                                    on: win.folder === modelData
                                    onClicked: win.folder = modelData
                                }
                            }

                            Item { Layout.fillHeight: true }
                        }

                        Item {
                            Layout.fillWidth: true
                            Layout.fillHeight: true

                            GridView {
                                id: grid
                                anchors.fill: parent
                                clip: true
                                boundsBehavior: Flickable.StopAtBounds
                                cellWidth: Math.floor(width / Math.max(1, Math.floor(width / 210)))
                                readonly property real shotHeight: (cellWidth - 12) * 0.5625
                                cellHeight: shotHeight + 46
                                model: win.shown

                                delegate: Item {
                                    id: tile
                                    required property var modelData
                                    required property int index
                                    readonly property bool picked: modelData.name === win.selected
                                    width: grid.cellWidth
                                    height: grid.cellHeight

                                    Component.onCompleted: {
                                        if (!win.intro)
                                            return
                                        opacity = 0
                                        scale = 0.94
                                        entrance.start()
                                    }

                                    SequentialAnimation {
                                        id: entrance

                                        PauseAnimation { duration: Math.min(tile.index, 14) * 24 }

                                        ParallelAnimation {
                                            NumberAnimation { target: tile; property: "opacity"; to: 1; duration: 300; easing.type: Easing.OutCubic }
                                            NumberAnimation { target: tile; property: "scale"; to: 1; duration: 340; easing.type: Easing.OutCubic }
                                        }
                                    }

                                    Rectangle {
                                        id: face
                                        anchors.fill: parent
                                        anchors.margins: 6
                                        radius: 3
                                        clip: true
                                        color: "#1d1d22"
                                        scale: tileHit.containsMouse ? 1.015 : 1
                                        border.width: tile.picked ? 2 : 1
                                        border.color: tile.picked ? "#7aa2f7" : tileHit.containsMouse ? "#3c3c46" : "#2a2a30"

                                        Behavior on scale { NumberAnimation { duration: 160; easing.type: Easing.OutCubic } }
                                        Behavior on border.width { NumberAnimation { duration: 120 } }
                                        Behavior on border.color { ColorAnimation { duration: 160 } }

                                        Rectangle {
                                            id: frame
                                            anchors.left: parent.left
                                            anchors.right: parent.right
                                            anchors.top: parent.top
                                            anchors.margins: 1
                                            height: grid.shotHeight - 1
                                            color: "#101014"

                                            Muted {
                                                anchors.centerIn: parent
                                                text: "No preview"
                                                opacity: !tile.modelData.preview || shot.status === Image.Error ? 1 : 0

                                                Behavior on opacity { NumberAnimation { duration: 200 } }
                                            }

                                            Image {
                                                id: shot
                                                anchors.fill: parent
                                                fillMode: Image.PreserveAspectCrop
                                                asynchronous: true
                                                cache: false
                                                opacity: status === Image.Ready ? 1 : 0
                                                source: tile.modelData.preview
                                                    ? "file://" + tile.modelData.preview + "?" + win.stamp
                                                    : ""

                                                Behavior on opacity { NumberAnimation { duration: 260 } }
                                            }
                                        }

                                        Row {
                                            anchors.left: parent.left
                                            anchors.right: parent.right
                                            anchors.top: frame.bottom
                                            anchors.margins: 8
                                            spacing: 6

                                            Text {
                                                visible: tile.modelData.current
                                                text: "●"
                                                color: "#7aa2f7"
                                                font.pixelSize: 11
                                                anchors.verticalCenter: parent.verticalCenter
                                            }

                                            Text {
                                                width: parent.width - (tile.modelData.current ? 17 : 0)
                                                text: tile.modelData.displayName
                                                color: "#e0e0e8"
                                                font.pixelSize: 13
                                                elide: Text.ElideRight
                                            }
                                        }

                                        MouseArea {
                                            id: tileHit
                                            anchors.fill: parent
                                            hoverEnabled: true
                                            onClicked: win.selected = tile.modelData.name
                                            onDoubleClicked: {
                                                win.selected = tile.modelData.name
                                                win.run(["set", tile.modelData.name])
                                            }
                                        }
                                    }
                                }
                            }

                            Muted {
                                anchors.centerIn: parent
                                width: Math.min(parent.width - 40, 340)
                                horizontalAlignment: Text.AlignHCenter
                                wrapMode: Text.WordWrap
                                opacity: win.shown.length === 0 ? 1 : 0
                                text: (win.info.styles || []).length === 0
                                    ? "No styles saved yet. Set your desktop up how you like it, then type a name at the top right and hit Save."
                                    : "Nothing matches."

                                Behavior on opacity { NumberAnimation { duration: 200 } }
                            }
                        }

                        Item {
                            id: side
                            visible: (win.info.styles || []).length > 0
                            Layout.preferredWidth: 270
                            Layout.fillHeight: true

                            Muted {
                                anchors.centerIn: parent
                                text: "Select a style to see its details"
                                opacity: win.selectedStyle === null ? 1 : 0

                                Behavior on opacity { NumberAnimation { duration: 240 } }
                            }

                            Rectangle {
                                id: panel
                                width: parent.width
                                height: parent.height
                                x: win.selectedStyle !== null ? 0 : 56
                                opacity: win.selectedStyle !== null ? 1 : 0
                                enabled: win.selectedStyle !== null
                                radius: 4
                                color: "#1b1b20"
                                border.width: 1
                                border.color: "#2a2a30"

                                Behavior on x { NumberAnimation { duration: 320; easing.type: Easing.OutCubic } }
                                Behavior on opacity { NumberAnimation { duration: 260; easing.type: Easing.OutCubic } }

                                Flickable {
                                    id: flick
                                    anchors.fill: parent
                                    anchors.margins: 12
                                    clip: true
                                    contentWidth: width
                                    contentHeight: details.implicitHeight
                                    boundsBehavior: Flickable.StopAtBounds

                                    ColumnLayout {
                                        id: details
                                        width: flick.width
                                        spacing: 8

                                        Rectangle {
                                            Layout.fillWidth: true
                                            Layout.preferredHeight: width * 0.5625
                                            color: "#101014"
                                            clip: true

                                            Image {
                                                id: bigShot
                                                anchors.fill: parent
                                                fillMode: Image.PreserveAspectCrop
                                                asynchronous: true
                                                cache: false
                                                opacity: status === Image.Ready ? 1 : 0
                                                source: win.shownStyle && win.shownStyle.preview
                                                    ? "file://" + win.shownStyle.preview + "?" + win.stamp
                                                    : ""

                                                Behavior on opacity { NumberAnimation { duration: 260 } }
                                            }

                                            Muted {
                                                anchors.centerIn: parent
                                                horizontalAlignment: Text.AlignHCenter
                                                text: "No preview\nDrop an image here"
                                                opacity: !win.shownStyle || !win.shownStyle.preview || bigShot.status === Image.Error ? 1 : 0

                                                Behavior on opacity { NumberAnimation { duration: 200 } }
                                            }

                                            Rectangle {
                                                anchors.fill: parent
                                                color: "#cc16161a"
                                                border.width: 2
                                                border.color: "#7aa2f7"
                                                opacity: dropper.containsDrag ? 1 : 0

                                                Behavior on opacity { NumberAnimation { duration: 140 } }

                                                Text {
                                                    anchors.centerIn: parent
                                                    text: "Drop to use as preview"
                                                    color: "#e0e0e8"
                                                    font.pixelSize: 13
                                                }
                                            }

                                            DropArea {
                                                id: dropper
                                                anchors.fill: parent
                                                onDropped: drop => {
                                                    if (drop.hasUrls)
                                                        win.setPreview(drop.urls[0].toString())
                                                }
                                            }
                                        }

                                        Text {
                                            Layout.fillWidth: true
                                            Layout.topMargin: 4
                                            text: win.shownStyle ? win.shownStyle.name : ""
                                            color: "#e6e6ee"
                                            font.pixelSize: 16
                                            elide: Text.ElideRight
                                        }

                                        Btn {
                                            Layout.fillWidth: true
                                            accent: true
                                            label: win.shownStyle && win.shownStyle.current ? "Reapply" : "Apply"
                                            onClicked: win.run(["set", win.selected])
                                        }

                                        Btn {
                                            Layout.fillWidth: true
                                            danger: win.confirming === "update"
                                            label: win.confirming === "update" ? "Click again to overwrite" : "Update with current setup"
                                            onClicked: {
                                                if (win.confirming !== "update") {
                                                    win.confirming = "update"
                                                    return
                                                }
                                                win.confirming = ""
                                                win.runHidden(["save", win.selected])
                                            }
                                        }

                                        Muted {
                                            Layout.topMargin: 8
                                            text: "Preview image"
                                        }

                                        RowLayout {
                                            Layout.fillWidth: true
                                            spacing: 6

                                            Input {
                                                id: pathField
                                                Layout.fillWidth: true
                                                hint: "Path to an image"
                                                onAccepted: {
                                                    win.setPreview(text)
                                                    text = ""
                                                }
                                            }

                                            Btn {
                                                label: "Set"
                                                enabled: pathField.text.trim() !== ""
                                                onClicked: {
                                                    win.setPreview(pathField.text)
                                                    pathField.text = ""
                                                }
                                            }
                                        }

                                        RowLayout {
                                            Layout.fillWidth: true
                                            spacing: 6

                                            Btn {
                                                Layout.fillWidth: true
                                                label: "Screenshot"
                                                onClicked: win.runHidden(["setpreview", win.selected])
                                            }

                                            Btn {
                                                Layout.fillWidth: true
                                                label: "Remove"
                                                enabled: win.shownStyle !== null && !!win.shownStyle.preview
                                                onClicked: win.run(["deletepreview", win.selected])
                                            }
                                        }

                                        Muted {
                                            Layout.topMargin: 8
                                            text: "Aliases"
                                        }

                                        Flow {
                                            Layout.fillWidth: true
                                            spacing: 6

                                            Repeater {
                                                model: win.shownStyle ? win.shownStyle.aliases : []

                                                delegate: Btn {
                                                    required property string modelData
                                                    label: modelData + " ×"
                                                    onClicked: win.run(["deletealias", win.selected, modelData])
                                                }
                                            }
                                        }

                                        RowLayout {
                                            Layout.fillWidth: true
                                            spacing: 6

                                            Input {
                                                id: aliasField
                                                Layout.fillWidth: true
                                                hint: "New alias"
                                                onAccepted: {
                                                    win.run(["alias", win.selected, text.trim()])
                                                    text = ""
                                                }
                                            }

                                            Btn {
                                                label: "Add"
                                                enabled: aliasField.text.trim() !== ""
                                                onClicked: {
                                                    win.run(["alias", win.selected, aliasField.text.trim()])
                                                    aliasField.text = ""
                                                }
                                            }
                                        }

                                        Muted {
                                            Layout.topMargin: 8
                                            text: "Rename"
                                        }

                                        RowLayout {
                                            Layout.fillWidth: true
                                            spacing: 6

                                            Input {
                                                id: renameField
                                                Layout.fillWidth: true
                                                hint: "New name"
                                            }

                                            Btn {
                                                label: "Rename"
                                                enabled: renameField.text.trim() !== ""
                                                onClicked: {
                                                    const next = renameField.text.trim()
                                                    win.pending = next
                                                    win.run(["rename", win.selected, next])
                                                    renameField.text = ""
                                                }
                                            }
                                        }

                                        Btn {
                                            Layout.fillWidth: true
                                            Layout.topMargin: 12
                                            danger: win.confirming === "delete"
                                            label: win.confirming === "delete" ? "Click again to delete" : "Delete"
                                            onClicked: {
                                                if (win.confirming !== "delete") {
                                                    win.confirming = "delete"
                                                    return
                                                }
                                                win.confirming = ""
                                                win.run(["delete", win.selected])
                                                win.selected = ""
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Muted {
                        id: statusText
                        Layout.fillWidth: true
                        Layout.topMargin: 12
                        text: win.status !== "" ? win.status : "Double-click a style to apply it"
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
                }
            }
        }
        """";
}
