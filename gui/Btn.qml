import QtQuick

Rectangle {
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
