import QtQuick
import QtQuick.Layouts

Item {
    id: root
    required property var shell
    property alias grid: grid

    function saveCurrent() {
        const name = saveName.text.trim()
        if (name === "")
            return
        shell.pending = name
        saveName.text = ""
        shell.runHidden(["save", name])
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            Layout.fillWidth: true
            spacing: 8

            Input {
                Layout.preferredWidth: 220
                hint: "Search"
                onTextChanged: shell.query = text
            }

            Item { Layout.fillWidth: true }

            Input {
                id: saveName
                Layout.preferredWidth: 260
                hint: "Save current setup as..."
                onAccepted: root.saveCurrent()
            }

            Btn {
                label: "Save"
                accent: true
                enabled: saveName.text.trim() !== ""
                onClicked: root.saveCurrent()
            }
        }

        RowLayout {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.topMargin: 12
            spacing: 14

            ColumnLayout {
                visible: (shell.info.folders || []).length > 0
                Layout.minimumWidth: 150
                Layout.preferredWidth: 150
                Layout.maximumWidth: 150
                Layout.fillHeight: true
                spacing: 0

                SideRow {
                    Layout.fillWidth: true
                    label: "All styles"
                    on: shell.folder === ""
                    onClicked: shell.folder = ""
                }

                Repeater {
                    model: shell.info.folders || []

                    delegate: SideRow {
                        required property string modelData
                        Layout.fillWidth: true
                        label: modelData
                        on: shell.folder === modelData
                        onClicked: shell.folder = modelData
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
                    model: shell.shown

                    delegate: Item {
                        id: tile
                        required property var modelData
                        required property int index
                        readonly property bool picked: modelData.name === shell.selected
                        width: grid.cellWidth
                        height: grid.cellHeight

                        Component.onCompleted: {
                            if (!shell.intro)
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
                                        ? "file://" + tile.modelData.preview + "?" + shell.stamp
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
                                onClicked: shell.selected = tile.modelData.name
                                onDoubleClicked: {
                                    shell.selected = tile.modelData.name
                                    shell.run(["set", tile.modelData.name])
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
                    opacity: shell.shown.length === 0 ? 1 : 0
                    text: (shell.info.styles || []).length === 0
                        ? "No styles saved yet. Set your desktop up how you like it, then type a name at the top right and hit Save."
                        : "Nothing matches."

                    Behavior on opacity { NumberAnimation { duration: 200 } }
                }
            }

            Item {
                id: side
                visible: (shell.info.styles || []).length > 0
                Layout.preferredWidth: 270
                Layout.fillHeight: true

                Muted {
                    anchors.centerIn: parent
                    text: "Select a style to see its details"
                    opacity: shell.selectedStyle === null ? 1 : 0

                    Behavior on opacity { NumberAnimation { duration: 240 } }
                }

                Rectangle {
                    id: panel
                    width: parent.width
                    height: parent.height
                    x: shell.selectedStyle !== null ? 0 : 56
                    opacity: shell.selectedStyle !== null ? 1 : 0
                    enabled: shell.selectedStyle !== null
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
                                    source: shell.shownStyle && shell.shownStyle.preview
                                        ? "file://" + shell.shownStyle.preview + "?" + shell.stamp
                                        : ""

                                    Behavior on opacity { NumberAnimation { duration: 260 } }
                                }

                                Muted {
                                    anchors.centerIn: parent
                                    horizontalAlignment: Text.AlignHCenter
                                    text: "No preview\nDrop an image here"
                                    opacity: !shell.shownStyle || !shell.shownStyle.preview || bigShot.status === Image.Error ? 1 : 0

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
                                            shell.setPreview(drop.urls[0].toString())
                                    }
                                }
                            }

                            Text {
                                Layout.fillWidth: true
                                Layout.topMargin: 4
                                text: shell.shownStyle ? shell.shownStyle.name : ""
                                color: "#e6e6ee"
                                font.pixelSize: 16
                                elide: Text.ElideRight
                            }

                            Btn {
                                Layout.fillWidth: true
                                accent: true
                                label: shell.shownStyle && shell.shownStyle.current ? "Reapply" : "Apply"
                                onClicked: shell.run(["set", shell.selected])
                            }

                            Btn {
                                Layout.fillWidth: true
                                danger: shell.confirming === "update"
                                label: shell.confirming === "update" ? "Click again to overwrite" : "Update with current setup"
                                onClicked: {
                                    if (shell.confirming !== "update") {
                                        shell.confirming = "update"
                                        return
                                    }
                                    shell.confirming = ""
                                    shell.runHidden(["save", shell.selected])
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
                                        shell.setPreview(text)
                                        text = ""
                                    }
                                }

                                Btn {
                                    label: "Set"
                                    enabled: pathField.text.trim() !== ""
                                    onClicked: {
                                        shell.setPreview(pathField.text)
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
                                    onClicked: shell.runHidden(["setpreview", shell.selected])
                                }

                                Btn {
                                    Layout.fillWidth: true
                                    label: "Remove"
                                    enabled: shell.shownStyle !== null && !!shell.shownStyle.preview
                                    onClicked: shell.run(["deletepreview", shell.selected])
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
                                    model: shell.shownStyle ? shell.shownStyle.aliases : []

                                    delegate: Btn {
                                        required property string modelData
                                        label: modelData + " ×"
                                        onClicked: shell.run(["deletealias", shell.selected, modelData])
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
                                        shell.run(["alias", shell.selected, text.trim()])
                                        text = ""
                                    }
                                }

                                Btn {
                                    label: "Add"
                                    enabled: aliasField.text.trim() !== ""
                                    onClicked: {
                                        shell.run(["alias", shell.selected, aliasField.text.trim()])
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
                                        shell.pending = next
                                        shell.run(["rename", shell.selected, next])
                                        renameField.text = ""
                                    }
                                }
                            }

                            Btn {
                                Layout.fillWidth: true
                                Layout.topMargin: 12
                                danger: shell.confirming === "delete"
                                label: shell.confirming === "delete" ? "Click again to delete" : "Delete"
                                onClicked: {
                                    if (shell.confirming !== "delete") {
                                        shell.confirming = "delete"
                                        return
                                    }
                                    shell.confirming = ""
                                    shell.run(["delete", shell.selected])
                                    shell.selected = ""
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
