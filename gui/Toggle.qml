import QtQuick

Item {
    id: root
    property bool checked: false
    signal toggled(bool value)

    implicitWidth: 34
    implicitHeight: 18
    opacity: enabled ? 1 : 0.4

    Rectangle {
        anchors.fill: parent
        radius: height / 2
        color: root.checked ? "#7aa2f7" : "#25252c"
        border.width: 1
        border.color: root.checked ? "#7aa2f7" : "#3a3a44"

        Behavior on color { ColorAnimation { duration: 140 } }
        Behavior on border.color { ColorAnimation { duration: 140 } }
    }

    Rectangle {
        width: 12
        height: 12
        radius: 6
        y: 3
        x: root.checked ? root.width - width - 3 : 3
        color: root.checked ? "#14141a" : "#a8a8b2"

        Behavior on x { NumberAnimation { duration: 150; easing.type: Easing.OutCubic } }
        Behavior on color { ColorAnimation { duration: 140 } }
    }

    MouseArea {
        anchors.fill: parent
        cursorShape: Qt.PointingHandCursor
        onClicked: root.toggled(!root.checked)
    }
}
