import QtQuick

Item {
    id: tab
    property string label: ""
    property bool on: false
    property bool dot: false
    signal clicked()

    implicitWidth: caption.implicitWidth + 32
    implicitHeight: 34

    Text {
        id: caption
        anchors.centerIn: parent
        text: tab.label
        font.pixelSize: 13
        color: tab.on ? "#e6e6ee" : tabHit.containsMouse ? "#c4c4cc" : "#8a8a94"

        Behavior on color { ColorAnimation { duration: 140 } }
    }

    Rectangle {
        anchors.left: caption.right
        anchors.leftMargin: 6
        anchors.verticalCenter: caption.verticalCenter
        width: 6
        height: 6
        radius: 3
        color: "#7aa2f7"
        opacity: tab.dot ? 1 : 0

        Behavior on opacity { NumberAnimation { duration: 160 } }
    }

    Rectangle {
        anchors.left: parent.left
        anchors.right: parent.right
        anchors.bottom: parent.bottom
        height: 2
        color: "#7aa2f7"
        opacity: tab.on ? 1 : 0

        Behavior on opacity { NumberAnimation { duration: 180 } }
    }

    MouseArea {
        id: tabHit
        anchors.fill: parent
        hoverEnabled: true
        cursorShape: Qt.PointingHandCursor
        onClicked: tab.clicked()
    }
}
