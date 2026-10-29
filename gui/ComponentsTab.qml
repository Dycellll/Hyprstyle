import QtQuick
import QtQuick.Layouts

Item {
    id: root
    required property var shell

    property int revision: 0
    property bool loaded: false
    property string savedSnapshot: ""
    property var savedRows: []
    property string pendingBaseline: ""
    property var pendingRows: []

    readonly property bool dirty: {
        revision
        return loaded && snapshot() !== savedSnapshot
    }

    readonly property string firstProblem: {
        revision
        for (let i = 0; i < listModel.count; i++) {
            const problem = listModel.get(i).problem
            if (problem === "empty")
                return "Give every component a name to save."
            if (problem === "duplicate")
                return "Two components share a name."
            if (problem === "invalid")
                return "A name has a slash or starts with a dot."
        }
        return ""
    }

    readonly property bool valid: firstProblem === ""

    function rows() {
        const out = []
        for (let i = 0; i < listModel.count; i++) {
            const r = listModel.get(i)
            out.push({
                name: r.name,
                type: r.type,
                source: r.source,
                saveHook: r.saveHook,
                loadHook: r.loadHook,
                saveOn: r.saveOn,
                loadOn: r.loadOn,
                hypr: r.hypr
            })
        }
        return out
    }

    function persisted(r) {
        return {
            name: r.name.trim(),
            type: r.type,
            source: r.type === "copy" ? r.source.trim() : "",
            saveHook: r.saveOn ? r.saveHook.trim() : "",
            loadHook: r.loadOn ? r.loadHook.trim() : "",
            hypr: r.hypr
        }
    }

    function snapshot() {
        return JSON.stringify(rows().map(persisted))
    }

    function fromConfig(c) {
        const saveHook = c.saveHook || ""
        const loadHook = c.loadHook || ""
        return {
            name: c.name || "",
            type: c.type === "script" ? "script" : "copy",
            source: c.source || "",
            saveHook: saveHook,
            loadHook: loadHook,
            saveOn: saveHook.trim() !== "",
            loadOn: loadHook.trim() !== "",
            hypr: c.hyprConfig === true
        }
    }

    function fill(list) {
        listModel.clear()
        const expand = list.length <= 3

        for (const r of list) {
            listModel.append({
                name: r.name,
                type: r.type,
                source: r.source,
                saveHook: r.saveHook,
                loadHook: r.loadHook,
                saveOn: r.saveOn,
                loadOn: r.loadOn,
                hypr: r.hypr,
                open: expand,
                problem: ""
            })
        }

        revalidate()
        revision++
    }

    function sync(configs) {
        if (loaded)
            return

        fill(configs.map(fromConfig))
        savedRows = rows()
        savedSnapshot = snapshot()
        loaded = true
    }

    function restore() {
        fill(savedRows)
    }

    function problemFor(i) {
        const name = listModel.get(i).name.trim()

        if (name === "")
            return "empty"
        if (name.indexOf("/") >= 0 || name.charAt(0) === ".")
            return "invalid"

        for (let j = 0; j < listModel.count; j++) {
            if (j !== i && listModel.get(j).name.trim().toLowerCase() === name.toLowerCase())
                return "duplicate"
        }

        return ""
    }

    function revalidate() {
        for (let i = 0; i < listModel.count; i++)
            listModel.setProperty(i, "problem", problemFor(i))
    }

    function setField(i, field, value) {
        listModel.setProperty(i, field, value)

        if (field === "name")
            revalidate()

        revision++
    }

    function setType(i, type) {
        listModel.setProperty(i, "type", type)

        const r = listModel.get(i)

        if (type === "script" && !r.saveOn && !r.loadOn) {
            listModel.setProperty(i, "saveOn", true)
            listModel.setProperty(i, "loadOn", true)
        }

        revision++
    }

    function removeAt(i) {
        listModel.remove(i)
        revalidate()
        revision++
    }

    function addComponent() {
        listModel.append({
            name: "",
            type: "copy",
            source: "",
            saveHook: "",
            loadHook: "",
            saveOn: false,
            loadOn: false,
            hypr: false,
            open: true,
            problem: "empty"
        })

        revision++
        revealNew.restart()
    }

    function payload() {
        const components = []
        const hypr = []

        for (const r of rows()) {
            const p = persisted(r)
            const entry = { name: p.name, type: p.type }

            if (p.type === "copy")
                entry.source = p.source
            if (p.saveHook !== "")
                entry.saveHook = p.saveHook
            if (p.loadHook !== "")
                entry.loadHook = p.loadHook

            components.push(entry)

            if (p.hypr)
                hypr.push(p.name)
        }

        return { components: components, hyprConfigComponents: hypr }
    }

    function save() {
        if (!dirty || !valid)
            return

        pendingRows = rows()
        pendingBaseline = snapshot()
        shell.run(["--save-components", JSON.stringify(payload())])
    }

    function saveFinished(ok) {
        if (!ok)
            return

        savedRows = pendingRows
        savedSnapshot = pendingBaseline
        revision++
    }

    ListModel {
        id: listModel
    }

    Timer {
        id: revealNew
        interval: 50

        onTriggered: {
            const item = list.itemAtIndex(listModel.count - 1)

            if (!item)
                return

            item.focusName()
            scroll.to = Math.max(0, Math.min(item.y, list.contentHeight - list.height))
            scroll.restart()
        }
    }

    Shortcut {
        sequence: "Ctrl+S"
        enabled: root.visible && root.dirty && root.valid
        onActivated: root.save()
    }

    ColumnLayout {
        anchors.fill: parent
        spacing: 0

        RowLayout {
            Layout.fillWidth: true
            spacing: 8

            Muted {
                Layout.fillWidth: true
                elide: Text.ElideRight
                color: root.dirty ? (root.valid ? "#7aa2f7" : "#eba0a6") : "#8a8a94"
                text: !root.dirty ? "Everything your styles handle."
                    : root.valid ? "You have unsaved changes."
                    : root.firstProblem

                Behavior on color { ColorAnimation { duration: 160 } }
            }

            ConfirmBtn {
                idleLabel: "Discard"
                askLabel: "Click again to discard"
                enabled: root.dirty
                onConfirmed: root.restore()
            }

            Btn {
                label: "Save changes"
                accent: true
                enabled: root.dirty && root.valid
                onClicked: root.save()
            }
        }

        Item {
            Layout.fillWidth: true
            Layout.fillHeight: true
            Layout.topMargin: 12

            ListView {
                id: list
                anchors.fill: parent
                anchors.rightMargin: 12
                clip: true
                spacing: 10
                boundsBehavior: Flickable.StopAtBounds
                model: listModel

                delegate: ComponentCard {
                    width: list.width
                    tab: root
                }

                footer: ColumnLayout {
                    width: list.width
                    spacing: 10

                    Muted {
                        Layout.fillWidth: true
                        visible: listModel.count === 0
                        horizontalAlignment: Text.AlignHCenter
                        wrapMode: Text.WordWrap
                        text: "A component is one thing your styles remember: your terminal's config, your wallpaper, your bar. Add your first one below."
                    }

                    Rectangle {
                        Layout.fillWidth: true
                        Layout.preferredHeight: 64
                        Layout.topMargin: 10
                        radius: 4
                        color: addHit.containsMouse ? "#1f1f25" : "#18181d"
                        border.width: 1
                        border.color: addHit.containsMouse ? "#7aa2f7" : "#2e2e36"

                        Behavior on color { ColorAnimation { duration: 140 } }
                        Behavior on border.color { ColorAnimation { duration: 140 } }

                        Row {
                            anchors.centerIn: parent
                            spacing: 10

                            Text {
                                anchors.verticalCenter: parent.verticalCenter
                                text: "+"
                                color: addHit.containsMouse ? "#7aa2f7" : "#8a8a94"
                                font.pixelSize: 26

                                Behavior on color { ColorAnimation { duration: 140 } }
                            }

                            Text {
                                anchors.verticalCenter: parent.verticalCenter
                                text: "Add a component"
                                color: addHit.containsMouse ? "#d4d4dc" : "#8a8a94"
                                font.pixelSize: 13

                                Behavior on color { ColorAnimation { duration: 140 } }
                            }
                        }

                        MouseArea {
                            id: addHit
                            anchors.fill: parent
                            hoverEnabled: true
                            cursorShape: Qt.PointingHandCursor
                            onClicked: root.addComponent()
                        }
                    }

                    Item {
                        Layout.preferredHeight: 2
                    }
                }

                add: Transition {
                    NumberAnimation { property: "opacity"; from: 0; to: 1; duration: 220 }
                }

                remove: Transition {
                    NumberAnimation { property: "opacity"; to: 0; duration: 160 }
                }

                displaced: Transition {
                    NumberAnimation { property: "y"; duration: 220; easing.type: Easing.OutCubic }
                }

                NumberAnimation {
                    id: scroll
                    target: list
                    property: "contentY"
                    duration: 260
                    easing.type: Easing.OutCubic
                }
            }

            Rectangle {
                anchors.right: parent.right
                width: 3
                radius: 1.5
                color: "#3a3a44"
                y: list.visibleArea.yPosition * list.height
                height: list.visibleArea.heightRatio * list.height
                visible: list.visibleArea.heightRatio < 1
            }
        }
    }
}
