import QtQuick

Rectangle {
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
