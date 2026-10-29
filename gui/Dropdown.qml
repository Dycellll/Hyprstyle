import QtQuick

Rectangle {
    id: root
    property var options: []
    property string current: ""
    property bool open: false
    signal picked(string value)

    readonly property bool over: head.containsMouse || rows.hovered

    function capital(word) {
        return word.charAt(0).toUpperCase() + word.slice(1)
    }

    function label(value) {
        const parts = value.split("-")
        return parts.length > 1 ? capital(parts[0]) + " (" + capital(parts[1]) + ")" : capital(value)
    }

    onOverChanged: {
        if (over)
            closer.stop()
        else if (open)
            closer.restart()
    }

    implicitWidth: 140
    implicitHeight: 30
    radius: 4
    z: open ? 20 : 0
    color: "#1b1b20"
    opacity: enabled ? 1 : 0.4
    border.width: 1
    border.color: open || head.containsMouse ? "#7aa2f7" : "#2e2e36"

    Behavior on border.color { ColorAnimation { duration: 140 } }
    Behavior on opacity { NumberAnimation { duration: 140 } }

    Timer {
        id: closer
        interval: 350
        onTriggered: root.open = false
    }

    Text {
        anchors.left: parent.left
        anchors.leftMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        text: root.label(root.current)
        color: "#d4d4dc"
        font.pixelSize: 13
    }

    Text {
        anchors.right: parent.right
        anchors.rightMargin: 10
        anchors.verticalCenter: parent.verticalCenter
        text: "▾"
        color: "#a8a8b2"
        font.pixelSize: 12
        rotation: root.open ? 180 : 0

        Behavior on rotation { NumberAnimation { duration: 160; easing.type: Easing.OutCubic } }
    }

    MouseArea {
        id: head
        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: root.open = !root.open
    }

    Rectangle {
        y: root.height + 4
        width: root.width
        height: root.options.length * 28 + 4
        radius: 4
        color: "#1b1b20"
        border.width: 1
        border.color: "#34343c"
        opacity: root.open ? 1 : 0
        visible: opacity > 0

        Behavior on opacity { NumberAnimation { duration: 140 } }

        Column {
            id: rows
            x: 2
            y: 2
            readonly property bool hovered: root.options.length > 0 && hoverCount.count > 0

            QtObject {
                id: hoverCount
                property int count: 0
            }

            Repeater {
                model: root.options

                delegate: Rectangle {
                    id: row
                    required property string modelData
                    width: root.width - 4
                    height: 28
                    radius: 3
                    color: rowHit.containsMouse ? "#2a2a31" : "transparent"

                    Behavior on color { ColorAnimation { duration: 120 } }

                    Text {
                        anchors.left: parent.left
                        anchors.leftMargin: 8
                        anchors.verticalCenter: parent.verticalCenter
                        text: root.label(row.modelData)
                        color: row.modelData === root.current ? "#7aa2f7" : "#d4d4dc"
                        font.pixelSize: 13
                    }

                    MouseArea {
                        id: rowHit
                        anchors.fill: parent
                        hoverEnabled: true
                        cursorShape: Qt.PointingHandCursor
                        onContainsMouseChanged: hoverCount.count += containsMouse ? 1 : -1
                        onClicked: {
                            root.open = false
                            root.picked(row.modelData)
                        }
                    }
                }
            }
        }
    }
}
